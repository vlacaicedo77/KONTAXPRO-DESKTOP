using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoCostoConfiguration
    : IEntityTypeConfiguration<ProductoCosto>
{
    public void Configure(
        EntityTypeBuilder<ProductoCosto> builder)
    {
        builder.ToTable(
            "productos_costos",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoId)
            .HasColumnName("producto_id")
            .IsRequired();

        builder.Property(x => x.UltimoPrecioCompra)
            .HasColumnName("ultimo_precio_compra")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.UltimoCostoEfectivo)
            .HasColumnName("ultimo_costo_efectivo")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CostoPromedio)
            .HasColumnName("costo_promedio")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CostoMaximoExistencia)
            .HasColumnName("costo_maximo_existencia")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
        .HasColumnName("created_at")
        .HasColumnType("timestamp without time zone")
        .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.ProductoId)
            .IsUnique();

        builder.HasOne(x => x.Producto)
            .WithOne(x => x.Costo)
            .HasForeignKey<ProductoCosto>(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}