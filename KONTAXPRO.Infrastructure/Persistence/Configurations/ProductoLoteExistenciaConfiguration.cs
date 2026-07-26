using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoLoteExistenciaConfiguration
    : IEntityTypeConfiguration<ProductoLoteExistencia>
{
    public void Configure(
        EntityTypeBuilder<ProductoLoteExistencia> builder)
    {
        builder.ToTable(
            "productos_lotes_existencias",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoLoteId)
            .HasColumnName("producto_lote_id")
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
            x.ProductoLoteId,
            x.BodegaId
        })
        .IsUnique();

        builder.HasOne(x => x.ProductoLote)
            .WithMany(x => x.Existencias)
            .HasForeignKey(x => x.ProductoLoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bodega)
            .WithMany()
            .HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}