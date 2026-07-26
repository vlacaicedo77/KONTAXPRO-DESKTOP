using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoPresentacionConfiguration
    : IEntityTypeConfiguration<ProductoPresentacion>
{
    public void Configure(
        EntityTypeBuilder<ProductoPresentacion> builder)
    {
        builder.ToTable(
            "productos_presentaciones",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoId)
            .HasColumnName("producto_id")
            .IsRequired();

        builder.Property(x => x.UnidadMedidaId)
            .HasColumnName("unidad_medida_id")
            .IsRequired();

        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.CodigoBarras)
            .HasColumnName("codigo_barras")
            .HasMaxLength(100);

        builder.Property(x => x.CodigoBarrasInterno)
            .HasColumnName("codigo_barras_interno")
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.FactorConversion)
            .HasColumnName("factor_conversion")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.EsPresentacionBase)
            .HasColumnName("es_presentacion_base")
            .IsRequired();

        builder.Property(x => x.PermiteCompra)
            .HasColumnName("permite_compra")
            .IsRequired();

        builder.Property(x => x.PermiteVenta)
            .HasColumnName("permite_venta")
            .IsRequired();

        builder.Property(x => x.Estado)
            .HasColumnName("estado")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => new
        {
            x.ProductoId,
            x.Codigo
        })
        .IsUnique();

        builder.HasIndex(x => x.CodigoBarras)
            .IsUnique()
            .HasFilter("codigo_barras IS NOT NULL");

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.Presentaciones)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.UnidadMedida)
            .WithMany()
            .HasForeignKey(x => x.UnidadMedidaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}