using KONTAXPRO.Domain.Entities.Comercial;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class TerceroConfiguration
    : IEntityTypeConfiguration<Tercero>
{
    public void Configure(EntityTypeBuilder<Tercero> builder)
    {
        builder.ToTable(
            "terceros",
            "s_comercial",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_terceros_origen_registro",
                    "origen_registro IN ('OFICIAL', 'OFFLINE')");
                table.HasCheckConstraint(
                    "ck_terceros_estado_verificacion",
                    "estado_verificacion IN ('PENDIENTE', 'VERIFICADO')");
                table.HasCheckConstraint(
                    "ck_terceros_estado",
                    "estado IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_terceros_consumidor_final_protegido",
                    "numero_identificacion <> '9999999999999' OR " +
                    "(razon_social = 'CONSUMIDOR FINAL' AND estado = 1)");
            });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.TipoIdentificacionId)
            .HasColumnName("tipo_identificacion_id").IsRequired();
        builder.Property(x => x.NumeroIdentificacion)
            .HasColumnName("numero_identificacion")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.RazonSocial)
            .HasColumnName("razon_social")
            .HasMaxLength(256).IsRequired();
        builder.Property(x => x.NombreComercial)
            .HasColumnName("nombre_comercial").HasMaxLength(256);
        builder.Property(x => x.Direccion)
            .HasColumnName("direccion").HasMaxLength(500);
        builder.Property(x => x.Correo)
            .HasColumnName("correo").HasMaxLength(254);
        builder.Property(x => x.Telefono)
            .HasColumnName("telefono").HasMaxLength(32);
        builder.Property(x => x.OrigenRegistro)
            .HasColumnName("origen_registro")
            .HasMaxLength(16).IsRequired();
        builder.Property(x => x.EstadoVerificacion)
            .HasColumnName("estado_verificacion")
            .HasMaxLength(16).IsRequired();
        builder.Property(x => x.FuenteVerificacion)
            .HasColumnName("fuente_verificacion").HasMaxLength(255);
        builder.Property(x => x.VerificadoAt)
            .HasColumnName("verificado_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.Ignore(x => x.EsConsumidorFinal);
        builder.HasIndex(x => new
            { x.TipoIdentificacionId, x.NumeroIdentificacion })
            .IsUnique()
            .HasDatabaseName(
                "ux_terceros_tipo_identificacion_numero");
        builder.HasOne(x => x.TipoIdentificacion).WithMany()
            .HasForeignKey(x => x.TipoIdentificacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmpresaTerceroConfiguration
    : IEntityTypeConfiguration<EmpresaTercero>
{
    public void Configure(EntityTypeBuilder<EmpresaTercero> builder)
    {
        builder.ToTable(
            "empresas_terceros",
            "s_comercial",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_empresas_terceros_tipo",
                    "es_cliente OR es_proveedor");
                table.HasCheckConstraint(
                    "ck_empresas_terceros_cupo_credito",
                    "cupo_credito IS NULL OR cupo_credito >= 0");
                table.HasCheckConstraint(
                    "ck_empresas_terceros_dias_credito",
                    "dias_credito IS NULL OR dias_credito >= 0");
                table.HasCheckConstraint(
                    "ck_empresas_terceros_estado",
                    "estado IN (0, 1)");
            });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.TerceroId)
            .HasColumnName("tercero_id").IsRequired();
        builder.Property(x => x.EsCliente)
            .HasColumnName("es_cliente").IsRequired();
        builder.Property(x => x.EsProveedor)
            .HasColumnName("es_proveedor").IsRequired();
        builder.Property(x => x.ListaPrecioId)
            .HasColumnName("lista_precio_id");
        builder.Property(x => x.CreditoHabilitado)
            .HasColumnName("credito_habilitado")
            .HasDefaultValue(false).IsRequired();
        builder.Property(x => x.CupoCredito)
            .HasColumnName("cupo_credito").HasPrecision(18, 2);
        builder.Property(x => x.DiasCredito)
            .HasColumnName("dias_credito");
        builder.Property(x => x.MotivoBloqueoCredito)
            .HasColumnName("motivo_bloqueo_credito").HasMaxLength(500);
        builder.Property(x => x.Observacion)
            .HasColumnName("observacion").HasMaxLength(1000);
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.HasIndex(x => new { x.EmpresaId, x.TerceroId })
            .IsUnique()
            .HasDatabaseName("ux_empresas_terceros_empresa_tercero");
        builder.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Tercero)
            .WithMany(x => x.EmpresasTerceros)
            .HasForeignKey(x => x.TerceroId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ListaPrecio).WithMany()
            .HasForeignKey(x => new { x.ListaPrecioId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
