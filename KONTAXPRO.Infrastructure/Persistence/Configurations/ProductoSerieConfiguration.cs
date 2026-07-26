using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoSerieConfiguration
    : IEntityTypeConfiguration<ProductoSerie>
{
    public void Configure(
        EntityTypeBuilder<ProductoSerie> builder)
    {
        builder.ToTable(
            "productos_series",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoId)
            .HasColumnName("producto_id")
            .IsRequired();

        builder.Property(x => x.ProductoLoteId)
            .HasColumnName("producto_lote_id");

        builder.Property(x => x.BodegaId)
            .HasColumnName("bodega_id");

        builder.Property(x => x.NumeroSerie)
            .HasColumnName("numero_serie")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.CostoUnitarioBase)
            .HasColumnName("costo_unitario_base")
            .HasPrecision(18, 6);

        builder.Property(x => x.EstadoSerie)
            .HasColumnName("estado_serie")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion")
            .HasMaxLength(500);

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
            x.NumeroSerie
        })
        .IsUnique();

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.Series)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductoLote)
            .WithMany(x => x.Series)
            .HasForeignKey(x => x.ProductoLoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Bodega)
            .WithMany()
            .HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
