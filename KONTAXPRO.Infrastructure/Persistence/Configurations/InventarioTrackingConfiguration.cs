using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class ProductoLoteConfiguration
    : IEntityTypeConfiguration<ProductoLote>
{
    public void Configure(EntityTypeBuilder<ProductoLote> b)
    {
        b.ToTable("productos_lotes", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_productos_lotes_estado", "estado IN (0, 1)");
            t.HasCheckConstraint("ck_productos_lotes_fechas",
                "fecha_elaboracion IS NULL OR fecha_caducidad IS NULL OR " +
                "fecha_caducidad >= fecha_elaboracion");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.HasAlternateKey(x => new { x.Id, x.ProductoId })
            .HasName("ak_productos_lotes_id_producto");
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.NumeroLote).HasColumnName("numero_lote")
            .HasMaxLength(128).IsRequired();
        b.Property(x => x.FechaElaboracion).HasColumnName("fecha_elaboracion")
            .HasColumnType("date");
        b.Property(x => x.FechaCaducidad).HasColumnName("fecha_caducidad")
            .HasColumnType("date");
        b.Property(x => x.Observacion).HasColumnName("observacion")
            .HasMaxLength(500);
        InventarioEf.Estado(b); InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.ProductoId, x.NumeroLote }).IsUnique()
            .HasDatabaseName("ux_productos_lotes_producto_numero");
        b.HasOne(x => x.Producto).WithMany(x => x.Lotes)
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoLoteExistenciaConfiguration
    : IEntityTypeConfiguration<ProductoLoteExistencia>
{
    public void Configure(EntityTypeBuilder<ProductoLoteExistencia> b)
    {
        b.ToTable("productos_lotes_existencias", "s_inventario", t =>
            t.HasCheckConstraint("ck_productos_lotes_existencias_reservado",
                "stock_reservado >= 0"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.LoteId).HasColumnName("lote_id").IsRequired();
        b.Property(x => x.BodegaId).HasColumnName("bodega_id").IsRequired();
        b.Property(x => x.StockActual).HasColumnName("stock_actual")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.StockReservado).HasColumnName("stock_reservado")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(255);
        b.Ignore(x => x.StockDisponible); InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.LoteId, x.BodegaId }).IsUnique()
            .HasDatabaseName("ux_productos_lotes_existencias_lote_bodega");
        b.HasOne(x => x.Lote).WithMany(x => x.Existencias)
            .HasForeignKey(x => x.LoteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoSerieConfiguration
    : IEntityTypeConfiguration<ProductoSerie>
{
    public void Configure(EntityTypeBuilder<ProductoSerie> b)
    {
        b.ToTable("productos_series", "s_inventario");
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.HasAlternateKey(x => new { x.Id, x.ProductoId, x.BodegaId })
            .HasName("ak_productos_series_id_producto_bodega");
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.ProductoLoteId).HasColumnName("producto_lote_id");
        b.Property(x => x.BodegaId).HasColumnName("bodega_id").IsRequired();
        b.Property(x => x.NumeroSerie).HasColumnName("numero_serie")
            .HasMaxLength(150).IsRequired();
        b.Property(x => x.EstadoSerieId)
            .HasColumnName("estado_serie_id").IsRequired();
        b.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(255);
        b.Property(x => x.Observacion).HasColumnName("observacion")
            .HasMaxLength(500);
        InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.ProductoId, x.NumeroSerie }).IsUnique()
            .HasDatabaseName("ux_productos_series_producto_numero");
        b.HasOne(x => x.Producto).WithMany(x => x.Series)
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoLote).WithMany(x => x.Series)
            .HasForeignKey(x => new { x.ProductoLoteId, x.ProductoId })
            .HasPrincipalKey(x => new { x.Id, x.ProductoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EstadoSerie).WithMany()
            .HasForeignKey(x => x.EstadoSerieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MovimientoInventarioConfiguration
    : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> b)
    {
        b.ToTable("movimientos_inventario", "s_inventario");
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.NumeroMovimiento).HasColumnName("numero_movimiento")
            .HasMaxLength(64).IsRequired();
        b.Property(x => x.TipoMovimientoId)
            .HasColumnName("tipo_movimiento_id").IsRequired();
        b.Property(x => x.FechaMovimiento).HasColumnName("fecha_movimiento")
            .HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.BodegaId).HasColumnName("bodega_id").IsRequired();
        b.Property(x => x.OrigenTipoId).HasColumnName("origen_tipo_id").IsRequired();
        b.Property(x => x.OrigenId).HasColumnName("origen_id").IsRequired();
        b.Property(x => x.NumeroDocumento).HasColumnName("numero_documento")
            .HasMaxLength(64);
        b.Property(x => x.Referencia).HasColumnName("referencia")
            .HasMaxLength(255);
        b.Property(x => x.Observacion).HasColumnName("observacion")
            .HasMaxLength(1000);
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        b.Property(x => x.Estado).HasColumnName("estado")
            .HasMaxLength(24).IsRequired();
        b.Property(x => x.AnuladoPorUsuarioId)
            .HasColumnName("anulado_por_usuario_id");
        b.Property(x => x.AnuladoAt).HasColumnName("anulado_at")
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion")
            .HasMaxLength(500);
        b.Property(x => x.MovimientoReversoId)
            .HasColumnName("movimiento_reverso_id");
        InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.EmpresaId, x.NumeroMovimiento }).IsUnique()
            .HasDatabaseName("ux_movimientos_inventario_empresa_numero");
        b.HasIndex(x => x.MovimientoReversoId).IsUnique()
            .HasFilter("movimiento_reverso_id IS NOT NULL")
            .HasDatabaseName("ux_movimientos_inventario_reverso");
        b.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoMovimiento).WithMany()
            .HasForeignKey(x => x.TipoMovimientoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OrigenTipo).WithMany()
            .HasForeignKey(x => x.OrigenTipoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoReverso)
            .WithOne(x => x.MovimientoOrigenReversado)
            .HasForeignKey<MovimientoInventario>(x => x.MovimientoReversoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MovimientoInventarioDetalleConfiguration
    : IEntityTypeConfiguration<MovimientoInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<MovimientoInventarioDetalle> b)
    {
        b.ToTable("movimientos_inventario_detalles", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_movimientos_inventario_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0");
            t.HasCheckConstraint("ck_movimientos_inventario_detalles_costos",
                "costo_unitario_base >= 0 AND costo_total >= 0");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.MovimientoInventarioId)
            .HasColumnName("movimiento_inventario_id").IsRequired();
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.ProductoPresentacionId)
            .HasColumnName("producto_presentacion_id").IsRequired();
        Decimal6(b, x => x.CantidadPresentacion, "cantidad_presentacion");
        Decimal6(b, x => x.FactorConversion, "factor_conversion");
        Decimal6(b, x => x.CantidadBase, "cantidad_base");
        Decimal6(b, x => x.CostoUnitarioBase, "costo_unitario_base");
        Decimal2(b, x => x.CostoTotal, "costo_total");
        Decimal6(b, x => x.StockAnterior, "stock_anterior");
        Decimal6(b, x => x.StockNuevo, "stock_nuevo");
        Decimal6(b, x => x.CostoPromedioAnterior, "costo_promedio_anterior");
        Decimal6(b, x => x.CostoPromedioNuevo, "costo_promedio_nuevo");
        b.Property(x => x.EsBonificacion).HasColumnName("es_bonificacion")
            .IsRequired();
        b.Property(x => x.Observacion).HasColumnName("observacion")
            .HasMaxLength(500);
        InventarioEf.Timestamps(b);
        b.HasOne(x => x.MovimientoInventario).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.MovimientoInventarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany(x => x.MovimientosDetalles)
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    internal static void Decimal6<TEntity>(
        EntityTypeBuilder<TEntity> b,
        System.Linq.Expressions.Expression<Func<TEntity, decimal>> p,
        string column) where TEntity : class =>
        b.Property(p).HasColumnName(column).HasPrecision(18, 6).IsRequired();
    internal static void Decimal2<TEntity>(
        EntityTypeBuilder<TEntity> b,
        System.Linq.Expressions.Expression<Func<TEntity, decimal>> p,
        string column) where TEntity : class =>
        b.Property(p).HasColumnName(column).HasPrecision(18, 2).IsRequired();
}

public sealed class MovimientoInventarioDetalleLoteConfiguration
    : IEntityTypeConfiguration<MovimientoInventarioDetalleLote>
{
    public void Configure(EntityTypeBuilder<MovimientoInventarioDetalleLote> b)
    {
        b.ToTable("movimientos_inventario_detalles_lotes", "s_inventario");
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.MovimientoInventarioDetalleId)
            .HasColumnName("movimiento_inventario_detalle_id").IsRequired();
        b.Property(x => x.ProductoLoteId)
            .HasColumnName("producto_lote_id").IsRequired();
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.CantidadBase, "cantidad_base");
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.StockLoteAnterior, "stock_lote_anterior");
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.StockLoteNuevo, "stock_lote_nuevo");
        InventarioEf.Timestamps(b, false);
        b.HasOne(x => x.MovimientoInventarioDetalle).WithMany(x => x.Lotes)
            .HasForeignKey(x => x.MovimientoInventarioDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoLote).WithMany()
            .HasForeignKey(x => x.ProductoLoteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MovimientoInventarioDetalleSerieConfiguration
    : IEntityTypeConfiguration<MovimientoInventarioDetalleSerie>
{
    public void Configure(EntityTypeBuilder<MovimientoInventarioDetalleSerie> b)
    {
        b.ToTable("movimientos_inventario_detalles_series", "s_inventario");
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.MovimientoInventarioDetalleId)
            .HasColumnName("movimiento_inventario_detalle_id").IsRequired();
        b.Property(x => x.ProductoSerieId)
            .HasColumnName("producto_serie_id").IsRequired();
        InventarioEf.Timestamps(b, false);
        b.HasIndex(x => new
            { x.MovimientoInventarioDetalleId, x.ProductoSerieId })
            .IsUnique()
            .HasDatabaseName("ux_movimientos_inventario_detalles_series_par");
        b.HasOne(x => x.MovimientoInventarioDetalle).WithMany(x => x.Series)
            .HasForeignKey(x => x.MovimientoInventarioDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoSerie).WithMany()
            .HasForeignKey(x => x.ProductoSerieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
