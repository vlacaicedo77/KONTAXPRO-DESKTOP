using System.Security.Cryptography;
using System.Data;
using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConfiguracionEntity =
    KONTAXPRO.Domain.Entities.Configuracion.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ConfiguracionFacturacionElectronicaService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    IValidadorCertificadoSri validadorCertificado,
    IAlmacenamientoCertificadoSri almacenamientoCertificado,
    IOptions<SriEndpointsOptions> sriOptions,
    IContextoInstalacion contextoInstalacion,
    IFacturacionElectronicaWorkerScheduler workerScheduler,
    ILogger<ConfiguracionFacturacionElectronicaService> logger)
    : IConfiguracionFacturacionElectronicaService
{
    private const string Configurar = "SRI_CONFIGURAR_FACTURACION";
    private const string CambiarCertificado = "SRI_CAMBIAR_CERTIFICADO";
    private const string CambiarAmbiente = "SRI_CAMBIAR_AMBIENTE";
    private const string AdministrarSecuenciales =
        "SRI_ADMINISTRAR_SECUENCIALES";
    private const string Diagnosticar = "SRI_EJECUTAR_DIAGNOSTICO";
    private static readonly string[] PermisosLectura =
        [Configurar, CambiarCertificado,
            AdministrarSecuenciales, Diagnosticar];

    public async Task<ConfiguracionFacturacionElectronicaDto> ObtenerAsync(
        long empresaId,
        long usuarioId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await ValidarAlgunoDeLosPermisosAsync(context, empresaId, usuarioId,
            PermisosLectura, cancellationToken);
        return await ObtenerConfiguracionAsync(context, empresaId,
            cancellationToken);
    }

    public async Task<ConfiguracionFacturacionElectronicaDto>
        GuardarAmbienteAsync(
            long empresaId,
            long usuarioId,
            long? establecimientoId,
            long? puntoEmisionId,
            string ambienteCodigo,
            bool habilitada,
            uint versionEsperada,
            CancellationToken cancellationToken = default)
    {
        var codigo = CodigoAmbiente(ambienteCodigo);
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId, Configurar,
            cancellationToken);
        await BloquearEmpresaAsync(context, empresaId, cancellationToken);

        await ValidarContextoAutorizadoAsync(context, empresaId, usuarioId,
            establecimientoId, puntoEmisionId, cancellationToken);

        var ambiente = await context.TiposAmbiente.SingleAsync(
            x => x.Codigo == codigo && x.Estado == 1, cancellationToken);
        var emision = await context.TiposEmision.SingleAsync(
            x => x.Codigo == 1 && x.Estado == 1, cancellationToken);
        var config = await context.FacturacionesElectronicas
            .SingleOrDefaultAsync(x => x.EmpresaId == empresaId,
                cancellationToken);
        ValidarVersion(config, versionEsperada);

        var ambienteCambio = config is not null &&
                             config.TipoAmbienteId != ambiente.Id;
        if (ambienteCambio)
            await ValidarPermisoAsync(context, empresaId, usuarioId,
                CambiarAmbiente, cancellationToken);

        if (habilitada)
            await ValidarPreparacionParaHabilitarAsync(context, empresaId,
                usuarioId, establecimientoId, puntoEmisionId, ambiente.Id,
                config,
                cancellationToken);

        var now = DateTime.UtcNow;
        if (config is null)
        {
            config = new ConfiguracionEntity
            {
                EmpresaId = empresaId,
                CreatedAt = now
            };
            context.FacturacionesElectronicas.Add(config);
        }

        config.TipoAmbienteId = ambiente.Id;
        config.TipoEmisionId = emision.Id;
        config.Habilitada = habilitada;
        config.UpdatedAt = now;

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = usuarioId,
                EmpresaId = empresaId,
                Accion = ambienteCambio
                    ? "SRI_CAMBIO_AMBIENTE"
                    : "SRI_CAMBIO_CONFIGURACION",
                Entidad = "FACTURACION_ELECTRONICA",
                EntidadId = config.Id,
                Descripcion =
                    $"Ambiente {ambiente.Nombre}; habilitada: {habilitada}.",
                CreatedAt = now
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw ConflictoConcurrencia(exception);
        }

        return await ObtenerAsync(empresaId, usuarioId, cancellationToken);
    }

    public async Task<ConfiguracionFacturacionElectronicaDto>
        ImportarCertificadoAsync(
            long empresaId,
            long usuarioId,
            string nombreArchivo,
            ReadOnlyMemory<byte> pkcs12,
            string password,
            uint versionEsperada,
            CancellationToken cancellationToken = default)
    {
        if (!contextoInstalacion.EsServidor)
            throw new InvalidOperationException(
                "El certificado electrónico sólo puede configurarse desde el nodo SERVIDOR.");
        if (!string.Equals(Path.GetExtension(nombreArchivo), ".p12",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Path.GetExtension(nombreArchivo), ".pfx",
                StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Seleccione un certificado .p12 o .pfx.");
        if (pkcs12.IsEmpty || pkcs12.Length > 5 * 1024 * 1024)
            throw new ArgumentException(
                "El certificado debe contener información y no superar 5 MB.");

        await using (var readContext = await dbContextFactory
                         .CreateDbContextAsync(cancellationToken))
        {
            await ValidarPermisoAsync(readContext, empresaId, usuarioId,
                CambiarCertificado, cancellationToken);
            var current = await readContext.FacturacionesElectronicas
                .AsNoTracking().SingleOrDefaultAsync(
                    x => x.EmpresaId == empresaId, cancellationToken);
            ValidarVersion(current, versionEsperada);
            var ruc = await readContext.Empresas
                .Where(x => x.Id == empresaId && x.Estado == 1)
                .Select(x => x.NumeroIdentificacion)
                .SingleAsync(cancellationToken);
            var validation = await validadorCertificado.ValidarAsync(
                pkcs12, password, ruc, true, cancellationToken);
            if (!validation.EsValido || validation.Certificado is null)
                throw new InvalidOperationException(
                    string.Join(" ", validation.Errores));
        }

        var newReference = await almacenamientoCertificado.GuardarAsync(
            empresaId, pkcs12, password, cancellationToken);
        string? oldReference = null;
        try
        {
            await using var context = await dbContextFactory
                .CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database
                .BeginTransactionAsync(cancellationToken);
            await ValidarPermisoAsync(context, empresaId, usuarioId,
                CambiarCertificado, cancellationToken);
            await BloquearEmpresaAsync(context, empresaId, cancellationToken);
            var config = await context.FacturacionesElectronicas
                .SingleOrDefaultAsync(x => x.EmpresaId == empresaId,
                    cancellationToken);
            ValidarVersion(config, versionEsperada);
            config ??= await AsegurarConfiguracionAsync(context, empresaId,
                cancellationToken);
            oldReference = config.CertificadoReferencia;

            var validation = await validadorCertificado.ValidarAsync(
                pkcs12, password,
                await context.Empresas.Where(x => x.Id == empresaId)
                    .Select(x => x.NumeroIdentificacion)
                    .SingleAsync(cancellationToken), false,
                cancellationToken);
            if (!validation.EsValido || validation.Certificado is null)
                throw new InvalidOperationException(
                    string.Join(" ", validation.Errores));
            var info = validation.Certificado;
            config.CertificadoNombre = Path.GetFileName(nombreArchivo);
            config.CertificadoReferencia = newReference;
            config.CertificadoTitular = info.Titular;
            config.CertificadoEmisor = info.Emisor;
            config.CertificadoNumeroSerie = info.NumeroSerie;
            config.CertificadoFechaInicio = DateOnly.FromDateTime(
                info.ValidoDesde.DateTime);
            config.CertificadoFechaCaducidad = DateOnly.FromDateTime(
                info.ValidoHasta.DateTime);
            config.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = usuarioId,
                EmpresaId = empresaId,
                Accion = "SRI_CAMBIO_CERTIFICADO",
                Entidad = "FACTURACION_ELECTRONICA",
                EntidadId = config.Id,
                Descripcion =
                    "Se actualizó el certificado de firma electrónica.",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await EliminarSinOcultarErrorAsync(empresaId, newReference,
                CancellationToken.None);
            throw ConflictoConcurrencia(exception);
        }
        catch
        {
            await EliminarSinOcultarErrorAsync(empresaId, newReference,
                CancellationToken.None);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(oldReference) &&
            !string.Equals(oldReference, newReference,
                StringComparison.Ordinal))
            await EliminarSinOcultarErrorAsync(empresaId, oldReference,
                CancellationToken.None);

        return await ObtenerAsync(empresaId, usuarioId, cancellationToken);
    }

    public async Task<ResultadoDiagnosticoFacturacionElectronica>
        DiagnosticarAsync(
            long empresaId,
            long usuarioId,
            long? establecimientoId,
            long? puntoEmisionId,
            CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId, Diagnosticar,
            cancellationToken);
        return await CrearDiagnosticoAsync(context, empresaId,
            usuarioId, establecimientoId, puntoEmisionId, cancellationToken);
    }

    public async Task<IReadOnlyList<SecuencialComprobanteSriDto>>
        ObtenerNumeracionesAsync(
            long empresaId,
            long usuarioId,
            string ambienteCodigo,
            CancellationToken cancellationToken = default)
    {
        var codigoAmbiente = CodigoAmbiente(ambienteCodigo);
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);

        var ambienteId = await context.TiposAmbiente.AsNoTracking()
            .Where(x => x.Codigo == codigoAmbiente && x.Estado == 1)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        var establecimientosAutorizados =
            context.UsuariosEmpresasEstablecimientos.AsNoTracking()
                .Where(x => x.UsuarioEmpresa!.UsuarioId == usuarioId &&
                            x.UsuarioEmpresa.EmpresaId == empresaId &&
                            x.UsuarioEmpresa.Estado == 1)
                .Select(x => x.EstablecimientoId);

        var rows = await context.SecuencialesComprobantes.AsNoTracking()
            .Where(x => x.TipoAmbienteId == ambienteId &&
                        x.PuntoEmision!.Estado == 1 &&
                        x.PuntoEmision.Establecimiento!.EmpresaId == empresaId &&
                        x.PuntoEmision.Establecimiento.Estado == 1 &&
                        x.TipoComprobante!.Estado == 1 &&
                        establecimientosAutorizados.Contains(
                            x.PuntoEmision.EstablecimientoId))
            .Select(x => new
            {
                x.Id,
                EstablecimientoId = x.PuntoEmision!.EstablecimientoId,
                EstablecimientoCodigo =
                    x.PuntoEmision.Establecimiento!.Codigo,
                EstablecimientoNombre =
                    x.PuntoEmision.Establecimiento.Nombre,
                PuntoEmisionId = x.PuntoEmisionId,
                PuntoEmisionCodigo = x.PuntoEmision.Codigo,
                PuntoEmisionNombre = x.PuntoEmision.Nombre,
                x.TipoComprobanteId,
                TipoComprobanteCodigoSri =
                    x.TipoComprobante!.CodigoSri,
                TipoComprobanteNombre = x.TipoComprobante.Nombre,
                x.TipoAmbienteId,
                Ambiente = x.TipoAmbiente!.Nombre,
                x.UltimoSecuencial,
                TieneComprobantesKontax =
                    context.ComprobantesElectronicos.Any(c =>
                        c.EmpresaId == empresaId &&
                        c.PuntoEmisionId == x.PuntoEmisionId &&
                        c.TipoComprobanteId == x.TipoComprobanteId &&
                        c.TipoAmbienteId == x.TipoAmbienteId &&
                        c.Secuencial != null)
            })
            .OrderBy(x => x.EstablecimientoCodigo)
            .ThenBy(x => x.PuntoEmisionCodigo)
            .ThenBy(x => x.TipoComprobanteCodigoSri)
            .ToListAsync(cancellationToken);

        return rows.Select(x => new SecuencialComprobanteSriDto(
            x.Id, x.EstablecimientoId, x.EstablecimientoCodigo,
            x.EstablecimientoNombre, x.PuntoEmisionId,
            x.PuntoEmisionCodigo, x.PuntoEmisionNombre,
            x.TipoComprobanteId, x.TipoComprobanteCodigoSri,
            x.TipoComprobanteNombre, x.TipoAmbienteId, x.Ambiente,
            x.UltimoSecuencial, x.TieneComprobantesKontax)).ToArray();
    }

    public async Task ActualizarUltimoSecuencialAsync(
        long empresaId,
        long usuarioId,
        long secuencialComprobanteId,
        int ultimoSecuencialEsperado,
        int nuevoUltimoSecuencial,
        CancellationToken cancellationToken = default)
    {
        if (secuencialComprobanteId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(secuencialComprobanteId));
        if (ultimoSecuencialEsperado is < 0 or > 999_999_999)
            throw new ArgumentOutOfRangeException(
                nameof(ultimoSecuencialEsperado));
        if (nuevoUltimoSecuencial is < 0 or > 999_999_998)
            throw new ArgumentOutOfRangeException(
                nameof(nuevoUltimoSecuencial),
                "El último secuencial debe estar entre 0 y 999999998.");

        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);

        var sequenceQuery = context.SecuencialesComprobantes
            .Where(x => x.Id == secuencialComprobanteId);
        var sequence = context.Database.IsRelational()
            ? await context.SecuencialesComprobantes
                .FromSqlInterpolated($"""
                    SELECT *
                      FROM s_configuracion.secuenciales_comprobantes
                     WHERE id = {secuencialComprobanteId}
                     FOR UPDATE
                    """)
                .SingleOrDefaultAsync(cancellationToken)
            : await sequenceQuery.SingleOrDefaultAsync(cancellationToken);
        if (sequence is null)
            throw new InvalidOperationException(
                "La numeración seleccionada ya no existe.");

        var contextInfo = await context.PuntosEmision.AsNoTracking()
            .Where(x => x.Id == sequence.PuntoEmisionId && x.Estado == 1 &&
                        x.Establecimiento!.EmpresaId == empresaId &&
                        x.Establecimiento.Estado == 1 &&
                        context.UsuariosEmpresasEstablecimientos.Any(a =>
                            a.UsuarioEmpresa!.UsuarioId == usuarioId &&
                            a.UsuarioEmpresa.EmpresaId == empresaId &&
                            a.UsuarioEmpresa.Estado == 1 &&
                            a.EstablecimientoId == x.EstablecimientoId))
            .Select(x => new
            {
                EstablecimientoId = x.EstablecimientoId,
                EstablecimientoCodigo = x.Establecimiento!.Codigo,
                PuntoEmisionCodigo = x.Codigo
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "La numeración no pertenece a un establecimiento autorizado.");

        if (sequence.UltimoSecuencial != ultimoSecuencialEsperado)
            throw new InvalidOperationException(
                "La numeración fue modificada por otro usuario. Recarga los datos e inténtalo nuevamente.");
        if (sequence.UltimoSecuencial == nuevoUltimoSecuencial)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var tieneComprobantes = await context.ComprobantesElectronicos
            .AsNoTracking().AnyAsync(x =>
                    x.EmpresaId == empresaId &&
                    x.PuntoEmisionId == sequence.PuntoEmisionId &&
                    x.TipoComprobanteId == sequence.TipoComprobanteId &&
                    x.TipoAmbienteId == sequence.TipoAmbienteId &&
                    x.Secuencial != null,
                cancellationToken);
        if (tieneComprobantes)
            throw new InvalidOperationException(
                "KONTAXPRO ya emitió comprobantes con esta numeración. No puede modificarse desde la configuración inicial.");

        var tipo = await context.TiposComprobante.AsNoTracking()
            .Where(x => x.Id == sequence.TipoComprobanteId)
            .Select(x => x.Nombre)
            .SingleAsync(cancellationToken);
        var ambiente = await context.TiposAmbiente.AsNoTracking()
            .Where(x => x.Id == sequence.TipoAmbienteId)
            .Select(x => x.Nombre)
            .SingleAsync(cancellationToken);
        var anterior = sequence.UltimoSecuencial;
        sequence.UltimoSecuencial = nuevoUltimoSecuencial;
        sequence.UpdatedAt = DateTime.UtcNow;
        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            EmpresaId = empresaId,
            EstablecimientoId = contextInfo.EstablecimientoId,
            Accion = "SRI_CONFIGURACION_SECUENCIAL",
            Entidad = "SECUENCIAL_COMPROBANTE",
            EntidadId = sequence.Id,
            Descripcion =
                $"{ambiente}; establecimiento {contextInfo.EstablecimientoCodigo}; punto {contextInfo.PuntoEmisionCodigo}; {tipo}; último {anterior:D9} a {nuevoUltimoSecuencial:D9}.",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<AdministracionPuntosEmisionSriDto>
        ObtenerPuntosEmisionAsync(
            long empresaId,
            long usuarioId,
            CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);

        var establecimientosRows = await context
            .UsuariosEmpresasEstablecimientos.AsNoTracking()
            .Where(x => x.UsuarioEmpresa!.UsuarioId == usuarioId &&
                        x.UsuarioEmpresa.EmpresaId == empresaId &&
                        x.UsuarioEmpresa.Estado == 1 &&
                        x.Establecimiento!.EmpresaId == empresaId &&
                        x.Establecimiento.Estado == 1)
            .Select(x => new
            {
                Id = x.EstablecimientoId,
                x.Establecimiento!.Codigo,
                x.Establecimiento.Nombre
            })
            .OrderBy(x => x.Codigo)
            .ToListAsync(cancellationToken);
        var ids = establecimientosRows.Select(x => x.Id).ToArray();
        var predeterminadoId = await context.UsuariosConfiguracionesEmpresa
            .AsNoTracking()
            .Where(x => x.UsuarioId == usuarioId &&
                        x.EmpresaId == empresaId)
            .Select(x => x.PuntoEmisionId)
            .SingleOrDefaultAsync(cancellationToken);

        var puntosRows = await context.PuntosEmision.AsNoTracking()
            .Where(x => ids.Contains(x.EstablecimientoId))
            .Select(x => new
            {
                x.Id,
                x.EstablecimientoId,
                EstablecimientoCodigo = x.Establecimiento!.Codigo,
                x.Codigo,
                x.Nombre,
                x.Estado
            })
            .OrderBy(x => x.EstablecimientoCodigo)
            .ThenBy(x => x.Codigo)
            .ToListAsync(cancellationToken);
        var pointIds = puntosRows.Select(x => x.Id).ToArray();
        var usados = await context.ComprobantesElectronicos.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId &&
                        x.PuntoEmisionId.HasValue &&
                        pointIds.Contains(x.PuntoEmisionId.Value) &&
                        x.Secuencial != null)
            .Select(x => x.PuntoEmisionId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);
        var usadosSet = usados.ToHashSet();
        var establecimientos = establecimientosRows.Select(x =>
            new EstablecimientoPuntoEmisionSriDto(
                x.Id, x.Codigo, x.Nombre)).ToArray();
        var puntos = puntosRows.Select(x => new PuntoEmisionSriDto(
            x.Id, x.EstablecimientoId, x.EstablecimientoCodigo,
            x.Codigo, x.Nombre, x.Estado == 1,
            x.Id == predeterminadoId, usadosSet.Contains(x.Id))).ToArray();
        var plantillasRows = await (
                from tipo in context.TiposComprobante.AsNoTracking()
                where tipo.Estado == 1
                from ambiente in context.TiposAmbiente.AsNoTracking()
                where ambiente.Estado == 1
                orderby tipo.CodigoSri, ambiente.Codigo
                select new
                {
                    TipoComprobanteId = tipo.Id,
                    TipoComprobanteCodigoSri = tipo.CodigoSri,
                    TipoComprobanteNombre = tipo.Nombre,
                    TipoAmbienteId = ambiente.Id,
                    Ambiente = ambiente.Nombre
                })
            .ToListAsync(cancellationToken);
        var plantillas = plantillasRows.Select(x =>
            new PlantillaSecuencialPuntoEmisionSriDto(
                x.TipoComprobanteId, x.TipoComprobanteCodigoSri,
                x.TipoComprobanteNombre, x.TipoAmbienteId,
                x.Ambiente)).ToArray();

        return new(establecimientos, puntos, plantillas);
    }

    public async Task CrearPuntoEmisionAsync(
        long empresaId,
        long usuarioId,
        CrearPuntoEmisionSriDto solicitud,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        var establecimientoId = solicitud.EstablecimientoId;
        var codigo = NormalizarCodigoPunto(solicitud.Codigo);
        var nombre = NormalizarNombrePunto(solicitud.Nombre, codigo);
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);
        var establecimiento = await ObtenerEstablecimientoAutorizadoAsync(
            context, empresaId, usuarioId, establecimientoId,
            cancellationToken);

        if (await context.PuntosEmision.AnyAsync(x =>
                x.EstablecimientoId == establecimientoId &&
                x.Codigo == codigo, cancellationToken))
            throw new InvalidOperationException(
                $"El punto de emisión {codigo} ya existe en el establecimiento {establecimiento.Codigo}.");

        var tiposComprobante = await context.TiposComprobante
            .Where(x => x.Estado == 1)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var ambientes = await context.TiposAmbiente
            .Where(x => x.Estado == 1)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        if (tiposComprobante.Count == 0 || ambientes.Count == 0)
            throw new InvalidOperationException(
                "No existen catálogos activos para crear las numeraciones del punto de emisión.");

        var esperadas = tiposComprobante
            .SelectMany(tipo => ambientes.Select(ambiente =>
                (TipoComprobanteId: tipo, TipoAmbienteId: ambiente)))
            .ToHashSet();
        var iniciales = solicitud.SecuencialesIniciales ?? [];
        if (iniciales.Any(x => x.UltimoSecuencial is < 0 or > 999_999_998))
            throw new ArgumentOutOfRangeException(
                nameof(solicitud.SecuencialesIniciales),
                "Los últimos secuenciales deben estar entre 0 y 999999998.");
        var recibidas = iniciales.Select(x =>
                (x.TipoComprobanteId, x.TipoAmbienteId))
            .ToList();
        if (recibidas.Count != recibidas.Distinct().Count())
            throw new ArgumentException(
                "No se puede repetir un tipo de comprobante y ambiente en la numeración inicial.",
                nameof(solicitud));
        if (solicitud.ContinuarNumeracionExistente &&
            (recibidas.Count != esperadas.Count ||
             !recibidas.All(esperadas.Contains)))
            throw new ArgumentException(
                "Para continuar una numeración debes confirmar el último número de todos los comprobantes y ambientes.",
                nameof(solicitud));
        if (!solicitud.ContinuarNumeracionExistente &&
            iniciales.Any(x => x.UltimoSecuencial != 0))
            throw new ArgumentException(
                "Un punto nuevo debe iniciar todos sus secuenciales en cero.",
                nameof(solicitud));
        var valoresIniciales = iniciales.ToDictionary(
            x => (x.TipoComprobanteId, x.TipoAmbienteId),
            x => x.UltimoSecuencial);

        var now = DateTime.UtcNow;
        var punto = new PuntoEmision
        {
            EstablecimientoId = establecimientoId,
            Codigo = codigo,
            Nombre = nombre,
            Estado = 1,
            CreatedAt = now,
            UpdatedAt = now
        };
        context.PuntosEmision.Add(punto);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var tipoComprobanteId in tiposComprobante)
            foreach (var tipoAmbienteId in ambientes)
                context.SecuencialesComprobantes.Add(
                    new SecuencialComprobante
                    {
                        PuntoEmisionId = punto.Id,
                        TipoComprobanteId = tipoComprobanteId,
                        TipoAmbienteId = tipoAmbienteId,
                        UltimoSecuencial = solicitud
                            .ContinuarNumeracionExistente
                            ? valoresIniciales[(tipoComprobanteId,
                                tipoAmbienteId)]
                            : 0,
                        CreatedAt = now,
                        UpdatedAt = now
                    });

        if (solicitud.EstablecerPredeterminado)
            await AplicarPuntoPredeterminadoAsync(context, empresaId,
                usuarioId, establecimientoId, punto.Id, now,
                cancellationToken);

        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            EmpresaId = empresaId,
            EstablecimientoId = establecimientoId,
            Accion = "SRI_CREACION_PUNTO_EMISION",
            Entidad = "PUNTO_EMISION",
            EntidadId = punto.Id,
            Descripcion =
                solicitud.ContinuarNumeracionExistente
                    ? $"Establecimiento {establecimiento.Codigo}; punto {codigo}; continuidad configurada para {valoresIniciales.Count(x => x.Value > 0)} numeraciones con emisiones previas."
                    : $"Establecimiento {establecimiento.Codigo}; punto {codigo}; numeraciones inicializadas en cero.",
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RenombrarPuntoEmisionAsync(
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        var nuevoNombre = NormalizarNombrePuntoEditable(nombre);
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);
        var punto = await ObtenerPuntoAutorizadoAsync(context, empresaId,
            usuarioId, puntoEmisionId, false, cancellationToken);
        if (string.Equals(punto.Nombre, nuevoNombre,
                StringComparison.Ordinal))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        var anterior = punto.Nombre;
        var now = DateTime.UtcNow;
        punto.Nombre = nuevoNombre;
        punto.UpdatedAt = now;
        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            EmpresaId = empresaId,
            EstablecimientoId = punto.EstablecimientoId,
            Accion = "SRI_RENOMBRE_PUNTO_EMISION",
            Entidad = "PUNTO_EMISION",
            EntidadId = punto.Id,
            Descripcion =
                $"Punto {punto.Codigo}; nombre interno actualizado de '{anterior}' a '{nuevoNombre}'.",
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task EstablecerPuntoEmisionPredeterminadoAsync(
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);
        var punto = await ObtenerPuntoAutorizadoAsync(context, empresaId,
            usuarioId, puntoEmisionId, true, cancellationToken);
        var now = DateTime.UtcNow;
        await AplicarPuntoPredeterminadoAsync(context, empresaId, usuarioId,
            punto.EstablecimientoId, punto.Id, now, cancellationToken);
        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            EmpresaId = empresaId,
            EstablecimientoId = punto.EstablecimientoId,
            Accion = "SRI_PUNTO_EMISION_PREDETERMINADO",
            Entidad = "PUNTO_EMISION",
            EntidadId = punto.Id,
            Descripcion =
                $"Punto {punto.Codigo} seleccionado como preferido del usuario.",
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task CambiarEstadoPuntoEmisionAsync(
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        bool activar,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(
            cancellationToken);
        await using var transaction = await context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable,
                cancellationToken);
        await ValidarPermisoAsync(context, empresaId, usuarioId,
            AdministrarSecuenciales, cancellationToken);
        var punto = await ObtenerPuntoAutorizadoAsync(context, empresaId,
            usuarioId, puntoEmisionId, false, cancellationToken);
        var nuevoEstado = activar ? 1 : 0;
        if (punto.Estado == nuevoEstado)
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        PuntoEmision? reemplazo = null;
        if (!activar)
        {
            reemplazo = await context.PuntosEmision
                .Where(x => x.EstablecimientoId == punto.EstablecimientoId &&
                            x.Id != punto.Id && x.Estado == 1)
                .OrderBy(x => x.Codigo)
                .FirstOrDefaultAsync(cancellationToken);
            if (reemplazo is null)
                throw new InvalidOperationException(
                    "No se puede inactivar el único punto de emisión activo del establecimiento.");

            var preferencias = await context.UsuariosConfiguracionesEmpresa
                .Where(x => x.PuntoEmisionId == punto.Id &&
                            x.EstablecimientoId == punto.EstablecimientoId)
                .ToListAsync(cancellationToken);
            foreach (var preferencia in preferencias)
            {
                preferencia.PuntoEmisionId = reemplazo.Id;
                preferencia.UpdatedAt = DateTime.UtcNow;
            }
        }

        var now = DateTime.UtcNow;
        punto.Estado = nuevoEstado;
        punto.UpdatedAt = now;
        context.Auditorias.Add(new Auditoria
        {
            UsuarioId = usuarioId,
            EmpresaId = empresaId,
            EstablecimientoId = punto.EstablecimientoId,
            Accion = activar
                ? "SRI_ACTIVACION_PUNTO_EMISION"
                : "SRI_INACTIVACION_PUNTO_EMISION",
            Entidad = "PUNTO_EMISION",
            EntidadId = punto.Id,
            Descripcion = activar
                ? $"Punto {punto.Codigo} activado."
                : $"Punto {punto.Codigo} inactivado; preferencia reasignada a {reemplazo!.Codigo}.",
            CreatedAt = now
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<ResultadoDiagnosticoFacturacionElectronica>
        CrearDiagnosticoAsync(
            KontaxDbContext context,
            long empresaId,
            long usuarioId,
            long? establecimientoId,
            long? puntoEmisionId,
            CancellationToken cancellationToken)
    {
        var empresa = await context.Empresas.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == empresaId && x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "La empresa activa no existe o está inactiva.");
        var config = await context.FacturacionesElectronicas.AsNoTracking()
            .Include(x => x.TipoAmbiente)
            .Include(x => x.TipoEmision)
            .SingleOrDefaultAsync(x => x.EmpresaId == empresaId,
                cancellationToken);
        var (establecimientoValido, puntoValido) =
            await ValidarContextoAutorizadoAsync(context, empresaId,
                usuarioId, establecimientoId, puntoEmisionId,
                cancellationToken);
        var secuencialValido = puntoValido && config is not null &&
            await context.SecuencialesComprobantes.AsNoTracking().AnyAsync(x =>
                    x.PuntoEmisionId == puntoEmisionId!.Value &&
                    x.TipoAmbienteId == config.TipoAmbienteId &&
                    x.TipoComprobante!.CodigoSri == "01",
                cancellationToken);
        var rucValido = IdentificacionEcuadorValidator
            .Validate(IdentificacionEcuadorValidator.RucCodigo,
                empresa.NumeroIdentificacion).IsValid;
        var endpointsValidos = EndpointsValidos(
            config?.TipoAmbiente?.Codigo ?? 1);

        var items = new List<ItemDiagnosticoFacturacionElectronica>
        {
            Item("NODO", "Nodo servidor", contextoInstalacion.EsServidor,
                contextoInstalacion.EsServidor
                    ? "Repositorio autoritativo disponible"
                    : "Abra esta configuración en el equipo SERVIDOR"),
            Item("WORKER", "Procesamiento automático",
                contextoInstalacion.EsServidor &&
                workerScheduler.EstaEjecutandose,
                workerScheduler.EstaEjecutandose
                    ? "Worker activo"
                    : "Worker detenido"),
            Item("RUC", "RUC del emisor", rucValido,
                rucValido ? "13 dígitos válidos"
                    : "Debe contener 13 dígitos numéricos"),
            Item("ESTABLECIMIENTO", "Establecimiento activo",
                establecimientoValido,
                establecimientoValido ? "Establecimiento actual"
                    : "Seleccione un establecimiento activo"),
            Item("PUNTO_EMISION", "Punto de emisión activo", puntoValido,
                puntoValido ? "Punto actual"
                    : "Seleccione un punto del establecimiento actual"),
            Item("AMBIENTE", "Ambiente SRI",
                config?.TipoAmbiente is not null,
                config?.TipoAmbiente?.Nombre ?? "No configurado"),
            Item("EMISION", "Tipo de emisión Offline",
                config?.TipoEmision?.Codigo == 1,
                config?.TipoEmision?.Nombre ?? "No configurado"),
            Item("ENDPOINTS", "Servicios oficiales SRI", endpointsValidos,
                endpointsValidos ? "Dominios oficiales configurados"
                    : "Los endpoints no corresponden al ambiente SRI"),
            Item("SECUENCIAL", "Secuencial de factura", secuencialValido,
                secuencialValido ? "Disponible para el punto actual"
                    : "Falta configurar para el punto y ambiente actuales"),
            Item("HABILITADA", "Facturación electrónica habilitada",
                config?.Habilitada == true,
                config?.Habilitada == true ? "Habilitada" : "Deshabilitada")
        };
        items.AddRange(await DiagnosticarCertificadoAsync(empresaId,
            empresa.NumeroIdentificacion, config, cancellationToken));
        return new(items.All(x => x.Correcto), items);
    }

    private async Task<IReadOnlyList<ItemDiagnosticoFacturacionElectronica>>
        DiagnosticarCertificadoAsync(
            long empresaId,
            string ruc,
            ConfiguracionEntity? config,
        CancellationToken cancellationToken)
    {
        if (config?.CertificadoReferencia is null)
            return [Item("CERTIFICADO", "Certificado electrónico", false,
                "No configurado")];
        if (!contextoInstalacion.EsServidor)
            return [Item("CERTIFICADO", "Certificado electrónico", false,
                "Sólo puede verificarse en el nodo SERVIDOR")];
        try
        {
            var material = await almacenamientoCertificado.LeerAsync(
                empresaId, config.CertificadoReferencia, cancellationToken);
            try
            {
                var result = await validadorCertificado.ValidarAsync(
                    material.Pkcs12, material.Password, ruc, true,
                    cancellationToken);
                return CrearItemsCertificado(result);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(material.Pkcs12);
            }
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or CryptographicException or
            InvalidOperationException or ArgumentException)
        {
            logger.LogWarning(exception,
                "No fue posible validar el certificado SRI de la empresa {EmpresaId}.",
                empresaId);
            return [Item("CERTIFICADO", "Certificado electrónico", false,
                "No fue posible abrir la configuración segura del certificado")];
        }
    }

    private async Task ValidarPreparacionParaHabilitarAsync(
        KontaxDbContext context,
        long empresaId,
        long usuarioId,
        long? establecimientoId,
        long? puntoEmisionId,
        long tipoAmbienteId,
        ConfiguracionEntity? config,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        if (!contextoInstalacion.EsServidor)
            errors.Add("la habilitación debe realizarse en el nodo SERVIDOR");
        if (!workerScheduler.EstaEjecutandose)
            errors.Add("el procesamiento automático no está activo");

        var empresa = await context.Empresas.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == empresaId && x.Estado == 1,
                cancellationToken);
        if (empresa is null || !IdentificacionEcuadorValidator.Validate(
                IdentificacionEcuadorValidator.RucCodigo,
                empresa.NumeroIdentificacion).IsValid)
            errors.Add("el RUC del emisor no es válido");

        var (establishmentOk, pointOk) =
            await ValidarContextoAutorizadoAsync(context, empresaId,
                usuarioId, establecimientoId, puntoEmisionId,
                cancellationToken);
        if (!establishmentOk)
            errors.Add("no existe un establecimiento activo seleccionado");
        if (!pointOk)
            errors.Add("no existe un punto de emisión activo seleccionado");
        if (pointOk && !await context.SecuencialesComprobantes.AsNoTracking()
                .AnyAsync(x => x.PuntoEmisionId == puntoEmisionId!.Value &&
                               x.TipoAmbienteId == tipoAmbienteId &&
                               x.TipoComprobante!.CodigoSri == "01",
                    cancellationToken))
            errors.Add(
                "falta el secuencial de factura para el punto y ambiente seleccionados");

        if (!EndpointsValidos(await context.TiposAmbiente
                .Where(x => x.Id == tipoAmbienteId)
                .Select(x => x.Codigo).SingleAsync(cancellationToken)))
            errors.Add("los endpoints del ambiente no son oficiales");

        if (config?.CertificadoReferencia is null)
            errors.Add("no existe un certificado electrónico");
        else if (empresa is not null && contextoInstalacion.EsServidor)
        {
            var certificateItems = await DiagnosticarCertificadoAsync(empresaId,
                empresa.NumeroIdentificacion, config, cancellationToken);
            if (certificateItems.Any(x => !x.Correcto))
                errors.Add("el certificado electrónico no es válido o accesible");
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(
                "No se puede habilitar la facturación electrónica: " +
                string.Join("; ", errors) + ".");
    }

    private bool EndpointsValidos(int ambiente)
    {
        try
        {
            _ = sriOptions.Value.Recepcion(ambiente);
            _ = sriOptions.Value.Autorizacion(ambiente);
            return true;
        }
        catch (Exception exception) when (exception is UriFormatException or
            InvalidOperationException or ArgumentOutOfRangeException)
        {
            logger.LogWarning(exception,
                "La configuración de endpoints SRI no es válida para el ambiente {Ambiente}.",
                ambiente);
            return false;
        }
    }

    private static IReadOnlyList<ItemDiagnosticoFacturacionElectronica>
        CrearItemsCertificado(ResultadoValidacionCertificadoSri result)
    {
        if (result.Certificado is null)
            return [Item("CERTIFICADO", "Certificado electrónico", false,
                string.Join(" ", result.Errores))];

        var info = result.Certificado;
        var localOk = result.Estado is not (
            EstadoCertificadoSri.Caducado or
            EstadoCertificadoSri.AunNoVigente or
            EstadoCertificadoSri.SinClavePrivada or
            EstadoCertificadoSri.ArchivoOContrasenaInvalida or
            EstadoCertificadoSri.Error);
        var localDetail = localOk
            ? $"Vigente; {Math.Max(0, info.DiasParaCaducar)} días restantes"
            : string.Join(" ", result.Errores.Where(x =>
                !x.Contains("cadena", StringComparison.OrdinalIgnoreCase) &&
                !x.Contains("revoc", StringComparison.OrdinalIgnoreCase)));
        var localItem = Item("CERTIFICADO", "Certificado electrónico",
            localOk, localDetail);
        if (localOk && info.DiasParaCaducar <= 30)
            localItem = Advertencia(localItem,
                $"Vigente; próximo a caducar en {Math.Max(0, info.DiasParaCaducar)} días");

        var revocationItem = result.EstadoRevocacion switch
        {
            EstadoRevocacionCertificadoSri.NoRevocado => Item(
                "REVOCACION_CERTIFICADO", "Estado de revocación", true,
                "Comprobado: no consta como revocado"),
            EstadoRevocacionCertificadoSri.Revocado => Item(
                "REVOCACION_CERTIFICADO", "Estado de revocación", false,
                result.FechaRevocacion.HasValue
                    ? $"Revocado el {result.FechaRevocacion:dd/MM/yyyy HH:mm}"
                    : "Revocado; fecha no disponible"),
            EstadoRevocacionCertificadoSri.Desconocido => Advertencia(
                Item("REVOCACION_CERTIFICADO", "Estado de revocación", true,
                    "No comprobado"),
                result.Advertencias.FirstOrDefault() ??
                "CRL/OCSP no disponible; intenta verificar nuevamente"),
            _ => Advertencia(
                Item("REVOCACION_CERTIFICADO", "Estado de revocación",
                    true, "No comprobado"),
                result.Advertencias.FirstOrDefault() ??
                "No fue posible comprobar la revocación; intenta nuevamente")
        };

        return [localItem, revocationItem];
    }

    private static ItemDiagnosticoFacturacionElectronica Advertencia(
        ItemDiagnosticoFacturacionElectronica item,
        string detail) => item with
        {
            Detalle = detail,
            Nivel = "ADVERTENCIA"
        };

    private async Task EliminarSinOcultarErrorAsync(
        long empresaId,
        string reference,
        CancellationToken cancellationToken)
    {
        try
        {
            await almacenamientoCertificado.EliminarAsync(empresaId, reference,
                cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or
            UnauthorizedAccessException or InvalidOperationException)
        {
            logger.LogWarning(exception,
                "No fue posible limpiar material de certificado reemplazado de la empresa {EmpresaId}.",
                empresaId);
        }
    }

    private static async Task<(bool EstablecimientoValido, bool PuntoValido)>
        ValidarContextoAutorizadoAsync(
            KontaxDbContext context,
            long empresaId,
            long usuarioId,
            long? establecimientoId,
            long? puntoEmisionId,
            CancellationToken cancellationToken)
    {
        if (!establecimientoId.HasValue)
        {
            if (puntoEmisionId.HasValue)
                throw new InvalidOperationException(
                    "No se puede seleccionar un punto de emisión sin establecimiento.");
            return (false, false);
        }

        await ObtenerEstablecimientoAutorizadoAsync(context, empresaId,
            usuarioId, establecimientoId.Value, cancellationToken);
        if (!puntoEmisionId.HasValue) return (true, false);

        var puntoValido = await context.PuntosEmision.AsNoTracking().AnyAsync(
            x => x.Id == puntoEmisionId.Value &&
                 x.EstablecimientoId == establecimientoId.Value &&
                 x.Estado == 1,
            cancellationToken);
        if (!puntoValido)
            throw new InvalidOperationException(
                "El punto de emisión no pertenece al establecimiento autorizado o está inactivo.");
        return (true, true);
    }

    private static async Task BloquearEmpresaAsync(
        KontaxDbContext context,
        long empresaId,
        CancellationToken cancellationToken)
    {
        var empresa = context.Database.IsRelational()
            ? await context.Empresas.FromSqlInterpolated($"""
                    SELECT *
                      FROM s_configuracion.empresas
                     WHERE id = {empresaId}
                     FOR UPDATE
                    """).SingleOrDefaultAsync(cancellationToken)
            : await context.Empresas.SingleOrDefaultAsync(x => x.Id == empresaId,
                cancellationToken);
        if (empresa is null)
            throw new InvalidOperationException(
                "La empresa activa no existe.");
    }

    private static async Task<EstablecimientoAutorizado>
        ObtenerEstablecimientoAutorizadoAsync(
            KontaxDbContext context,
            long empresaId,
            long usuarioId,
            long establecimientoId,
            CancellationToken cancellationToken)
    {
        return await context.UsuariosEmpresasEstablecimientos.AsNoTracking()
            .Where(x => x.EstablecimientoId == establecimientoId &&
                        x.UsuarioEmpresa!.UsuarioId == usuarioId &&
                        x.UsuarioEmpresa.EmpresaId == empresaId &&
                        x.UsuarioEmpresa.Estado == 1 &&
                        x.Establecimiento!.EmpresaId == empresaId &&
                        x.Establecimiento.Estado == 1)
            .Select(x => new EstablecimientoAutorizado(
                x.EstablecimientoId, x.Establecimiento!.Codigo))
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new UnauthorizedAccessException(
                "El establecimiento no está autorizado para el usuario.");
    }

    private static async Task<PuntoEmision> ObtenerPuntoAutorizadoAsync(
        KontaxDbContext context,
        long empresaId,
        long usuarioId,
        long puntoEmisionId,
        bool exigirActivo,
        CancellationToken cancellationToken)
    {
        if (puntoEmisionId <= 0)
            throw new ArgumentOutOfRangeException(nameof(puntoEmisionId));
        var punto = context.Database.IsRelational()
            ? await context.PuntosEmision.FromSqlInterpolated($"""
                    SELECT *
                      FROM s_configuracion.puntos_emision
                     WHERE id = {puntoEmisionId}
                     FOR UPDATE
                    """).SingleOrDefaultAsync(cancellationToken)
            : await context.PuntosEmision.SingleOrDefaultAsync(
                x => x.Id == puntoEmisionId, cancellationToken);
        if (punto is null)
            throw new InvalidOperationException(
                "El punto de emisión seleccionado ya no existe.");
        await ObtenerEstablecimientoAutorizadoAsync(context, empresaId,
            usuarioId, punto.EstablecimientoId, cancellationToken);
        if (exigirActivo && punto.Estado != 1)
            throw new InvalidOperationException(
                "El punto de emisión seleccionado está inactivo.");
        return punto;
    }

    private static async Task AplicarPuntoPredeterminadoAsync(
        KontaxDbContext context,
        long empresaId,
        long usuarioId,
        long establecimientoId,
        long puntoEmisionId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var configuracion = await context.UsuariosConfiguracionesEmpresa
            .SingleOrDefaultAsync(x => x.UsuarioId == usuarioId &&
                                       x.EmpresaId == empresaId,
                cancellationToken);
        if (configuracion is null)
        {
            configuracion = new UsuarioConfiguracionEmpresa
            {
                UsuarioId = usuarioId,
                EmpresaId = empresaId,
                EstablecimientoId = establecimientoId,
                PuntoEmisionId = puntoEmisionId,
                CreatedAt = now,
                UpdatedAt = now
            };
            context.UsuariosConfiguracionesEmpresa.Add(configuracion);
            return;
        }

        if (configuracion.EstablecimientoId != establecimientoId)
            configuracion.BodegaId = null;
        configuracion.EstablecimientoId = establecimientoId;
        configuracion.PuntoEmisionId = puntoEmisionId;
        configuracion.UpdatedAt = now;
    }

    private static string NormalizarCodigoPunto(string value)
    {
        var codigo = (value ?? string.Empty).Trim();
        if (codigo.Length is < 1 or > 3 ||
            !codigo.All(char.IsAsciiDigit) ||
            !int.TryParse(codigo, out var numero) || numero is < 1 or > 999)
            throw new ArgumentException(
                "El punto de emisión debe ser un número entre 001 y 999.",
                nameof(value));
        return numero.ToString("D3",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string NormalizarNombrePunto(string value, string codigo)
    {
        var nombre = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(nombre))
            nombre = $"PUNTO DE EMISIÓN {codigo}";
        if (nombre.Length > 150)
            throw new ArgumentException(
                "El nombre del punto de emisión no puede superar 150 caracteres.",
                nameof(value));
        return nombre;
    }

    private static string NormalizarNombrePuntoEditable(string value)
    {
        var nombre = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException(
                "El nombre interno del punto de emisión es obligatorio.",
                nameof(value));
        if (nombre.Length > 150)
            throw new ArgumentException(
                "El nombre del punto de emisión no puede superar 150 caracteres.",
                nameof(value));
        return nombre;
    }

    private static int CodigoAmbiente(string value) =>
        string.Equals(value, "PRODUCCION",
            StringComparison.OrdinalIgnoreCase) ? 2 :
        string.Equals(value, "PRUEBAS",
            StringComparison.OrdinalIgnoreCase) ? 1 :
        throw new ArgumentException(
            "El ambiente debe ser PRUEBAS o PRODUCCION.");

    private static ItemDiagnosticoFacturacionElectronica Item(
        string code, string description, bool ok, string detail) =>
        new(code, description, ok, detail);

    private static void ValidarVersion(
        ConfiguracionEntity? config,
        uint expectedVersion)
    {
        if (config is null)
        {
            if (expectedVersion != 0) throw ConflictoConcurrencia();
            return;
        }
        if (config.Version != expectedVersion) throw ConflictoConcurrencia();
    }

    private static InvalidOperationException ConflictoConcurrencia(
        Exception? inner = null) => new(
        "La configuración SRI fue modificada por otro usuario. Se recargaron los datos vigentes; revisa los cambios e inténtalo nuevamente.",
        inner);

    private static async Task ValidarPermisoAsync(
        KontaxDbContext context,
        long empresaId,
        long usuarioId,
        string permission,
        CancellationToken cancellationToken) =>
        await ValidarAlgunoDeLosPermisosAsync(context, empresaId, usuarioId,
            [permission], cancellationToken);

    private static async Task ValidarAlgunoDeLosPermisosAsync(
        KontaxDbContext context,
        long empresaId,
        long usuarioId,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken)
    {
        var allowed = await context.UsuariosEmpresasRoles.AsNoTracking()
            .AnyAsync(x =>
                    x.UsuarioEmpresa!.EmpresaId == empresaId &&
                    x.UsuarioEmpresa.UsuarioId == usuarioId &&
                    x.UsuarioEmpresa.Estado == 1 &&
                    x.Rol!.Estado == 1 &&
                    x.Rol.RolesPermisos.Any(rp =>
                        rp.Permiso!.Estado == 1 &&
                        permissions.Contains(rp.Permiso.Codigo)),
                cancellationToken);
        if (!allowed)
            throw new UnauthorizedAccessException(
                "El usuario no tiene permisos para esta operación SRI en la empresa activa.");
    }

    private static async Task<ConfiguracionEntity> AsegurarConfiguracionAsync(
        KontaxDbContext context,
        long empresaId,
        CancellationToken cancellationToken)
    {
        var config = await context.FacturacionesElectronicas
            .SingleOrDefaultAsync(x => x.EmpresaId == empresaId,
                cancellationToken);
        if (config is not null) return config;
        var ambiente = await context.TiposAmbiente.SingleAsync(
            x => x.Codigo == 1 && x.Estado == 1, cancellationToken);
        var emision = await context.TiposEmision.SingleAsync(
            x => x.Codigo == 1 && x.Estado == 1, cancellationToken);
        config = new ConfiguracionEntity
        {
            EmpresaId = empresaId,
            TipoAmbienteId = ambiente.Id,
            TipoEmisionId = emision.Id,
            Habilitada = false,
            CreatedAt = DateTime.UtcNow
        };
        context.FacturacionesElectronicas.Add(config);
        return config;
    }

    private static async Task<ConfiguracionFacturacionElectronicaDto>
        ObtenerConfiguracionAsync(
            KontaxDbContext context,
            long empresaId,
            CancellationToken cancellationToken)
    {
        if (!await context.Empresas.AsNoTracking().AnyAsync(
                x => x.Id == empresaId && x.Estado == 1,
                cancellationToken))
            throw new InvalidOperationException(
                "La empresa activa no existe o está inactiva.");

        var config = await context.FacturacionesElectronicas.AsNoTracking()
            .Include(x => x.TipoAmbiente)
            .Include(x => x.TipoEmision)
            .SingleOrDefaultAsync(x => x.EmpresaId == empresaId,
                cancellationToken);
        if (config is null)
        {
            var ambiente = await context.TiposAmbiente.AsNoTracking()
                .SingleAsync(x => x.Codigo == 1 && x.Estado == 1,
                    cancellationToken);
            var emision = await context.TiposEmision.AsNoTracking()
                .SingleAsync(x => x.Codigo == 1 && x.Estado == 1,
                    cancellationToken);
            return new(empresaId, ambiente.Id, ambiente.Nombre,
                emision.Id, emision.Nombre, false, null, null, null,
                null, null, null, 0);
        }

        return new(config.EmpresaId, config.TipoAmbienteId,
            config.TipoAmbiente!.Nombre, config.TipoEmisionId,
            config.TipoEmision!.Nombre, config.Habilitada,
            config.CertificadoNombre, config.CertificadoTitular,
            config.CertificadoEmisor, config.CertificadoNumeroSerie,
            config.CertificadoFechaInicio,
            config.CertificadoFechaCaducidad, config.Version);
    }

    private sealed record EstablecimientoAutorizado(long Id, string Codigo);
}
