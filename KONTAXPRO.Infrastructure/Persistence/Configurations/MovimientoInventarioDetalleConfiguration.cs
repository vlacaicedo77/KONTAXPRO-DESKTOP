using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioDetalleConfiguration
    : IEntityTypeConfiguration<MovimientoInventarioDetalle>
{
    public void Configure(
        EntityTypeBuilder<MovimientoInventarioDetalle> builder)
    {
        builder.ToTable(
            "movimientos_inventario_detalles",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.MovimientoInventarioId)
            .HasColumnName("movimiento_inventario_id")
            .IsRequired();

        builder.Property(x => x.ProductoId)
            .HasColumnName("producto_id")
            .IsRequired();

        builder.Property(x => x.ProductoPresentacionId)
            .HasColumnName("producto_presentacion_id");

        builder.Property(x => x.ProductoLoteId)
            .HasColumnName("producto_lote_id");

        builder.Property(x => x.ProductoSerieId)
            .HasColumnName("producto_serie_id");

        builder.Property(x => x.CantidadPresentacion)
            .HasColumnName("cantidad_presentacion")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.FactorConversion)
            .HasColumnName("factor_conversion")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CantidadBase)
            .HasColumnName("cantidad_base")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CostoUnitarioBase)
            .HasColumnName("costo_unitario_base")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.CostoTotal)
            .HasColumnName("costo_total")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.EsBonificacion)
            .HasColumnName("es_bonificacion")
            .IsRequired();

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion")
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(x => x.MovimientoInventario)
            .WithMany(x => x.Detalles)
            .HasForeignKey(x => x.MovimientoInventarioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Producto)
            .WithMany(x => x.MovimientosInventarioDetalles)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductoPresentacion)
            .WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductoLote)
            .WithMany()
            .HasForeignKey(x => x.ProductoLoteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ProductoSerie)
            .WithMany()
            .HasForeignKey(x => x.ProductoSerieId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}