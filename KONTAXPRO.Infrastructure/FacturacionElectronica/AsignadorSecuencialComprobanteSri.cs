using System.Data;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class AsignadorSecuencialComprobanteSri(
    IDbContextFactory<KontaxDbContext> dbContextFactory)
    : IAsignadorSecuencialComprobanteSri
{
    public async Task<int> AsignarAsync(
        long empresaId,
        long establecimientoId,
        long puntoEmisionId,
        long tipoComprobanteId,
        long tipoAmbienteId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);
        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            UPDATE s_configuracion.secuenciales_comprobantes AS s
               SET ultimo_secuencial = s.ultimo_secuencial + 1,
                   updated_at = CURRENT_TIMESTAMP
              FROM s_configuracion.puntos_emision AS p
              JOIN s_configuracion.establecimientos AS e
                ON e.id = p.establecimiento_id
             WHERE s.punto_emision_id = p.id
               AND s.punto_emision_id = @punto
               AND p.establecimiento_id = @establecimiento
               AND e.empresa_id = @empresa
               AND s.tipo_comprobante_id = @tipo
               AND s.tipo_ambiente_id = @ambiente
               AND s.ultimo_secuencial < 999999999
             RETURNING s.ultimo_secuencial;
            """;
        command.Parameters.Add(new NpgsqlParameter<long>("punto", puntoEmisionId));
        command.Parameters.Add(new NpgsqlParameter<long>("establecimiento", establecimientoId));
        command.Parameters.Add(new NpgsqlParameter<long>("empresa", empresaId));
        command.Parameters.Add(new NpgsqlParameter<long>("tipo", tipoComprobanteId));
        command.Parameters.Add(new NpgsqlParameter<long>("ambiente", tipoAmbienteId));
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
            throw new InvalidOperationException(
                "No existe un secuencial válido para la empresa, punto, comprobante y ambiente seleccionados.");
        await transaction.CommitAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }
}
