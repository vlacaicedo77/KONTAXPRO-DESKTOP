using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class EstadoComprobanteElectronicoService(
    IDbContextFactory<KontaxDbContext> dbContextFactory)
    : IEstadoComprobanteElectronicoService
{
    public async Task RegistrarResultadoAsync(
        long comprobanteElectronicoId,
        string tipoEvento,
        string? nuevoEstado,
        IReadOnlyCollection<MensajeSri> mensajes,
        string? codigo = null,
        string? mensaje = null,
        string? informacionAdicional = null,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);

        var comprobante = await context.ComprobantesElectronicos
            .Include(x => x.EstadoComprobanteElectronico)
            .SingleOrDefaultAsync(x => x.Id == comprobanteElectronicoId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "No se encontró el comprobante electrónico.");

        var now = DateTime.UtcNow;
        if (mensajes.Count == 0)
        {
            context.ComprobantesElectronicosEventos.Add(
                CrearEvento(comprobante.Id, tipoEvento, codigo, mensaje,
                    informacionAdicional, now));
        }
        else
        {
            // Cada mensaje del SRI se conserva como un evento independiente.
            foreach (var sriMessage in mensajes)
                context.ComprobantesElectronicosEventos.Add(
                    CrearEvento(comprobante.Id, tipoEvento,
                        sriMessage.Codigo, sriMessage.Mensaje,
                        sriMessage.InformacionAdicional, now));
        }

        if (!string.IsNullOrWhiteSpace(nuevoEstado))
        {
            var actual = comprobante.EstadoComprobanteElectronico!.Codigo;
            if (ComprobanteElectronico.PuedeCambiarEstado(actual, nuevoEstado))
            {
                var target = await context.EstadosComprobanteElectronico
                    .SingleAsync(x => x.Codigo == nuevoEstado && x.Estado == 1,
                        cancellationToken);
                comprobante.EstadoComprobanteElectronicoId = target.Id;
                comprobante.EstadoComprobanteElectronico = target;
                comprobante.UpdatedAt = now;
            }
            // Un error secundario se registra, pero AUTORIZADO permanece intacto.
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static ComprobanteElectronicoEvento CrearEvento(
        long comprobanteId,
        string tipoEvento,
        string? codigo,
        string? mensaje,
        string? informacionAdicional,
        DateTime createdAt) =>
        new()
        {
            ComprobanteElectronicoId = comprobanteId,
            TipoEvento = tipoEvento,
            Codigo = codigo,
            Mensaje = mensaje,
            InformacionAdicional = informacionAdicional,
            CreatedAt = createdAt
        };
}
