using System.Data;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Domain.Entities.Cartera;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace KONTAXPRO.Infrastructure.Compras;

internal static class CompraAccountingProcessor
{
    internal static async Task CreateAsync(
        KontaxDbContext context,
        Compra purchase,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (purchase.Id <= 0)
            throw new InvalidOperationException(
                "La compra debe persistirse antes de generar sus efectos contables.");
        var purchaseTotal = decimal.Round(purchase.Total, 2,
            MidpointRounding.AwayFromZero);
        if (purchaseTotal <= 0)
            throw new InvalidOperationException(
                "La compra debe tener un total mayor que cero para generar la cuenta por pagar.");

        var existingDebt = await context.CuentasPorPagar.AsNoTracking().AnyAsync(
            x => x.EmpresaId == purchase.EmpresaId &&
                 x.OrigenTipo == "COMPRA" && x.OrigenId == purchase.Id,
            cancellationToken);
        if (existingDebt)
            throw new InvalidOperationException(
                "La compra ya generó una cuenta por pagar.");

        var originType = await context.TiposOrigenAsiento.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Codigo == "COMPRA" && x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "No está disponible el tipo de origen contable COMPRA.");
        if (await context.Asientos.AsNoTracking().AnyAsync(x =>
                x.EmpresaId == purchase.EmpresaId &&
                x.TipoOrigenAsientoId == originType.Id &&
                x.OrigenId == purchase.Id, cancellationToken))
            throw new InvalidOperationException(
                "La compra ya fue contabilizada.");

        var debtMovementType = await context.TiposMovimientoCuentasPorPagar
            .AsNoTracking().SingleOrDefaultAsync(x =>
                x.Codigo == "ORIGEN_DEUDA" && x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "No está disponible el movimiento ORIGEN_DEUDA.");
        var period = await RequireOpenPeriodAsync(context, purchase.EmpresaId,
            purchase.FechaEmision, cancellationToken);
        var accounts = await RequireConfiguredAccountsAsync(context,
            purchase.EmpresaId, false, purchase.ImpuestoTotal > 0,
            cancellationToken);

        var baseByAccount = purchase.Detalles
            .Where(x => x.PrecioTotalSinImpuesto != 0)
            .GroupBy(x => x.CuentaContableId)
            .Select(x => new AccountDebit(x.Key,
                decimal.Round(x.Sum(y => y.PrecioTotalSinImpuesto), 2,
                    MidpointRounding.AwayFromZero)))
            .Where(x => x.Amount > 0)
            .ToList();
        if (baseByAccount.Count == 0)
            throw new InvalidOperationException(
                "La compra no contiene valores contabilizables en sus líneas.");

        var iva = decimal.Round(purchase.Detalles.SelectMany(x => x.Impuestos)
            .Where(x => x.CodigoImpuestoSri == "2")
            .Sum(x => x.ValorImpuesto), 2, MidpointRounding.AwayFromZero);
        var otherTaxes = decimal.Round(purchase.Detalles.SelectMany(x => x.Impuestos)
            .Where(x => x.CodigoImpuestoSri != "2")
            .Sum(x => x.ValorImpuesto), 2, MidpointRounding.AwayFromZero);
        if (otherTaxes > 0)
            throw new InvalidOperationException(
                "El comprobante contiene impuestos distintos de IVA. Configura su tratamiento contable antes de registrar la compra.");
        if ((purchase.DocumentoRecibidoSri?.Propina ?? 0) != 0)
            throw new InvalidOperationException(
                "El comprobante contiene propina. Configura su tratamiento contable antes de registrar la compra.");

        var debitTotal = baseByAccount.Sum(x => x.Amount) + iva;
        decimal roundingAdjustment;
        try
        {
            roundingAdjustment = CompraImportacionRules
                .ObtenerAjusteContableRedondeo(debitTotal, purchaseTotal);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                "El total del comprobante incluye cargos o diferencias que aún no tienen tratamiento contable. Revisa el XML antes de guardar.");
        }
        if (roundingAdjustment != 0)
        {
            var target = baseByAccount.OrderByDescending(x => x.Amount)
                .ThenBy(x => x.AccountId).First();
            target.Amount += roundingAdjustment;
            if (target.Amount <= 0)
                throw new InvalidOperationException(
                    "El ajuste de redondeo dejaría una cuenta contable sin valor positivo.");
        }

        var debt = new CuentaPorPagar
        {
            EmpresaId = purchase.EmpresaId,
            EmpresaTerceroId = purchase.EmpresaTerceroId,
            OrigenTipo = "COMPRA",
            OrigenId = purchase.Id,
            FechaOrigen = purchase.FechaEmision,
            FechaVencimiento = purchase.FechaVencimiento ?? purchase.FechaEmision,
            ValorOriginal = purchaseTotal,
            SaldoActual = purchaseTotal,
            Estado = "PENDIENTE",
            CreatedAt = now
        };
        debt.Movimientos.Add(new CuentaPorPagarMovimiento
        {
            Secuencia = 1,
            TipoMovimientoCuentaPorPagarId = debtMovementType.Id,
            OrigenTipo = "COMPRA",
            OrigenId = purchase.Id,
            Valor = purchaseTotal,
            SaldoAnterior = 0,
            SaldoNuevo = purchaseTotal,
            Descripcion = $"Origen de deuda por compra {PurchaseReference(purchase)}.",
            UsuarioId = purchase.UsuarioId,
            CreatedAt = now
        });
        context.CuentasPorPagar.Add(debt);

        var number = await NextEntryNumberAsync(context, purchase.EmpresaId,
            purchase.FechaEmision.Year, now, cancellationToken);
        var entry = new Asiento
        {
            EmpresaId = purchase.EmpresaId,
            PeriodoId = period.Id,
            UsuarioId = purchase.UsuarioId,
            NumeroAsiento = number,
            Fecha = purchase.FechaEmision,
            TipoAsiento = "AUTOMATICO",
            TipoOrigenAsientoId = originType.Id,
            OrigenId = purchase.Id,
            Concepto = $"Compra {PurchaseReference(purchase)}.",
            Estado = "CONTABILIZADO",
            CreatedAt = now
        };
        var order = 0;
        foreach (var debit in baseByAccount.OrderBy(x => x.AccountId))
            entry.Detalles.Add(new AsientoDetalle
            {
                EmpresaId = purchase.EmpresaId,
                CuentaContableId = debit.AccountId,
                EmpresaTerceroId = purchase.EmpresaTerceroId,
                Orden = ++order,
                Descripcion = "Base imponible de compra",
                Debe = debit.Amount,
                Haber = 0,
                CreatedAt = now
            });
        if (iva > 0)
            entry.Detalles.Add(new AsientoDetalle
            {
                EmpresaId = purchase.EmpresaId,
                CuentaContableId = accounts.IvaCreditAccountId ??
                    throw new InvalidOperationException(
                        "Falta configurar la cuenta contable IVA_CREDITO_TRIBUTARIO."),
                EmpresaTerceroId = purchase.EmpresaTerceroId,
                Orden = ++order,
                Descripcion = "IVA crédito tributario",
                Debe = iva,
                Haber = 0,
                CreatedAt = now
            });
        entry.Detalles.Add(new AsientoDetalle
        {
            EmpresaId = purchase.EmpresaId,
            CuentaContableId = accounts.PayableAccountId,
            EmpresaTerceroId = purchase.EmpresaTerceroId,
            Orden = ++order,
            Descripcion = "Cuenta por pagar a proveedor",
            Debe = 0,
            Haber = purchaseTotal,
            CreatedAt = now
        });
        if (!entry.EstaBalanceado())
            throw new InvalidOperationException(
                "El asiento automático de la compra no está balanceado.");
        context.Asientos.Add(entry);
    }

    internal static async Task ReverseAsync(
        KontaxDbContext context,
        Compra purchase,
        string reason,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var debt = await context.CuentasPorPagar
            .Include(x => x.Movimientos)
            .SingleOrDefaultAsync(x => x.EmpresaId == purchase.EmpresaId &&
                x.OrigenTipo == "COMPRA" && x.OrigenId == purchase.Id,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "La compra no tiene su cuenta por pagar asociada.");
        if (debt.Estado == "ANULADA")
            throw new InvalidOperationException(
                "La cuenta por pagar de la compra ya fue anulada.");
        if (debt.SaldoActual != debt.ValorOriginal ||
            debt.Movimientos.Any(x => x.Secuencia > 1))
            throw new InvalidOperationException(
                "La cuenta por pagar tiene pagos u otros movimientos. Revértelos antes de anular la compra.");

        var cancellationType = await context.TiposMovimientoCuentasPorPagar
            .AsNoTracking().SingleOrDefaultAsync(x =>
                x.Codigo == "ANULACION_DEUDA" && x.Estado == 1,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "No está disponible el movimiento ANULACION_DEUDA.");
        debt.Movimientos.Add(new CuentaPorPagarMovimiento
        {
            Secuencia = 2,
            TipoMovimientoCuentaPorPagarId = cancellationType.Id,
            OrigenTipo = "COMPRA",
            OrigenId = purchase.Id,
            Valor = debt.SaldoActual,
            SaldoAnterior = debt.SaldoActual,
            SaldoNuevo = 0,
            Descripcion = $"Anulación de deuda. {reason}",
            UsuarioId = purchase.AnuladoPorUsuarioId ?? purchase.UsuarioId,
            CreatedAt = now
        });
        debt.SaldoActual = 0;
        debt.Estado = "ANULADA";
        debt.UpdatedAt = now;

        var original = await context.Asientos
            .Include(x => x.Detalles)
            .SingleOrDefaultAsync(x => x.EmpresaId == purchase.EmpresaId &&
                x.OrigenId == purchase.Id &&
                x.TipoOrigenAsiento!.Codigo == "COMPRA",
                cancellationToken)
            ?? throw new InvalidOperationException(
                "La compra no tiene su asiento contable asociado.");
        if (original.Estado != "CONTABILIZADO" ||
            await context.Asientos.AsNoTracking().AnyAsync(x =>
                x.AsientoOrigenReversadoId == original.Id, cancellationToken))
            throw new InvalidOperationException(
                "El asiento de la compra ya fue anulado o revertido.");
        var reversalDate = EcuadorDate(now);
        var reversalPeriod = await RequireOpenPeriodAsync(context,
            original.EmpresaId, reversalDate, cancellationToken);

        var reverse = new Asiento
        {
            EmpresaId = original.EmpresaId,
            PeriodoId = reversalPeriod.Id,
            UsuarioId = purchase.AnuladoPorUsuarioId ?? purchase.UsuarioId,
            NumeroAsiento = await NextEntryNumberAsync(context,
                original.EmpresaId, reversalDate.Year, now, cancellationToken),
            Fecha = reversalDate,
            TipoAsiento = "AUTOMATICO",
            TipoOrigenAsientoId = original.TipoOrigenAsientoId,
            OrigenId = null,
            AsientoOrigenReversadoId = original.Id,
            Concepto = $"Reverso de {original.NumeroAsiento}. {reason}",
            Estado = "CONTABILIZADO",
            CreatedAt = now
        };
        foreach (var detail in original.Detalles.OrderBy(x => x.Orden))
            reverse.Detalles.Add(new AsientoDetalle
            {
                EmpresaId = original.EmpresaId,
                CuentaContableId = detail.CuentaContableId,
                EmpresaTerceroId = detail.EmpresaTerceroId,
                Orden = detail.Orden,
                Descripcion = $"Reverso: {detail.Descripcion}",
                Debe = detail.Haber,
                Haber = detail.Debe,
                CreatedAt = now
            });
        if (!reverse.EstaBalanceado())
            throw new InvalidOperationException(
                "El asiento de reverso de la compra no está balanceado.");
        original.Estado = "ANULADO";
        original.AnuladoPorUsuarioId = purchase.AnuladoPorUsuarioId;
        original.AnuladaAt = now;
        original.MotivoAnulacion = reason;
        original.UpdatedAt = now;
        context.Asientos.Add(reverse);
    }

    internal static async Task<long> RequireInventoryAccountAsync(
        KontaxDbContext context,
        long companyId,
        CancellationToken cancellationToken)
    {
        var accounts = await RequireConfiguredAccountsAsync(
            context, companyId, true, false, cancellationToken);
        return accounts.InventoryAccountId ??
            throw new InvalidOperationException(
                "Falta configurar la cuenta contable INVENTARIO.");
    }

    private static async Task<ConfiguredAccounts> RequireConfiguredAccountsAsync(
        KontaxDbContext context,
        long companyId,
        bool requireInventory,
        bool requireIva,
        CancellationToken cancellationToken)
    {
        var requiredCodes = new List<string> { "CUENTAS_POR_PAGAR" };
        if (requireInventory) requiredCodes.Add("INVENTARIO");
        if (requireIva) requiredCodes.Add("IVA_CREDITO_TRIBUTARIO");
        var configurations = await context.ConfiguracionCuentas.AsNoTracking()
            .Where(x => x.EmpresaId == companyId && x.Estado == 1 &&
                requiredCodes.Contains(x.TipoConfiguracionContable!.Codigo) &&
                x.CuentaContable!.EmpresaId == companyId &&
                x.CuentaContable.Estado == 1 &&
                x.CuentaContable.AceptaMovimientos)
            .Select(x => new
            {
                x.TipoConfiguracionContable!.Codigo,
                x.CuentaContableId
            }).ToListAsync(cancellationToken);
        var missing = requiredCodes.Where(code =>
            configurations.All(x => x.Codigo != code)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Falta configurar las cuentas contables: {string.Join(", ", missing)}.");
        return new ConfiguredAccounts(
            configurations.SingleOrDefault(x => x.Codigo == "INVENTARIO")
                ?.CuentaContableId,
            configurations.Single(x => x.Codigo == "CUENTAS_POR_PAGAR").CuentaContableId,
            configurations.SingleOrDefault(x => x.Codigo == "IVA_CREDITO_TRIBUTARIO")
                ?.CuentaContableId);
    }

    internal static async Task<PeriodoContable> RequireOpenPeriodAsync(
        KontaxDbContext context,
        long companyId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var period = await context.PeriodosContables.SingleOrDefaultAsync(x =>
            x.EmpresaId == companyId && x.Anio == date.Year &&
            x.Mes == date.Month, cancellationToken);
        if (period is null)
            throw new InvalidOperationException(
                $"No existe el período contable {date:yyyy-MM} para la empresa.");
        if (period.Estado != "ABIERTO" || date < period.FechaInicio ||
            date > period.FechaFin)
            throw new InvalidOperationException(
                $"El período contable {date:yyyy-MM} está cerrado o no contiene la fecha de la compra.");
        return period;
    }

    internal static async Task<string> NextEntryNumberAsync(
        KontaxDbContext context,
        long companyId,
        int year,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction()
            ?? throw new InvalidOperationException(
                "El secuencial contable requiere una transacción activa.");
        command.CommandText = """
            INSERT INTO s_contabilidad.secuenciales_asientos
                (empresa_id, anio, ultimo_secuencial, created_at, updated_at)
            VALUES (@empresa_id, @anio, 1, @now, @now)
            ON CONFLICT (empresa_id, anio)
            DO UPDATE SET
                ultimo_secuencial = s_contabilidad.secuenciales_asientos.ultimo_secuencial + 1,
                updated_at = EXCLUDED.updated_at
            RETURNING ultimo_secuencial;
            """;
        AddParameter(command, "empresa_id", companyId);
        AddParameter(command, "anio", year);
        AddParameter(command, "now", now);
        var value = await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "No fue posible asignar el secuencial del asiento.");
        var sequence = Convert.ToInt64(value);
        return $"ASI-{year}-{sequence:000000}";
    }

    private static void AddParameter(
        System.Data.Common.DbCommand command,
        string name,
        object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string PurchaseReference(Compra purchase) =>
        string.IsNullOrWhiteSpace(purchase.NumeroDocumento)
            ? purchase.Id.ToString()
            : purchase.NumeroDocumento;

    private static DateOnly EcuadorDate(DateTime utcNow)
    {
        var instant = utcNow.Kind == DateTimeKind.Utc
            ? utcNow
            : utcNow.ToUniversalTime();
        TimeZoneInfo zone;
        try
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById("America/Guayaquil");
        }
        catch (TimeZoneNotFoundException)
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(
                "SA Pacific Standard Time");
        }
        return DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(instant, zone));
    }

    private sealed record ConfiguredAccounts(
        long? InventoryAccountId,
        long PayableAccountId,
        long? IvaCreditAccountId);

    private sealed class AccountDebit(long accountId, decimal amount)
    {
        public long AccountId { get; } = accountId;
        public decimal Amount { get; set; } = amount;
    }
}
