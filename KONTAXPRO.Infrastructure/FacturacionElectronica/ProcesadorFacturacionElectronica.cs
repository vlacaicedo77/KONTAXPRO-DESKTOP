using System.Text;
using System.Security.Cryptography;
using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ProcesadorFacturacionElectronica(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    IGeneradorXmlComprobanteElectronico generador,
    IFirmadorXmlComprobanteElectronico firmador,
    IClienteRecepcionSri recepcion,
    IClienteAutorizacionSri autorizacion,
    IAlmacenamientoDocumentosElectronicos storage,
    ILogger<ProcesadorFacturacionElectronica> logger)
    : IProcesadorFacturacionElectronica
{
    public async Task<int> RecuperarProcesamientosAbandonadosAsync(
        Guid instalacionId,
        TimeSpan antiguedadMinima,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var processing = await EstadoIdAsync(context,
            EstadosComprobanteElectronico.Procesando, cancellationToken);
        var technical = await EstadoIdAsync(context,
            EstadosComprobanteElectronico.ErrorTecnico, cancellationToken);
        var limit = DateTime.UtcNow - antiguedadMinima;
        var rows = await context.ComprobantesElectronicos
            .Where(x => x.EstadoComprobanteElectronicoId == processing &&
                        x.ProcesamientoIniciadoAt < limit)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.EstadoComprobanteElectronicoId, technical)
                .SetProperty(x => x.ProcesamientoIniciadoAt, (DateTime?)null)
                .SetProperty(x => x.ProcesadoPorInstalacionUuid, (Guid?)null)
                .SetProperty(x => x.UpdatedAt, DateTime.UtcNow), cancellationToken);
        return rows;
    }

    public async Task<int> ProcesarPendientesAsync(
        Guid instalacionId,
        CancellationToken cancellationToken = default)
    {
        var ids = await ObtenerCandidatosAsync(cancellationToken);
        var processed = 0;
        foreach (var id in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await ReclamarAsync(id, instalacionId, cancellationToken))
                continue;
            try
            {
                await ProcesarUnoAsync(id, cancellationToken);
                processed++;
            }
            catch (Exception ex) when (ex is IOException or HttpRequestException or
                TaskCanceledException or TimeoutException)
            {
                await RegistrarErrorTecnicoAsync(id, ex, cancellationToken);
                logger.LogWarning(ex,
                    "Error técnico transitorio al procesar comprobante electrónico {ComprobanteId}.",
                    id);
            }
            catch (Exception ex) when (ex is CryptographicException or XmlException or
                InvalidDataException or InvalidOperationException or ArgumentException or
                FormatException)
            {
                await RegistrarErrorDefinitivoAsync(id, ex, cancellationToken);
                logger.LogError(ex,
                    "Error definitivo al procesar comprobante electrónico {ComprobanteId}.",
                    id);
            }
        }
        return processed;
    }

    private async Task ProcesarUnoAsync(long id, CancellationToken cancellationToken)
    {
        var snapshot = await CargarSnapshotAsync(id, cancellationToken);
        logger.LogInformation(
            "Procesando comprobante electrónico {ComprobanteId} de empresa {EmpresaId}, " +
            "clave {ClaveAcceso}, ambiente {Ambiente}.",
            id, snapshot.EmpresaId, snapshot.ClaveAcceso, snapshot.Ambiente);
        var artifact = new ArtefactoElectronico(snapshot.EmpresaId,
            snapshot.Ruc, snapshot.ClaveAcceso, "GENERADO");
        if (!snapshot.XmlGeneradoAt.HasValue)
        {
            await using var xml = await generador.GenerarAsync(id, cancellationToken);
            await storage.GuardarAsync(artifact, xml, cancellationToken);
            await ActualizarEtapaAsync(id, EstadosComprobanteElectronico.Generado,
                x =>
                {
                    x.XmlGeneradoAt = DateTime.UtcNow;
                    x.XmlGeneradoReferencia = "GENERADO";
                }, cancellationToken);
        }
        if (!snapshot.XmlFirmadoAt.HasValue)
        {
            await using var generated = await storage.AbrirLecturaAsync(artifact, cancellationToken);
            await using var signed = await firmador.FirmarAsync(
                generated, snapshot.EmpresaId, cancellationToken);
            await storage.GuardarAsync(artifact with { TipoArtefacto = "FIRMADO" },
                signed, cancellationToken);
            await ActualizarEtapaAsync(id, EstadosComprobanteElectronico.Firmado,
                x =>
                {
                    x.XmlFirmadoAt = DateTime.UtcNow;
                    x.XmlFirmadoReferencia = "FIRMADO";
                }, cancellationToken);
        }

        // Si hubo un envío incierto, primero se reconcilia por clave. Nunca se
        // genera otra clave ni se reenvía ciegamente.
        if (snapshot.IntentosEnvio > 0 && snapshot.EstadoRecepcion != "RECIBIDA")
        {
            var reconciliation = await autorizacion.ConsultarAsync(
                snapshot.Ambiente, snapshot.ClaveAcceso, cancellationToken);
            if (reconciliation.Estado == "AUTORIZADO")
            {
                await AplicarAutorizacionAsync(id, artifact, reconciliation, cancellationToken);
                return;
            }
        }

        if (snapshot.EstadoRecepcion != "RECIBIDA")
        {
            byte[] bytes;
            await using (var signed = await storage.AbrirLecturaAsync(
                artifact with { TipoArtefacto = "FIRMADO" }, cancellationToken))
            {
                using var memory = new MemoryStream();
                await signed.CopyToAsync(memory, cancellationToken);
                bytes = memory.ToArray();
            }
            var reception = await recepcion.EnviarAsync(snapshot.Ambiente,
                snapshot.ClaveAcceso, bytes, cancellationToken);
            logger.LogInformation(
                "Recepción SRI del comprobante {ComprobanteId}: {EstadoRecepcion}.",
                id, reception.Estado);
            await RegistrarRecepcionAsync(id, reception, cancellationToken);
            if (reception.Estado != "RECIBIDA") return;
        }

        var authorization = await autorizacion.ConsultarAsync(snapshot.Ambiente,
            snapshot.ClaveAcceso, cancellationToken);
        logger.LogInformation(
            "Autorización SRI del comprobante {ComprobanteId}: {EstadoAutorizacion}.",
            id, authorization.Estado);
        if (authorization.Estado == "AUTORIZADO")
            await AplicarAutorizacionAsync(id, artifact, authorization, cancellationToken);
        else
            await RegistrarAutorizacionAsync(id, authorization, cancellationToken);
    }

    private async Task<long[]> ObtenerCandidatosAsync(CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var terminal = new[]
        {
            EstadosComprobanteElectronico.Autorizado,
            EstadosComprobanteElectronico.NoAutorizado,
            EstadosComprobanteElectronico.Devuelto,
            EstadosComprobanteElectronico.Error
        };
        return await context.ComprobantesElectronicos.AsNoTracking()
            .Where(x =>
                !terminal.Contains(x.EstadoComprobanteElectronico!.Codigo) &&
                x.Empresa!.FacturacionElectronica != null &&
                x.Empresa.FacturacionElectronica.Habilitada &&
                x.Empresa.FacturacionElectronica.CertificadoReferencia != null)
            .OrderBy(x => x.CreatedAt).Select(x => x.Id).Take(10).ToArrayAsync(token);
    }

    private async Task<bool> ReclamarAsync(long id, Guid installation, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var processing = await EstadoIdAsync(context,
            EstadosComprobanteElectronico.Procesando, token);
        var rows = await context.ComprobantesElectronicos
            .Where(x => x.Id == id && x.ProcesamientoIniciadoAt == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.EstadoComprobanteElectronicoId, processing)
                .SetProperty(x => x.ProcesamientoIniciadoAt, DateTime.UtcNow)
                .SetProperty(x => x.ProcesadoPorInstalacionUuid, installation), token);
        return rows == 1;
    }

    private async Task<ComprobanteSnapshot> CargarSnapshotAsync(
        long id, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        return await context.ComprobantesElectronicos.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ComprobanteSnapshot(
                x.EmpresaId, x.Empresa!.NumeroIdentificacion, x.ClaveAcceso,
                x.TipoAmbiente!.Codigo, x.XmlGeneradoAt, x.XmlFirmadoAt,
                x.IntentosEnvio, x.EstadoRecepcion))
            .SingleAsync(token);
    }

    private async Task ActualizarEtapaAsync(long id, string state,
        Action<ComprobanteElectronico> update, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.ComprobantesElectronicos.SingleAsync(x => x.Id == id, token);
        entity.EstadoComprobanteElectronicoId = await EstadoIdAsync(context, state, token);
        update(entity);
        entity.UpdatedAt = DateTime.UtcNow;
        context.ComprobantesElectronicosEventos.Add(new()
        {
            ComprobanteElectronicoId = id,
            TipoEvento = state == EstadosComprobanteElectronico.Generado
                ? TiposEventoComprobanteElectronico.Generacion
                : TiposEventoComprobanteElectronico.Firma,
            Codigo = state,
            Mensaje = state == EstadosComprobanteElectronico.Generado
                ? "XML generado y validado localmente."
                : "XML firmado y verificado localmente.",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(token);
    }

    private async Task RegistrarRecepcionAsync(long id, ResultadoSri result, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.ComprobantesElectronicos.SingleAsync(x => x.Id == id, token);
        entity.IntentosEnvio++;
        entity.FechaEnvio = DateTime.UtcNow;
        entity.EstadoRecepcion = result.Estado;
        entity.EstadoComprobanteElectronicoId = await EstadoIdAsync(context,
            result.Estado == "RECIBIDA" ? EstadosComprobanteElectronico.Recibido :
            EstadosComprobanteElectronico.Devuelto, token);
        AgregarEventos(context, id, TiposEventoComprobanteElectronico.Recepcion, result.Mensajes);
        Liberar(entity);
        await context.SaveChangesAsync(token);
    }

    private async Task RegistrarAutorizacionAsync(long id, ResultadoSri result, CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.ComprobantesElectronicos.SingleAsync(x => x.Id == id, token);
        entity.IntentosAutorizacion++;
        entity.FechaUltimaConsulta = DateTime.UtcNow;
        entity.EstadoAutorizacion = result.Estado;
        var state = result.Estado == "NO_AUTORIZADO"
            ? EstadosComprobanteElectronico.NoAutorizado
            : EstadosComprobanteElectronico.PendienteAutorizacion;
        entity.EstadoComprobanteElectronicoId = await EstadoIdAsync(context, state, token);
        AgregarEventos(context, id, TiposEventoComprobanteElectronico.Autorizacion, result.Mensajes);
        Liberar(entity);
        await context.SaveChangesAsync(token);
    }

    private async Task AplicarAutorizacionAsync(long id, ArtefactoElectronico artifact,
        ResultadoSri result, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(result.XmlAutorizado))
            throw new InvalidDataException(
                "El SRI informó AUTORIZADO sin devolver el comprobante autorizado.");
        await using var stream = new MemoryStream(
            Encoding.UTF8.GetBytes(result.XmlAutorizado));
        await storage.GuardarAsync(artifact with { TipoArtefacto = "AUTORIZADO" },
            stream, token);
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.ComprobantesElectronicos.SingleAsync(x => x.Id == id, token);
        entity.IntentosAutorizacion++;
        entity.EstadoAutorizacion = "AUTORIZADO";
        entity.NumeroAutorizacion = result.NumeroAutorizacion;
        entity.FechaAutorizacion = result.FechaAutorizacion;
        entity.XmlAutorizadoAt = DateTime.UtcNow;
        entity.XmlAutorizadoReferencia = "AUTORIZADO";
        entity.FechaUltimaConsulta = DateTime.UtcNow;
        entity.EstadoComprobanteElectronicoId = await EstadoIdAsync(context,
            EstadosComprobanteElectronico.Autorizado, token);
        AgregarEventos(context, id, TiposEventoComprobanteElectronico.Autorizacion, result.Mensajes);
        Liberar(entity);
        await context.SaveChangesAsync(token);
    }

    private async Task RegistrarErrorTecnicoAsync(long id, Exception error,
        CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.ComprobantesElectronicos.SingleAsync(x => x.Id == id, token);
        entity.EstadoComprobanteElectronicoId = await EstadoIdAsync(context,
            EstadosComprobanteElectronico.ErrorTecnico, token);
        context.ComprobantesElectronicosEventos.Add(new()
        {
            ComprobanteElectronicoId = id,
            TipoEvento = TiposEventoComprobanteElectronico.Error,
            Codigo = error.GetType().Name,
            Mensaje = "Ocurrió un error técnico transitorio.",
            CreatedAt = DateTime.UtcNow
        });
        Liberar(entity);
        await context.SaveChangesAsync(token);
    }

    private async Task RegistrarErrorDefinitivoAsync(long id, Exception error,
        CancellationToken token)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(token);
        var entity = await context.ComprobantesElectronicos.SingleAsync(x => x.Id == id, token);
        entity.EstadoComprobanteElectronicoId = await EstadoIdAsync(context,
            EstadosComprobanteElectronico.Error, token);
        context.ComprobantesElectronicosEventos.Add(new()
        {
            ComprobanteElectronicoId = id,
            TipoEvento = TiposEventoComprobanteElectronico.Error,
            Codigo = error.GetType().Name,
            Mensaje = "El comprobante no superó una validación local definitiva.",
            CreatedAt = DateTime.UtcNow
        });
        Liberar(entity);
        await context.SaveChangesAsync(token);
    }

    private static void AgregarEventos(KontaxDbContext context, long id,
        string type, IReadOnlyList<MensajeSri> messages)
    {
        foreach (var message in messages)
            context.ComprobantesElectronicosEventos.Add(new()
            {
                ComprobanteElectronicoId = id,
                TipoEvento = type,
                Codigo = message.Codigo,
                Mensaje = message.Mensaje,
                InformacionAdicional = message.InformacionAdicional,
                CreatedAt = DateTime.UtcNow
            });
    }

    private static void Liberar(ComprobanteElectronico entity)
    {
        entity.ProcesamientoIniciadoAt = null;
        entity.ProcesadoPorInstalacionUuid = null;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task<long> EstadoIdAsync(KontaxDbContext context,
        string code, CancellationToken token) => await context.EstadosComprobanteElectronico
        .Where(x => x.Codigo == code && x.Estado == 1).Select(x => x.Id).SingleAsync(token);

    private sealed record ComprobanteSnapshot(
        long EmpresaId,
        string Ruc,
        string ClaveAcceso,
        int Ambiente,
        DateTime? XmlGeneradoAt,
        DateTime? XmlFirmadoAt,
        int IntentosEnvio,
        string? EstadoRecepcion);
}
