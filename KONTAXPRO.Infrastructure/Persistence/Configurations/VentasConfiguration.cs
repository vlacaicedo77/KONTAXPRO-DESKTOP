using System.Text;
using KONTAXPRO.Domain.Entities.Ventas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class VentasEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        bool updated = true) where TEntity : class
    {
        // Las bases CLR sólo comparten estructura; cada documento es una tabla
        // independiente y no una jerarquía TPH/TPT.
        b.HasBaseType((Type?)null);
        b.ToTable(table, "s_ventas");
        b.HasKey("Id");
        b.Property<long>("Id").HasColumnName("id")
            .UseIdentityByDefaultColumn();

        foreach (var property in typeof(TEntity).GetProperties()
                     .Where(x => IsScalar(x.PropertyType)))
            b.Property(property.Name).HasColumnName(Snake(property.Name));

        if (typeof(TEntity).GetProperty("CreatedAt") is not null)
            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        if (updated && typeof(TEntity).GetProperty("UpdatedAt") is not null)
            b.Property<DateTime?>("UpdatedAt")
                .HasColumnType("timestamp with time zone");
    }

    internal static void Documento<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table) where TEntity : DocumentoVentaBase
    {
        Base(b, table);
        Money(b, "Subtotal", "DescuentoTotal", "Total");
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    internal static void DetalleComercial<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table) where TEntity : DetalleComercialBase
    {
        Base(b, table);
        Quantity(b, "CantidadPresentacion", "FactorConversion", "CantidadBase",
            "CostoUnitarioBase");
        Money(b, "PrecioReferencia", "PrecioUnitario", "DescuentoValor",
            "PrecioFinalUnitario", "Subtotal");
        b.Property(x => x.DescuentoPorcentaje).HasPrecision(18, 6);
        b.Property(x => x.OrigenFacturable).HasMaxLength(32).IsRequired();
        b.HasOne(x => x.Producto).WithMany()
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PrecioModificadoPorUsuario).WithMany()
            .HasForeignKey(x => x.PrecioModificadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    internal static void Quantity<TEntity>(
        EntityTypeBuilder<TEntity> b,
        params string[] names) where TEntity : class
    {
        foreach (var name in names)
            b.Property<decimal>(name).HasPrecision(18, 6).IsRequired();
    }

    internal static void Money<TEntity>(
        EntityTypeBuilder<TEntity> b,
        params string[] names) where TEntity : class
    {
        foreach (var name in names)
            b.Property<decimal>(name).HasPrecision(18, 2).IsRequired();
    }

    internal static void TaxSnapshot<TEntity>(
        EntityTypeBuilder<TEntity> b) where TEntity : class
    {
        b.Property<string>("CodigoImpuestoSri").HasMaxLength(16).IsRequired();
        b.Property<string>("CodigoPorcentajeSri").HasMaxLength(16).IsRequired();
        b.Property<string>("NombreImpuesto").HasMaxLength(150).IsRequired();
        b.Property<string>("TipoCalculo").HasMaxLength(16).IsRequired();
        b.Property<decimal?>("Porcentaje").HasPrecision(18, 6);
        b.Property<decimal?>("ValorEspecifico").HasPrecision(18, 6);
        b.Property<decimal>("BaseImponible").HasPrecision(18, 2).IsRequired();
        b.Property<decimal>("ValorImpuesto").HasPrecision(18, 2).IsRequired();
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) ||
               type == typeof(decimal) || type == typeof(DateTime) ||
               type == typeof(DateOnly) || type == typeof(Guid);
    }

    private static string Snake(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i]))
                result.Append('_');
            result.Append(char.ToLowerInvariant(value[i]));
        }
        return result.ToString();
    }
}

public sealed class VentaXfConfiguration : IEntityTypeConfiguration<VentaXf>
{
    public void Configure(EntityTypeBuilder<VentaXf> b)
    {
        VentasEf.Documento(b, "ventas_xf");
        b.Property(x => x.NumeroXf).HasMaxLength(64).IsRequired();
        b.Property(x => x.FechaVenta).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroXf }).IsUnique()
            .HasDatabaseName("ux_ventas_xf_empresa_numero");
    }
}

public sealed class VentaXfDetalleConfiguration
    : IEntityTypeConfiguration<VentaXfDetalle>
{
    public void Configure(EntityTypeBuilder<VentaXfDetalle> b)
    {
        VentasEf.DetalleComercial(b, "ventas_xf_detalles");
        b.ToTable("ventas_xf_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_ventas_xf_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        b.HasOne(x => x.VentaXf).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.VentaXfId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaEntregaConfiguration
    : IEntityTypeConfiguration<NotaEntrega>
{
    public void Configure(EntityTypeBuilder<NotaEntrega> b)
    {
        VentasEf.Documento(b, "notas_entrega");
        b.Property(x => x.NumeroNota).HasMaxLength(64).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroNota }).IsUnique()
            .HasDatabaseName("ux_notas_entrega_empresa_numero");
        b.HasOne(x => x.Proforma).WithMany()
            .HasForeignKey(x => x.ProformaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaEntregaDetalleConfiguration
    : IEntityTypeConfiguration<NotaEntregaDetalle>
{
    public void Configure(EntityTypeBuilder<NotaEntregaDetalle> b)
    {
        VentasEf.DetalleComercial(b, "notas_entrega_detalles");
        b.ToTable("notas_entrega_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_notas_entrega_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        b.HasOne(x => x.NotaEntrega).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.NotaEntregaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class NotaEntregaXfDetalleConfiguration
    : IEntityTypeConfiguration<NotaEntregaXfDetalle>
{
    public void Configure(EntityTypeBuilder<NotaEntregaXfDetalle> b)
    {
        VentasEf.Base(b, "notas_entrega_xf_detalles", false);
        VentasEf.Quantity(b, "CantidadBase");
        b.HasIndex(x => new { x.NotaEntregaDetalleId, x.VentaXfDetalleId })
            .IsUnique().HasDatabaseName("ux_notas_entrega_xf_detalles_par");
        b.HasOne(x => x.NotaEntregaDetalle).WithMany(x => x.OrigenesXf)
            .HasForeignKey(x => x.NotaEntregaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.VentaXfDetalle).WithMany(x => x.NotasEntrega)
            .HasForeignKey(x => x.VentaXfDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaConfiguration : IEntityTypeConfiguration<Factura>
{
    public void Configure(EntityTypeBuilder<Factura> b)
    {
        VentasEf.Documento(b, "facturas");
        b.ToTable("facturas", "s_ventas", t =>
            t.HasCheckConstraint("ck_facturas_origen",
                "origen_facturacion IN ('DIRECTA', 'DESDE_NOTA_ENTREGA', 'DESDE_XF')"));
        b.Property(x => x.NumeroDocumento).HasMaxLength(32).IsRequired();
        b.Property(x => x.OrigenFacturacion).HasMaxLength(32).IsRequired();
        b.Property(x => x.MotivoReceptorDistinto).HasMaxLength(500);
        b.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone");
        VentasEf.Money(b, "SubtotalSinImpuestos", "ImpuestoTotal");
        b.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.Secuencial })
            .IsUnique().HasDatabaseName("ux_facturas_emision_secuencial");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento }).IsUnique()
            .HasDatabaseName("ux_facturas_empresa_numero");
        b.HasOne(x => x.PuntoEmision).WithMany()
            .HasForeignKey(x => new { x.PuntoEmisionId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Proforma).WithMany()
            .HasForeignKey(x => x.ProformaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReceptorDistintoAutorizadoPorUsuario).WithMany()
            .HasForeignKey(x => x.ReceptorDistintoAutorizadoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaDetalleConfiguration
    : IEntityTypeConfiguration<FacturaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaDetalle> b)
    {
        VentasEf.DetalleComercial(b, "facturas_detalles");
        b.ToTable("facturas_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_facturas_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.Factura).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.FacturaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<FacturaDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<FacturaDetalleImpuesto> b)
    {
        VentasEf.Base(b, "facturas_detalles_impuestos", false);
        VentasEf.TaxSnapshot(b);
        b.HasOne(x => x.FacturaDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.FacturaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto).WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaNotaEntregaDetalleConfiguration
    : IEntityTypeConfiguration<FacturaNotaEntregaDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaNotaEntregaDetalle> b)
    {
        VentasEf.Base(b, "facturas_notas_entrega_detalles", false);
        VentasEf.Quantity(b, "CantidadBase");
        b.HasIndex(x => new { x.FacturaDetalleId, x.NotaEntregaDetalleId })
            .IsUnique().HasDatabaseName("ux_facturas_notas_entrega_detalles_par");
        b.HasOne(x => x.FacturaDetalle).WithMany(x => x.OrigenesNotaEntrega)
            .HasForeignKey(x => x.FacturaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.NotaEntregaDetalle).WithMany(x => x.Facturas)
            .HasForeignKey(x => x.NotaEntregaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaXfDetalleConfiguration
    : IEntityTypeConfiguration<FacturaXfDetalle>
{
    public void Configure(EntityTypeBuilder<FacturaXfDetalle> b)
    {
        VentasEf.Base(b, "facturas_xf_detalles", false);
        VentasEf.Quantity(b, "CantidadBase");
        b.HasIndex(x => new { x.FacturaDetalleId, x.VentaXfDetalleId })
            .IsUnique().HasDatabaseName("ux_facturas_xf_detalles_par");
        b.HasOne(x => x.FacturaDetalle).WithMany(x => x.OrigenesXf)
            .HasForeignKey(x => x.FacturaDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.VentaXfDetalle).WithMany(x => x.Facturas)
            .HasForeignKey(x => x.VentaXfDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturaFormaPagoConfiguration
    : IEntityTypeConfiguration<FacturaFormaPago>
{
    public void Configure(EntityTypeBuilder<FacturaFormaPago> b)
    {
        VentasEf.Base(b, "facturas_formas_pago", false);
        VentasEf.Money(b, "Valor");
        b.Property(x => x.CodigoFormaPagoSri).HasMaxLength(16).IsRequired();
        b.Property(x => x.UnidadTiempo).HasMaxLength(16);
        b.ToTable("facturas_formas_pago", "s_ventas", t =>
        {
            t.HasCheckConstraint("ck_facturas_formas_pago_valor", "valor > 0");
            t.HasCheckConstraint("ck_facturas_formas_pago_plazo",
                "plazo IS NULL OR plazo >= 0");
        });
        b.HasOne(x => x.Factura).WithMany(x => x.FormasPago)
            .HasForeignKey(x => x.FacturaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MedioPago).WithMany()
            .HasForeignKey(x => x.MedioPagoId).OnDelete(DeleteBehavior.Restrict);
    }
}
