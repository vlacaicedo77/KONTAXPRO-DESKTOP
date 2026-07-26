using KONTAXPRO.Domain.Entities.Compras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class LiquidacionCompraConfiguration
    : IEntityTypeConfiguration<LiquidacionCompra>
{
    public void Configure(EntityTypeBuilder<LiquidacionCompra> b)
    {
        ComprasEf.Base(b, "liquidaciones_compra");
        b.Property(x => x.NumeroDocumento).HasMaxLength(32).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaEmision).HasColumnType("date");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        ComprasEf.Money(b, "SubtotalSinImpuestos", "DescuentoTotal",
            "Subtotal", "ImpuestoTotal", "Total");
        b.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.Secuencial })
            .IsUnique().HasDatabaseName(
                "ux_liquidaciones_compra_emision_secuencial");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento }).IsUnique()
            .HasDatabaseName("ux_liquidaciones_compra_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PuntoEmision).WithMany()
            .HasForeignKey(x => new { x.PuntoEmisionId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LiquidacionCompraDetalleConfiguration
    : IEntityTypeConfiguration<LiquidacionCompraDetalle>
{
    public void Configure(EntityTypeBuilder<LiquidacionCompraDetalle> b)
    {
        ComprasEf.Base(b, "liquidaciones_compra_detalles");
        b.ToTable("liquidaciones_compra_detalles", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_liquidaciones_compra_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0");
            t.HasCheckConstraint("ck_liquidaciones_compra_detalles_producto",
                "(producto_presentacion_id IS NULL OR producto_id IS NOT NULL) " +
                "AND (bodega_id IS NULL OR producto_id IS NOT NULL)");
        });
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        ComprasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "PrecioUnitario", "DescuentoPorcentaje",
            "CostoUnitarioBase");
        ComprasEf.Money(b, "DescuentoValor", "Subtotal", "CostoTotalLinea");
        b.HasOne(x => x.LiquidacionCompra).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.LiquidacionCompraId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany().HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LiquidacionCompraDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<LiquidacionCompraDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<LiquidacionCompraDetalleImpuesto> b)
    {
        ComprasEf.Base(b, "liquidaciones_compra_detalles_impuestos", false);
        ComprasEf.Tax(b);
        b.HasOne(x => x.LiquidacionCompraDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.LiquidacionCompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto).WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DevolucionCompraConfiguration
    : IEntityTypeConfiguration<DevolucionCompra>
{
    public void Configure(EntityTypeBuilder<DevolucionCompra> b)
    {
        ComprasEf.Base(b, "devoluciones_compras");
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_devoluciones_compras_id_empresa");
        b.Property(x => x.NumeroDevolucion).HasMaxLength(64).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaDevolucion)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        ComprasEf.Money(b, "Subtotal", "Total");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDevolucion }).IsUnique()
            .HasDatabaseName("ux_devoluciones_compras_empresa_numero");
        b.HasIndex(x => new { x.EmpresaId, x.TipoOrigenDevolucionCompraId,
            x.OrigenId }).HasDatabaseName("ix_devoluciones_compras_origen");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoOrigenDevolucionCompra).WithMany()
            .HasForeignKey(x => x.TipoOrigenDevolucionCompraId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DevolucionCompraDetalleConfiguration
    : IEntityTypeConfiguration<DevolucionCompraDetalle>
{
    public void Configure(EntityTypeBuilder<DevolucionCompraDetalle> b)
    {
        ComprasEf.Base(b, "devoluciones_compras_detalles");
        b.ToTable("devoluciones_compras_detalles", "s_compras", t =>
            t.HasCheckConstraint("ck_devoluciones_compras_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        ComprasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "CostoUnitarioBase");
        ComprasEf.Money(b, "CostoTotal");
        b.HasOne(x => x.DevolucionCompra).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.DevolucionCompraId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany().HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
