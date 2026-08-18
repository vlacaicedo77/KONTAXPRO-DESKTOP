using System.Data;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Models.Tesoreria;
using KONTAXPRO.Domain.Entities.Bancos;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Domain.Entities.Tesoreria;
using KONTAXPRO.Infrastructure.Compras;
using KONTAXPRO.Infrastructure.Inventory;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Tesoreria;

public sealed partial class OperacionSinSustentoService
{
    private const decimal ReversalTolerance = 0.000001m;

    public async Task<OperacionSinSustentoCatalogoDto> ListarAsync(
        OperacionSinSustentoCatalogoRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var authorizedEstablishments = context.UsuariosEmpresasEstablecimientos
            .AsNoTracking().Where(x => x.UsuarioEmpresa!.UsuarioId == request.UsuarioId &&
                x.UsuarioEmpresa.EmpresaId == request.EmpresaId &&
                x.UsuarioEmpresa.Estado == 1)
            .Select(x => x.EstablecimientoId);
        if (!await context.UsuariosEmpresasRoles.AsNoTracking().AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == request.UsuarioId &&
                x.UsuarioEmpresa.EmpresaId == request.EmpresaId &&
                x.UsuarioEmpresa.Estado == 1 && x.Rol!.Estado == 1 &&
                x.Rol.RolesPermisos.Any(rp => rp.Permiso!.Estado == 1 &&
                    rp.Permiso.Codigo == "TESORERIA_VER_SIN_SUSTENTO"),
                cancellationToken))
            throw new InvalidOperationException("No tienes permiso para consultar operaciones sin sustento.");
        var source = context.OperacionesSinSustento.AsNoTracking()
            .Where(x => x.EmpresaId == request.EmpresaId &&
                x.EstablecimientoId == request.EstablecimientoId &&
                authorizedEstablishments.Contains(x.EstablecimientoId));
        var result = await source.GroupBy(_ => 1).Select(g => new OperacionSinSustentoCatalogoDto
        {
            Confirmadas = g.Count(x => x.Estado == "CONFIRMADO"),
            Gastos = g.Count(x => x.Estado == "CONFIRMADO" && x.TipoOperacion == "GASTO"),
            Inventario = g.Count(x => x.Estado == "CONFIRMADO" && x.TipoOperacion == "INVENTARIO"),
            Anuladas = g.Count(x => x.Estado == "ANULADO"),
            TotalConfirmado = g.Where(x => x.Estado == "CONFIRMADO").Sum(x => x.Total)
        }).SingleOrDefaultAsync(cancellationToken) ?? new();
        var query = source;
        var search = request.Busqueda?.Trim().ToUpperInvariant();
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.NumeroOperacion.ToUpper().Contains(search) ||
                x.Beneficiario.ToUpper().Contains(search) || x.Motivo.ToUpper().Contains(search) ||
                (x.Referencia != null && x.Referencia.ToUpper().Contains(search)));
        if (request.Estado is "CONFIRMADO" or "ANULADO") query = query.Where(x => x.Estado == request.Estado);
        if (request.Tipo is "GASTO" or "INVENTARIO") query = query.Where(x => x.TipoOperacion == request.Tipo);
        result.Total = await query.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(request.TamanoPagina, 10, 200);
        var orderedQuery = (request.Orden, request.OrdenDescendente) switch
        {
            (OperacionSinSustentoCatalogoOrden.Operacion, false) => query
                .OrderBy(x => x.NumeroOperacion),
            (OperacionSinSustentoCatalogoOrden.Operacion, true) => query
                .OrderByDescending(x => x.NumeroOperacion),
            (OperacionSinSustentoCatalogoOrden.Beneficiario, false) => query
                .OrderBy(x => x.Beneficiario),
            (OperacionSinSustentoCatalogoOrden.Beneficiario, true) => query
                .OrderByDescending(x => x.Beneficiario),
            (OperacionSinSustentoCatalogoOrden.Fondo, false) => query
                .OrderBy(x => x.MedioSalida),
            (OperacionSinSustentoCatalogoOrden.Fondo, true) => query
                .OrderByDescending(x => x.MedioSalida),
            (OperacionSinSustentoCatalogoOrden.Total, false) => query
                .OrderBy(x => x.Total),
            (OperacionSinSustentoCatalogoOrden.Total, true) => query
                .OrderByDescending(x => x.Total),
            (OperacionSinSustentoCatalogoOrden.Estado, false) => query
                .OrderBy(x => x.Estado),
            (OperacionSinSustentoCatalogoOrden.Estado, true) => query
                .OrderByDescending(x => x.Estado),
            (OperacionSinSustentoCatalogoOrden.Fecha, false) => query
                .OrderBy(x => x.Fecha),
            _ => query.OrderByDescending(x => x.Fecha)
        };
        result.Items = await orderedQuery.ThenByDescending(x => x.Id)
            .Skip((Math.Max(1, request.Pagina) - 1) * pageSize).Take(pageSize)
            .Select(x => new OperacionSinSustentoItemDto
            {
                Id = x.Id, Numero = x.NumeroOperacion, Fecha = x.Fecha, Tipo = x.TipoOperacion,
                Beneficiario = x.Beneficiario, Motivo = x.Motivo, MedioSalida = x.MedioSalida,
                Total = x.Total, Estado = x.Estado, TieneEvidencia = x.EvidenciaRutaRelativa != null,
                Detalles = x.Detalles.Count
            }).ToListAsync(cancellationToken);
        return result;
    }

    public async Task<OperacionSinSustentoDetalleDto?> ObtenerAsync(
        long empresaId, long usuarioId, long id,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var establishmentId = await context.OperacionesSinSustento.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == id)
            .Select(x => (long?)x.EstablecimientoId)
            .SingleOrDefaultAsync(cancellationToken);
        if (!establishmentId.HasValue) return null;
        await RequirePermissionAsync(context, usuarioId, empresaId,
            establishmentId.Value, "TESORERIA_VER_SIN_SUSTENTO",
            cancellationToken);
        var detail = await context.OperacionesSinSustento.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == id)
            .Select(x => new OperacionSinSustentoDetalleDto
            {
                Id = x.Id, Numero = x.NumeroOperacion, Fecha = x.Fecha, Tipo = x.TipoOperacion,
                MedioSalida = x.MedioSalida, CajaSesionId = x.CajaSesionId,
                CuentaBancariaId = x.CuentaBancariaId, BodegaId = x.BodegaId,
                Beneficiario = x.Beneficiario, Motivo = x.Motivo, Referencia = x.Referencia,
                Total = x.Total, Estado = x.Estado, MotivoAnulacion = x.MotivoAnulacion,
                SustituyeAId = x.OperacionSustituidaId,
                SustituidaPorId = x.OperacionSustituta == null ? null : x.OperacionSustituta.Id,
                TieneEvidencia = x.EvidenciaRutaRelativa != null,
                EvidenciaNombre = x.EvidenciaNombre,
                Lineas = x.Detalles.OrderBy(d => d.Id).Select(d => new OperacionSinSustentoLineaDto
                {
                    CuentaContableId = d.CuentaContableId,
                    Cuenta = d.CuentaContable == null ? null : d.CuentaContable.Codigo + " · " + d.CuentaContable.Nombre,
                    ProductoId = d.ProductoId, ProductoPresentacionId = d.ProductoPresentacionId,
                    Producto = d.Producto == null ? null : d.Producto.Nombre + " · " + d.ProductoPresentacion!.Nombre,
                    Descripcion = d.Descripcion, Cantidad = d.CantidadPresentacion,
                    Factor = d.FactorConversion, CantidadBase = d.CantidadBase,
                    CostoUnitarioBase = d.CostoUnitarioBase, Total = d.CostoTotal
                }).ToList()
            }).SingleOrDefaultAsync(cancellationToken);
        if (detail is null || detail.Tipo != "INVENTARIO") return detail;

        var controls = await context.MovimientosInventarioDetalles.AsNoTracking()
            .Where(x => x.MovimientoInventario!.OrigenTipo!.Codigo ==
                    "OPERACION_SIN_SUSTENTO" &&
                x.MovimientoInventario.OrigenId == id &&
                x.MovimientoInventario.EmpresaId == empresaId &&
                x.MovimientoInventario.Estado == "CONFIRMADO")
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                Lotes = x.Lotes.Select(l => new IngresoInventarioLoteRequest
                {
                    NumeroLote = l.ProductoLote!.NumeroLote,
                    CantidadBase = l.CantidadBase,
                    FechaElaboracion = l.ProductoLote.FechaElaboracion,
                    FechaCaducidad = l.ProductoLote.FechaCaducidad
                }).ToList(),
                Series = x.Series.Select(s => new IngresoInventarioSerieRequest
                {
                    NumeroSerie = s.ProductoSerie!.NumeroSerie,
                    NumeroLote = s.ProductoSerie.ProductoLote == null
                        ? null : s.ProductoSerie.ProductoLote.NumeroLote
                }).ToList()
            }).ToListAsync(cancellationToken);
        for (var i = 0; i < Math.Min(detail.Lineas.Count, controls.Count); i++)
        {
            detail.Lineas[i].Lotes = controls[i].Lotes;
            detail.Lineas[i].Series = controls[i].Series;
        }
        return detail;
    }

    public async Task<SoporteSinSustentoContenidoDto> ObtenerEvidenciaAsync(
        long empresaId, long usuarioId, long operacionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var evidence = await context.OperacionesSinSustento.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Id == operacionId)
            .Select(x => new
            {
                x.EstablecimientoId, x.EvidenciaRutaRelativa,
                x.EvidenciaNombre, x.EvidenciaSha256, x.EvidenciaTamano
            }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("La operación no existe.");
        await RequirePermissionAsync(context, usuarioId, empresaId,
            evidence.EstablecimientoId, "TESORERIA_VER_SIN_SUSTENTO",
            cancellationToken);
        if (evidence.EvidenciaRutaRelativa is null || evidence.EvidenciaSha256 is null ||
            !evidence.EvidenciaTamano.HasValue)
            throw new InvalidOperationException("La operación no tiene evidencia adjunta.");
        return await soporteStorage.LeerVerificadoAsync(
            evidence.EvidenciaRutaRelativa,
            evidence.EvidenciaNombre ?? "evidencia",
            evidence.EvidenciaSha256, evidence.EvidenciaTamano.Value,
            cancellationToken);
    }

    public async Task<OperacionSinSustentoResult> AnularAsync(
        AnularOperacionSinSustentoRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Motivo) || request.Motivo.Trim().Length < 5)
            return OperacionSinSustentoResult.Fail("Indica un motivo de anulación de al menos 5 caracteres.");
        try
        {
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);
            var operation = await context.OperacionesSinSustento
                .FromSqlInterpolated($"SELECT * FROM s_tesoreria.operaciones_sin_sustento WHERE id = {request.OperacionId} AND empresa_id = {request.EmpresaId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);
            if (operation is null) return OperacionSinSustentoResult.Fail("La operación no existe.");
            await RequirePermissionAsync(context, request.UsuarioId, request.EmpresaId,
                operation.EstablecimientoId, "TESORERIA_ANULAR_SIN_SUSTENTO",
                cancellationToken);
            if (operation.Estado == "ANULADO") return OperacionSinSustentoResult.Ok(operation.Id, "La operación ya estaba anulada.");
            if (await context.OperacionesSinSustento.AnyAsync(x => x.OperacionSustituidaId == operation.Id, cancellationToken))
                return OperacionSinSustentoResult.Fail("La operación ya fue sustituida por una corrección.");
            await ReverseCoreAsync(context, operation, request.UsuarioId, request.Motivo.Trim(), cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return OperacionSinSustentoResult.Ok(operation.Id,
                $"Operación {operation.NumeroOperacion} anulada mediante movimientos reversos.");
        }
        catch (InvalidOperationException ex) { return OperacionSinSustentoResult.Fail(ex.Message); }
        catch (DbUpdateException) { return OperacionSinSustentoResult.Fail("La operación cambió simultáneamente. Actualiza el listado."); }
    }

    public async Task<OperacionSinSustentoResult> CorregirAsync(
        OperacionSinSustentoRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.OperacionSustituidaId.HasValue)
            return OperacionSinSustentoResult.Fail("Selecciona la operación que será corregida.");
        var validation = Application.Tesoreria.OperacionSinSustentoRules.Validate(request);
        if (validation is not null) return OperacionSinSustentoResult.Fail(validation);
        SoporteSinSustentoGuardadoDto? stored = null;
        try
        {
            if (request.EvidenciaContenido is { Length: > 0 })
                stored = await soporteStorage.GuardarAsync(request.EmpresaId, request.EvidenciaNombre ?? string.Empty,
                    request.EvidenciaContenido, cancellationToken);
            await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            var original = await context.OperacionesSinSustento
                .FromSqlInterpolated($"SELECT * FROM s_tesoreria.operaciones_sin_sustento WHERE id = {request.OperacionSustituidaId.Value} AND empresa_id = {request.EmpresaId} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken) ?? throw new InvalidOperationException("La operación original no existe.");
            if (original.Estado != "CONFIRMADO" || await context.OperacionesSinSustento.AnyAsync(
                    x => x.OperacionSustituidaId == original.Id, cancellationToken))
                throw new InvalidOperationException("La operación ya fue anulada o sustituida.");
            await RequirePermissionAsync(context, request.UsuarioId,
                request.EmpresaId, original.EstablecimientoId,
                "TESORERIA_CORREGIR_SIN_SUSTENTO", cancellationToken);
            var reusableSeries = await context.MovimientosInventarioDetallesSeries
                .AsNoTracking()
                .Where(x => x.MovimientoInventarioDetalle!.MovimientoInventarioId ==
                    original.MovimientoInventarioId)
                .Select(x => new
                {
                    x.ProductoSerieId,
                    x.ProductoSerie!.ProductoId,
                    x.ProductoSerie.NumeroSerie
                }).ToDictionaryAsync(x =>
                    $"{x.ProductoId}|{x.NumeroSerie.ToUpper()}",
                    x => x.ProductoSerieId, cancellationToken);
            await ReverseCoreAsync(context, original, request.UsuarioId, "Sustituida por corrección", cancellationToken);
            var result = await RegisterCoreAsync(context, request, stored,
                "TESORERIA_CORREGIR_SIN_SUSTENTO", reusableSeries,
                cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result with { Message = $"Corrección registrada; {original.NumeroOperacion} quedó sustituida." };
        }
        catch (InvalidOperationException ex) { await DeleteStoredAsync(stored); return OperacionSinSustentoResult.Fail(ex.Message); }
        catch (DbUpdateException) { await DeleteStoredAsync(stored); return OperacionSinSustentoResult.Fail("No se pudo corregir por concurrencia o integridad."); }
    }

    private static async Task ReverseCoreAsync(KontaxDbContext context, OperacionSinSustento operation,
        long userId, string reason, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (operation.MedioSalida == "CAJA") await ReverseCashAsync(context, operation, userId, reason, now, cancellationToken);
        else await ReverseBankAsync(context, operation, userId, reason, now, cancellationToken);
        if (operation.TipoOperacion == "INVENTARIO") await ReverseInventoryAsync(context, operation, userId, reason, now, cancellationToken);
        await ReverseEntryAsync(context, operation, userId, reason, now, cancellationToken);
        operation.Estado = "ANULADO"; operation.AnuladoPorUsuarioId = userId;
        operation.AnuladaAt = now; operation.MotivoAnulacion = reason; operation.UpdatedAt = now;
        context.Auditorias.Add(new Auditoria { UsuarioId = userId, EmpresaId = operation.EmpresaId,
            EstablecimientoId = operation.EstablecimientoId, Accion = "ANULAR_OPERACION_SIN_SUSTENTO",
            Entidad = "operaciones_sin_sustento", EntidadId = operation.Id, Descripcion = reason, CreatedAt = now });
    }

    private static async Task ReverseCashAsync(KontaxDbContext context, OperacionSinSustento operation,
        long userId, string reason, DateTime now, CancellationToken cancellationToken)
    {
        var original = await context.MovimientosCaja.Include(x => x.CajaSesion)
            .SingleAsync(x => x.Id == operation.MovimientoCajaId, cancellationToken);
        if (original.MovimientoReversoId.HasValue) throw new InvalidOperationException("El movimiento de caja ya fue revertido.");
        var targetSessionId = original.CajaSesion!.Estado == "ABIERTA"
            ? original.CajaSesionId
            : await context.CajasSesiones.AsNoTracking()
                .Where(x => x.CajaId == original.CajaSesion.CajaId &&
                    x.Estado == "ABIERTA")
                .Select(x => (long?)x.Id)
                .SingleOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException(
                    "La caja original está cerrada. Abre una sesión actual de esa misma caja para registrar el reverso sin alterar el cierre histórico.");
        var type = await context.TiposMovimientoCaja.SingleAsync(x => x.Codigo == "REVERSO_EGRESO" && x.Estado == 1, cancellationToken);
        var reverse = new MovimientoCaja { CajaSesionId = targetSessionId, UsuarioId = userId,
            TipoMovimientoCajaId = type.Id, OrigenTipo = "OPERACION_SIN_SUSTENTO", OrigenId = operation.Id,
            FechaMovimiento = now, Valor = original.Valor, Concepto = $"Reverso {operation.NumeroOperacion}. {reason}",
            Estado = "CONFIRMADO", CreatedAt = now };
        context.MovimientosCaja.Add(reverse); await context.SaveChangesAsync(cancellationToken);
        original.MovimientoReversoId = reverse.Id;
    }

    private static async Task ReverseBankAsync(KontaxDbContext context, OperacionSinSustento operation,
        long userId, string reason, DateTime now, CancellationToken cancellationToken)
    {
        var original = await context.MovimientosBancarios.SingleAsync(x => x.Id == operation.MovimientoBancarioId, cancellationToken);
        if (original.MovimientoReversoId.HasValue) throw new InvalidOperationException("El movimiento bancario ya fue revertido.");
        var type = await context.TiposMovimientoBancario.SingleAsync(x => x.Codigo == "REVERSO_SALIDA" && x.Estado == 1, cancellationToken);
        var reverse = new MovimientoBancario { CuentaBancariaId = original.CuentaBancariaId, UsuarioId = userId,
            TipoMovimientoBancarioId = type.Id, OrigenTipo = "OPERACION_SIN_SUSTENTO", OrigenId = operation.Id,
            FechaMovimiento = now, Valor = original.Valor, Referencia = operation.NumeroOperacion,
            Concepto = $"Reverso {operation.NumeroOperacion}. {reason}", Estado = "CONFIRMADO", CreatedAt = now };
        context.MovimientosBancarios.Add(reverse); await context.SaveChangesAsync(cancellationToken);
        original.MovimientoReversoId = reverse.Id;
    }

    private static async Task ReverseEntryAsync(KontaxDbContext context, OperacionSinSustento operation,
        long userId, string reason, DateTime now, CancellationToken cancellationToken)
    {
        var original = await context.Asientos.Include(x => x.Detalles)
            .SingleAsync(x => x.Id == operation.AsientoId && x.EmpresaId == operation.EmpresaId, cancellationToken);
        if (original.Estado != "CONTABILIZADO" || await context.Asientos.AnyAsync(x => x.AsientoOrigenReversadoId == original.Id, cancellationToken))
            throw new InvalidOperationException("El asiento contable ya fue revertido.");
        var date = DateOnly.FromDateTime(now.AddHours(-5));
        var period = await CompraAccountingProcessor.RequireOpenPeriodAsync(context, operation.EmpresaId, date, cancellationToken);
        var reverse = new Asiento { EmpresaId = operation.EmpresaId, PeriodoId = period.Id, UsuarioId = userId,
            NumeroAsiento = await CompraAccountingProcessor.NextEntryNumberAsync(context, operation.EmpresaId, date.Year, now, cancellationToken),
            Fecha = date, TipoAsiento = "AUTOMATICO", TipoOrigenAsientoId = original.TipoOrigenAsientoId,
            AsientoOrigenReversadoId = original.Id, Concepto = $"Reverso de {original.NumeroAsiento}. {reason}",
            Estado = "CONTABILIZADO", CreatedAt = now };
        foreach (var d in original.Detalles.OrderBy(x => x.Orden)) reverse.Detalles.Add(new AsientoDetalle
        { EmpresaId = operation.EmpresaId, CuentaContableId = d.CuentaContableId, Orden = d.Orden,
            Descripcion = $"Reverso: {d.Descripcion}", Debe = d.Haber, Haber = d.Debe, CreatedAt = now });
        if (!reverse.EstaBalanceado()) throw new InvalidOperationException("El asiento reverso no está balanceado.");
        context.Asientos.Add(reverse); original.Estado = "ANULADO"; original.AnuladoPorUsuarioId = userId;
        original.AnuladaAt = now; original.MotivoAnulacion = reason; original.UpdatedAt = now;
    }
}
