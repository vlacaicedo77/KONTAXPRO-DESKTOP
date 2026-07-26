using KONTAXPRO.Domain.Entities.Configuracion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable(
            "empresas",
            "s_configuracion",
            table => table.HasCheckConstraint(
                "ck_empresas_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.RegimenTributarioId)
            .HasColumnName("regimen_tributario_id").IsRequired();
        builder.Property(x => x.NumeroIdentificacion)
            .HasColumnName("numero_identificacion")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.RazonSocial)
            .HasColumnName("razon_social")
            .HasMaxLength(256).IsRequired();
        builder.Property(x => x.NombreComercial)
            .HasColumnName("nombre_comercial").HasMaxLength(256);
        builder.Property(x => x.ObligadoContabilidad)
            .HasColumnName("obligado_contabilidad").IsRequired();
        builder.Property(x => x.ContribuyenteEspecialNumero)
            .HasColumnName("contribuyente_especial_numero")
            .HasMaxLength(32);
        builder.Property(x => x.Correo)
            .HasColumnName("correo").HasMaxLength(254);
        builder.Property(x => x.Telefono)
            .HasColumnName("telefono").HasMaxLength(32);
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.HasIndex(x => x.NumeroIdentificacion)
            .IsUnique()
            .HasDatabaseName("ux_empresas_numero_identificacion");
        builder.HasOne(x => x.RegimenTributario).WithMany()
            .HasForeignKey(x => x.RegimenTributarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
