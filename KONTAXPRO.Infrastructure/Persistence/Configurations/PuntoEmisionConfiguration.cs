using KONTAXPRO.Domain.Entities.Configuracion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class PuntoEmisionConfiguration
    : IEntityTypeConfiguration<PuntoEmision>
{
    public void Configure(EntityTypeBuilder<PuntoEmision> builder)
    {
        builder.ToTable(
            "puntos_emision",
            "s_configuracion",
            table => table.HasCheckConstraint(
                "ck_puntos_emision_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id").IsRequired();
        builder.Property(x => x.Codigo)
            .HasColumnName("codigo").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Nombre)
            .HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasAlternateKey(x => new { x.Id, x.EstablecimientoId })
            .HasName("ak_puntos_emision_id_establecimiento");
        builder.HasIndex(x => new { x.EstablecimientoId, x.Codigo })
            .IsUnique()
            .HasDatabaseName("ux_puntos_emision_establecimiento_codigo");
        builder.HasOne(x => x.Establecimiento)
            .WithMany(x => x.PuntosEmision)
            .HasForeignKey(x => x.EstablecimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
