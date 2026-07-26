using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class MovimientoInventarioConfiguration
    : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(
        EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable(
            "movimientos_inventario",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(x => x.BodegaOrigenId)
            .HasColumnName("bodega_origen_id");

        builder.Property(x => x.BodegaDestinoId)
            .HasColumnName("bodega_destino_id");

        builder.Property(x => x.TipoMovimiento)
            .HasColumnName("tipo_movimiento")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.FechaMovimiento)
            .HasColumnName("fecha_movimiento")
            .IsRequired();

        builder.Property(x => x.NumeroDocumento)
            .HasColumnName("numero_documento")
            .HasMaxLength(100);

        builder.Property(x => x.Referencia)
            .HasColumnName("referencia")
            .HasMaxLength(200);

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion")
            .HasMaxLength(1000);

        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id");

        builder.Property(x => x.Estado)
            .HasColumnName("estado")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(x => x.Empresa)
            .WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BodegaOrigen)
            .WithMany()
            .HasForeignKey(x => x.BodegaOrigenId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.BodegaDestino)
            .WithMany()
            .HasForeignKey(x => x.BodegaDestinoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Usuario)
            .WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}