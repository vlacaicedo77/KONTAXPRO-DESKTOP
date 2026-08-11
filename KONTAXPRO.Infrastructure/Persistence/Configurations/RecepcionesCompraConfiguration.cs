using KONTAXPRO.Domain.Entities.Compras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class ProveedorProductoEquivalenciaConfiguration
    : IEntityTypeConfiguration<ProveedorProductoEquivalencia>
{
    public void Configure(EntityTypeBuilder<ProveedorProductoEquivalencia> b)
    {
        ComprasEf.Base(b, "proveedores_productos_equivalencias");
        b.ToTable("proveedores_productos_equivalencias", "s_compras", t =>
            t.HasCheckConstraint("ck_proveedores_productos_equivalencias_tipo",
                "tipo_codigo IN ('PRINCIPAL', 'AUXILIAR')"));
        b.Property(x => x.CodigoProveedor).HasMaxLength(100).IsRequired();
        b.Property(x => x.CodigoProveedorNormalizado)
            .HasMaxLength(100).IsRequired();
        b.Property(x => x.TipoCodigo).HasMaxLength(16).IsRequired();
        b.Property(x => x.DescripcionOriginal).HasMaxLength(500);
        b.HasIndex(x => new
            {
                x.EmpresaId,
                x.TerceroId,
                x.TipoCodigo,
                x.CodigoProveedorNormalizado
            })
            .IsUnique()
            .HasDatabaseName("ux_proveedor_producto_equivalencia_codigo");
        b.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tercero).WithMany()
            .HasForeignKey(x => x.TerceroId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => new { x.ProductoPresentacionId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreadoPorUsuario).WithMany()
            .HasForeignKey(x => x.CreadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraRecepcionConfiguration
    : IEntityTypeConfiguration<CompraRecepcion>
{
    public void Configure(EntityTypeBuilder<CompraRecepcion> b)
    {
        ComprasEf.Base(b, "compras_recepciones");
        b.ToTable("compras_recepciones", "s_compras", t =>
            t.HasCheckConstraint("ck_compras_recepciones_estado",
                "estado IN ('CONFIRMADA', 'ANULADA')"));
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_compras_recepciones_id_empresa");
        b.HasAlternateKey(x => new { x.Id, x.CompraId, x.EmpresaId })
            .HasName("ak_compras_recepciones_id_compra_empresa");
        b.HasAlternateKey(x => new
            { x.Id, x.CompraId, x.BodegaId, x.EmpresaId })
            .HasName("ak_compras_recepciones_id_compra_bodega_empresa");
        b.Property(x => x.NumeroRecepcion).HasMaxLength(32).IsRequired();
        b.Property(x => x.FechaRecepcion)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.Estado).HasMaxLength(16).IsRequired();
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        b.HasIndex(x => x.OperacionUuid).IsUnique()
            .HasDatabaseName("ux_compras_recepciones_operacion_uuid");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroRecepcion }).IsUnique()
            .HasDatabaseName("ux_compras_recepciones_empresa_numero");
        b.HasIndex(x => x.MovimientoInventarioId).IsUnique()
            .HasFilter("movimiento_inventario_id IS NOT NULL")
            .HasDatabaseName("ux_compras_recepciones_movimiento");
        b.HasOne(x => x.Compra).WithMany(x => x.Recepciones)
            .HasForeignKey(x => new { x.CompraId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => new { x.BodegaId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladaPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoInventario).WithMany()
            .HasForeignKey(x => x.MovimientoInventarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraRecepcionDetalleConfiguration
    : IEntityTypeConfiguration<CompraRecepcionDetalle>
{
    public void Configure(EntityTypeBuilder<CompraRecepcionDetalle> b)
    {
        ComprasEf.Base(b, "compras_recepciones_detalles", false);
        b.ToTable("compras_recepciones_detalles", "s_compras", t =>
            t.HasCheckConstraint("ck_compras_recepciones_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 AND " +
                "cantidad_base > 0 AND costo_unitario_base >= 0 AND " +
                "costo_total >= 0"));
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_compras_recepciones_detalles_id_empresa");
        b.HasAlternateKey(x => new { x.Id, x.ProductoId, x.EmpresaId })
            .HasName("ak_compras_recepciones_detalles_id_producto_empresa");
        b.HasAlternateKey(x => new
            { x.Id, x.ProductoId, x.BodegaId, x.EmpresaId })
            .HasName("ak_compras_recepciones_detalles_id_producto_bodega_empresa");
        ComprasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "CostoUnitarioBase");
        b.Property(x => x.UltimoPrecioCompraAnterior).HasPrecision(18, 6);
        b.Property(x => x.UltimoCostoEfectivoAnterior).HasPrecision(18, 6);
        ComprasEf.Money(b, "CostoTotal");
        b.HasIndex(x => new { x.CompraRecepcionId, x.CompraDetalleId })
            .IsUnique()
            .HasDatabaseName("ux_compras_recepciones_detalles_compra_detalle");
        b.HasIndex(x => x.MovimientoInventarioDetalleId).IsUnique()
            .HasFilter("movimiento_inventario_detalle_id IS NOT NULL")
            .HasDatabaseName("ux_compras_recepciones_detalles_movimiento");
        b.HasOne(x => x.CompraRecepcion).WithMany(x => x.Detalles)
            .HasForeignKey(x => new
            {
                x.CompraRecepcionId,
                x.CompraId,
                x.BodegaId,
                x.EmpresaId
            })
            .HasPrincipalKey(x => new
                { x.Id, x.CompraId, x.BodegaId, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CompraDetalle).WithMany(x => x.RecepcionesDetalles)
            .HasForeignKey(x => new
            {
                x.CompraDetalleId,
                x.CompraId,
                x.EmpresaId
            })
            .HasPrincipalKey(x => new { x.Id, x.CompraId, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany()
            .HasForeignKey(x => new { x.ProductoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => new
            {
                x.ProductoPresentacionId,
                x.ProductoId,
                x.EmpresaId
            })
            .HasPrincipalKey(x => new { x.Id, x.ProductoId, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoInventarioDetalle).WithMany()
            .HasForeignKey(x => x.MovimientoInventarioDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraRecepcionDetalleLoteConfiguration
    : IEntityTypeConfiguration<CompraRecepcionDetalleLote>
{
    public void Configure(EntityTypeBuilder<CompraRecepcionDetalleLote> b)
    {
        ComprasEf.Base(b, "compras_recepciones_detalles_lotes", false);
        b.ToTable("compras_recepciones_detalles_lotes", "s_compras", t =>
            t.HasCheckConstraint("ck_compras_recepciones_detalles_lotes_cantidad",
                "cantidad_base > 0"));
        b.Property(x => x.NumeroLote).HasMaxLength(100).IsRequired();
        b.Property(x => x.CantidadBase).HasPrecision(18, 6).IsRequired();
        b.Property(x => x.FechaElaboracion).HasColumnType("date");
        b.Property(x => x.FechaCaducidad).HasColumnType("date");
        b.HasIndex(x => new { x.CompraRecepcionDetalleId, x.ProductoLoteId })
            .IsUnique()
            .HasDatabaseName("ux_compras_recepciones_detalles_lotes_lote");
        b.HasOne(x => x.CompraRecepcionDetalle).WithMany(x => x.Lotes)
            .HasForeignKey(x => new
            {
                x.CompraRecepcionDetalleId,
                x.ProductoId,
                x.EmpresaId
            })
            .HasPrincipalKey(x => new { x.Id, x.ProductoId, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoLote).WithMany()
            .HasForeignKey(x => new { x.ProductoLoteId, x.ProductoId })
            .HasPrincipalKey(x => new { x.Id, x.ProductoId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraRecepcionDetalleSerieConfiguration
    : IEntityTypeConfiguration<CompraRecepcionDetalleSerie>
{
    public void Configure(EntityTypeBuilder<CompraRecepcionDetalleSerie> b)
    {
        ComprasEf.Base(b, "compras_recepciones_detalles_series", false);
        b.Property(x => x.NumeroSerie).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.CompraRecepcionDetalleId, x.ProductoSerieId })
            .IsUnique()
            .HasDatabaseName("ux_compras_recepciones_detalles_series_serie");
        b.HasOne(x => x.CompraRecepcionDetalle).WithMany(x => x.Series)
            .HasForeignKey(x => new
            {
                x.CompraRecepcionDetalleId,
                x.ProductoId,
                x.BodegaId,
                x.EmpresaId
            })
            .HasPrincipalKey(x => new
                { x.Id, x.ProductoId, x.BodegaId, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoSerie).WithMany()
            .HasForeignKey(x => new
                { x.ProductoSerieId, x.ProductoId, x.BodegaId })
            .HasPrincipalKey(x => new { x.Id, x.ProductoId, x.BodegaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
