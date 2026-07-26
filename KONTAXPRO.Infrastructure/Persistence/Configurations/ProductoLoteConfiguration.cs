using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoLoteConfiguration
    : IEntityTypeConfiguration<ProductoLote>
{
    public void Configure(EntityTypeBuilder<ProductoLote> builder)
    {
        builder.ToTable(
            "productos_lotes",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoId)
            .HasColumnName("producto_id")
            .IsRequired();

        builder.Property(x => x.NumeroLote)
            .HasColumnName("numero_lote")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.FechaFabricacion)
            .HasColumnName("fecha_fabricacion");

        builder.Property(x => x.FechaCaducidad)
            .HasColumnName("fecha_caducidad");

        builder.Property(x => x.CostoUnitarioBase)
            .HasColumnName("costo_unitario_base")
            .HasPrecision(18, 6);

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion")
            .HasMaxLength(500);

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
            x.NumeroLote
        })
        .IsUnique();

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.Lotes)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
