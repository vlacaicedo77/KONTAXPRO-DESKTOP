using KONTAXPRO.Domain.Entities.Ventas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class ProformaConfiguration : IEntityTypeConfiguration<Proforma>
{
    public void Configure(EntityTypeBuilder<Proforma> b)
    {
        VentasEf.Base(b, "proformas");
        b.ToTable("proformas", "s_ventas", t =>
            t.HasCheckConstraint("ck_proformas_estado",
                "estado IN ('ABIERTA', 'CONVERTIDA', 'ANULADA')"));
        b.Property(x => x.NumeroProforma).HasMaxLength(64).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(16).IsRequired();
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone");
        b.Property(x => x.FechaVigencia).HasColumnType("date");
        VentasEf.Money(b, "Subtotal", "DescuentoTotal", "ImpuestoTotal", "Total");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroProforma }).IsUnique()
            .HasDatabaseName("ux_proformas_empresa_numero");
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
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProformaDetalleConfiguration
    : IEntityTypeConfiguration<ProformaDetalle>
{
    public void Configure(EntityTypeBuilder<ProformaDetalle> b)
    {
        VentasEf.Base(b, "proformas_detalles");
        b.ToTable("proformas_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_proformas_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        VentasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "DescuentoPorcentaje");
        VentasEf.Money(b, "PrecioReferencia", "PrecioUnitario",
            "DescuentoValor", "PrecioFinalUnitario", "Subtotal");
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Proforma).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.ProformaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PrecioModificadoPorUsuario).WithMany()
            .HasForeignKey(x => x.PrecioModificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProformaDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<ProformaDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<ProformaDetalleImpuesto> b)
    {
        VentasEf.Base(b, "proformas_detalles_impuestos", false);
        VentasEf.TaxSnapshot(b);
        b.HasOne(x => x.ProformaDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.ProformaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto).WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DevolucionVentaConfiguration
    : IEntityTypeConfiguration<DevolucionVenta>
{
    public void Configure(EntityTypeBuilder<DevolucionVenta> b)
    {
        VentasEf.Documento(b, "devoluciones_ventas");
        b.Property(x => x.NumeroDevolucion).HasMaxLength(64).IsRequired();
        b.Property(x => x.FechaDevolucion)
            .HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDevolucion }).IsUnique()
            .HasDatabaseName("ux_devoluciones_ventas_empresa_numero");
        b.HasIndex(x => new { x.EmpresaId, x.TipoOrigenDevolucionVentaId,
            x.OrigenId }).HasDatabaseName("ix_devoluciones_ventas_origen");
        b.HasOne(x => x.TipoOrigenDevolucionVenta).WithMany()
            .HasForeignKey(x => x.TipoOrigenDevolucionVentaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DevolucionVentaDetalleConfiguration
    : IEntityTypeConfiguration<DevolucionVentaDetalle>
{
    public void Configure(EntityTypeBuilder<DevolucionVentaDetalle> b)
    {
        VentasEf.Base(b, "devoluciones_ventas_detalles");
        b.ToTable("devoluciones_ventas_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_devoluciones_ventas_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        VentasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "CostoUnitarioBase");
        VentasEf.Money(b, "CostoTotal");
        b.HasOne(x => x.DevolucionVenta).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.DevolucionVentaId)
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

public sealed class NotaCreditoConfiguration
    : IEntityTypeConfiguration<NotaCredito>
{
    public void Configure(EntityTypeBuilder<NotaCredito> b)
    {
        VentasEf.Documento(b, "notas_credito");
        b.Property(x => x.NumeroDocumento).HasMaxLength(32).IsRequired();
        b.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone");
        VentasEf.Money(b, "SubtotalSinImpuestos", "ImpuestoTotal");
        b.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.Secuencial })
            .IsUnique().HasDatabaseName("ux_notas_credito_emision_secuencial");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento }).IsUnique()
            .HasDatabaseName("ux_notas_credito_empresa_numero");
        b.HasOne(x => x.PuntoEmision).WithMany()
            .HasForeignKey(x => new { x.PuntoEmisionId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Factura).WithMany().HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DevolucionVenta).WithMany()
            .HasForeignKey(x => x.DevolucionVentaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaCreditoDetalleConfiguration
    : IEntityTypeConfiguration<NotaCreditoDetalle>
{
    public void Configure(EntityTypeBuilder<NotaCreditoDetalle> b)
    {
        VentasEf.Base(b, "notas_credito_detalles");
        VentasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase");
        VentasEf.Money(b, "PrecioUnitario", "DescuentoValor", "Subtotal");
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.NotaCredito).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.NotaCreditoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FacturaDetalle).WithMany()
            .HasForeignKey(x => x.FacturaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaCreditoDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<NotaCreditoDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<NotaCreditoDetalleImpuesto> b)
    {
        VentasEf.Base(b, "notas_credito_detalles_impuestos", false);
        VentasEf.TaxSnapshot(b);
        b.HasOne(x => x.NotaCreditoDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.NotaCreditoDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaDebitoConfiguration
    : IEntityTypeConfiguration<NotaDebito>
{
    public void Configure(EntityTypeBuilder<NotaDebito> b)
    {
        VentasEf.Documento(b, "notas_debito");
        b.Property(x => x.NumeroDocumento).HasMaxLength(32).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone");
        VentasEf.Money(b, "SubtotalSinImpuestos", "ImpuestoTotal");
        b.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.Secuencial })
            .IsUnique().HasDatabaseName("ux_notas_debito_emision_secuencial");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento }).IsUnique()
            .HasDatabaseName("ux_notas_debito_empresa_numero");
        b.HasOne(x => x.PuntoEmision).WithMany()
            .HasForeignKey(x => new { x.PuntoEmisionId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Factura).WithMany().HasForeignKey(x => x.FacturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaDebitoDetalleConfiguration
    : IEntityTypeConfiguration<NotaDebitoDetalle>
{
    public void Configure(EntityTypeBuilder<NotaDebitoDetalle> b)
    {
        VentasEf.Base(b, "notas_debito_detalles");
        VentasEf.Money(b, "Valor");
        b.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        b.ToTable("notas_debito_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_notas_debito_detalles_valor", "valor > 0"));
        b.HasOne(x => x.NotaDebito).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.NotaDebitoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaDebitoDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<NotaDebitoDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<NotaDebitoDetalleImpuesto> b)
    {
        VentasEf.Base(b, "notas_debito_detalles_impuestos", false);
        VentasEf.TaxSnapshot(b);
        b.HasOne(x => x.NotaDebitoDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.NotaDebitoDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
