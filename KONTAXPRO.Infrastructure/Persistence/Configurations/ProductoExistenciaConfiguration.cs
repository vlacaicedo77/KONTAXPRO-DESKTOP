using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoExistenciaConfiguration
    : IEntityTypeConfiguration<ProductoExistencia>
{
    public void Configure(
        EntityTypeBuilder<ProductoExistencia> builder)
    {
        builder.ToTable(
            "productos_existencias",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoId)
            .HasColumnName("producto_id")
            .IsRequired();

        builder.Property(x => x.BodegaId)
            .HasColumnName("bodega_id")
            .IsRequired();

        builder.Property(x => x.StockActual)
            .HasColumnName("stock_actual")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.StockReservado)
            .HasColumnName("stock_reservado")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone");

        builder.Ignore(x => x.StockDisponible);

        builder.HasIndex(x => new
        {
            x.ProductoId,
            x.BodegaId
        })
        .IsUnique();

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.Existencias)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bodega)
            .WithMany(x => x.ProductosExistencias)
            .HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}