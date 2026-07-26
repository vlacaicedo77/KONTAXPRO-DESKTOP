using System.Text;
using KONTAXPRO.Domain.Entities.Compras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class ComprasEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        bool updated = true) where TEntity : class
    {
        b.ToTable(table, "s_compras");
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

    internal static void Tax<TEntity>(EntityTypeBuilder<TEntity> b)
        where TEntity : class
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
            if (i > 0 && char.IsUpper(value[i])) result.Append('_');
            result.Append(char.ToLowerInvariant(value[i]));
        }
        return result.ToString();
    }
}

public sealed class DocumentoRecibidoSriConfiguration
    : IEntityTypeConfiguration<DocumentoRecibidoSri>
{
    public void Configure(EntityTypeBuilder<DocumentoRecibidoSri> b)
    {
        ComprasEf.Base(b, "documentos_recibidos_sri");
        b.ToTable("documentos_recibidos_sri", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_documentos_recibidos_sri_clasificacion",
                "clasificacion IS NULL OR clasificacion IN " +
                "('INVENTARIO', 'GASTO', 'ACTIVO', 'OTRO')");
            t.HasCheckConstraint("ck_documentos_recibidos_sri_estado",
                "estado_procesamiento IN ('PENDIENTE', 'PROCESADO', 'NO_APLICA')");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_documentos_recibidos_sri_id_empresa");
        b.Property(x => x.NumeroDocumento).HasMaxLength(64).IsRequired();
        b.Property(x => x.ClaveAcceso).HasMaxLength(49).IsRequired();
        b.Property(x => x.IdentificacionReceptor).HasMaxLength(20).IsRequired();
        b.Property(x => x.NumeroDocumentoModificado).HasMaxLength(64);
        b.Property(x => x.Clasificacion).HasMaxLength(16);
        b.Property(x => x.EstadoProcesamiento).HasMaxLength(16).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnType("date");
        b.Property(x => x.FechaAutorizacion)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.XmlObtenidoAt)
            .HasColumnType("timestamp with time zone");
        ComprasEf.Money(b, "ValorSinImpuestos", "Iva", "ImporteTotal");
        b.HasIndex(x => x.ClaveAcceso).IsUnique()
            .HasDatabaseName("ux_documentos_recibidos_sri_clave_acceso");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tercero).WithMany().HasForeignKey(x => x.TerceroId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> b)
    {
        ComprasEf.Base(b, "compras");
        b.ToTable("compras", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_compras_tipo",
                "tipo_compra IN ('FACTURADA', 'SIN_FACTURA')");
            t.HasCheckConstraint("ck_compras_documento",
                "(tipo_compra = 'FACTURADA' AND tipo_comprobante_id IS NOT NULL " +
                "AND numero_documento IS NOT NULL) OR " +
                "(tipo_compra = 'SIN_FACTURA')");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_compras_id_empresa");
        b.Property(x => x.TipoCompra).HasMaxLength(16).IsRequired();
        b.Property(x => x.NumeroDocumento).HasMaxLength(64);
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaEmision).HasColumnType("date");
        b.Property(x => x.FechaIngreso).HasColumnType("timestamp with time zone");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        ComprasEf.Money(b, "SubtotalSinImpuestos", "DescuentoTotal",
            "Subtotal", "ImpuestoTotal", "Total");
        b.HasIndex(x => x.DocumentoRecibidoSriId).IsUnique()
            .HasFilter("documento_recibido_sri_id IS NOT NULL")
            .HasDatabaseName("ux_compras_documento_recibido_sri");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento })
            .HasFilter("numero_documento IS NOT NULL")
            .HasDatabaseName("ix_compras_empresa_numero");
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
        b.HasOne(x => x.DocumentoRecibidoSri).WithMany()
            .HasForeignKey(x => new { x.DocumentoRecibidoSriId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraDetalleConfiguration
    : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> b)
    {
        ComprasEf.Base(b, "compras_detalles");
        b.ToTable("compras_detalles", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_compras_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0");
            t.HasCheckConstraint("ck_compras_detalles_producto",
                "(producto_presentacion_id IS NULL OR producto_id IS NOT NULL) " +
                "AND (bodega_id IS NULL OR producto_id IS NOT NULL)");
        });
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        ComprasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "PrecioUnitarioCompra", "DescuentoPorcentaje",
            "CostoUnitarioBase");
        ComprasEf.Money(b, "DescuentoValor", "CostoTotalLinea");
        b.HasOne(x => x.Compra).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.CompraId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany().HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<CompraDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<CompraDetalleImpuesto> b)
    {
        ComprasEf.Base(b, "compras_detalles_impuestos", false);
        ComprasEf.Tax(b);
        b.HasOne(x => x.CompraDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.CompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto).WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
