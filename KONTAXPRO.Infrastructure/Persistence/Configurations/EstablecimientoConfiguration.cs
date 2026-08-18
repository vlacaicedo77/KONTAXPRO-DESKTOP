using KONTAXPRO.Domain.Entities.Configuracion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class EstablecimientoConfiguration
    : IEntityTypeConfiguration<Establecimiento>
{
    public void Configure(EntityTypeBuilder<Establecimiento> builder)
    {
        builder.ToTable(
            "establecimientos",
            "s_configuracion",
            table => table.HasCheckConstraint(
                "ck_establecimientos_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.Codigo)
            .HasColumnName("codigo").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Prefijo)
            .HasColumnName("prefijo").HasMaxLength(16).IsRequired();
        builder.Property(x => x.Nombre)
            .HasColumnName("nombre").HasMaxLength(150).IsRequired();
        builder.Property(x => x.NombreComercial)
            .HasColumnName("nombre_comercial").HasMaxLength(300);
        builder.Property(x => x.Direccion)
            .HasColumnName("direccion").HasMaxLength(500).IsRequired();
        builder.Property(x => x.EsMatriz)
            .HasColumnName("es_matriz").IsRequired();
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        builder.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_establecimientos_id_empresa");
        builder.HasIndex(x => new { x.EmpresaId, x.Codigo })
            .IsUnique()
            .HasDatabaseName("ux_establecimientos_empresa_codigo");
        builder.HasIndex(x => new { x.EmpresaId, x.Prefijo })
            .IsUnique()
            .HasDatabaseName("ux_establecimientos_empresa_prefijo");
        builder.HasIndex(x => x.EmpresaId)
            .IsUnique()
            .HasFilter("es_matriz")
            .HasDatabaseName("ux_establecimientos_empresa_matriz");
        builder.HasOne(x => x.Empresa).WithMany(x => x.Establecimientos)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
