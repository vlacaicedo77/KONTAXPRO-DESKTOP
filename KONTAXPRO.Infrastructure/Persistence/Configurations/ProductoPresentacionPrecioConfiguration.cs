using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoPresentacionPrecioConfiguration
    : IEntityTypeConfiguration<ProductoPresentacionPrecio>
{
    public void Configure(
        EntityTypeBuilder<ProductoPresentacionPrecio> builder)
    {
        builder.ToTable(
            "productos_presentaciones_precios",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ProductoPresentacionId)
            .HasColumnName("producto_presentacion_id")
            .IsRequired();

        builder.Property(x => x.ListaPrecioId)
            .HasColumnName("lista_precio_id")
            .IsRequired();

        builder.Property(x => x.MetodoCalculo)
            .HasColumnName("metodo_calculo")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Porcentaje)
            .HasColumnName("porcentaje")
            .HasPrecision(9, 4);

        builder.Property(x => x.Precio)
            .HasColumnName("precio")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.PrecioManual)
            .HasColumnName("precio_manual")
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
            x.ProductoPresentacionId,
            x.ListaPrecioId
        })
        .IsUnique();

        builder.HasOne(x => x.ProductoPresentacion)
            .WithMany(x => x.Precios)
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ListaPrecio)
            .WithMany()
            .HasForeignKey(x => x.ListaPrecioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}