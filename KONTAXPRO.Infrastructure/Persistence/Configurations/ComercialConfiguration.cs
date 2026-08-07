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
                    "ck_terceros_estado_cliente",
                    "estado_cliente IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_terceros_estado_proveedor",
                    "estado_proveedor IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_terceros_consumidor_final_protegido",
                    "numero_identificacion <> '9999999999999' OR " +
                    "(razon_social = 'CONSUMIDOR FINAL' AND es_cliente AND " +
                    "estado_cliente = 1 AND estado = 1)");
            });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.TipoIdentificacionId)
            .HasColumnName("tipo_identificacion_id").IsRequired();
        builder.Property(x => x.NumeroIdentificacion)
            .HasColumnName("numero_identificacion")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.ClaveIdentidad)
            .HasColumnName("clave_identidad")
            .HasMaxLength(24).IsRequired();
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
        builder.Property(x => x.EsCliente)
            .HasColumnName("es_cliente").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.EstadoCliente)
            .HasColumnName("estado_cliente").HasDefaultValue(0).IsRequired();
        builder.Property(x => x.EsProveedor)
            .HasColumnName("es_proveedor").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.EstadoProveedor)
            .HasColumnName("estado_proveedor").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.Version)
            .IsRowVersion();
        builder.Ignore(x => x.EsConsumidorFinal);
        builder.HasIndex(x => new
            { x.TipoIdentificacionId, x.NumeroIdentificacion })
            .IsUnique()
            .HasDatabaseName(
                "ux_terceros_tipo_identificacion_numero");
        builder.HasIndex(x => x.ClaveIdentidad)
            .IsUnique()
            .HasDatabaseName(
                KONTAXPRO.Application.Clientes.ClaveIdentidadTercero
                    .UniqueConstraintName);
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
        builder.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_empresas_terceros_id_empresa");
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.TerceroId)
            .HasColumnName("tercero_id").IsRequired();
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

public sealed class TerceroIdentificacionConfiguration
    : IEntityTypeConfiguration<TerceroIdentificacion>
{
    public void Configure(EntityTypeBuilder<TerceroIdentificacion> builder)
    {
        builder.ToTable(
            "terceros_identificaciones",
            "s_comercial",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_terceros_identificaciones_verificacion",
                    "estado_verificacion IN ('PENDIENTE', 'VERIFICADO')");
                table.HasCheckConstraint(
                    "ck_terceros_identificaciones_estado",
                    "estado IN (0, 1)");
            });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.TerceroId)
            .HasColumnName("tercero_id").IsRequired();
        builder.Property(x => x.TipoIdentificacionId)
            .HasColumnName("tipo_identificacion_id").IsRequired();
        builder.Property(x => x.NumeroIdentificacion)
            .HasColumnName("numero_identificacion")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.NumeroNormalizado)
            .HasColumnName("numero_normalizado")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.EsPrincipal)
            .HasColumnName("es_principal").IsRequired();
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
        builder.HasIndex(x => new
            { x.TipoIdentificacionId, x.NumeroNormalizado })
            .IsUnique()
            .HasDatabaseName(
                "ux_terceros_identificaciones_tipo_numero");
        builder.HasIndex(x => x.TerceroId)
            .HasDatabaseName("ix_terceros_identificaciones_tercero");
        builder.HasIndex(x => new { x.TerceroId, x.EsPrincipal })
            .IsUnique()
            .HasFilter("es_principal")
            .HasDatabaseName(
                "ux_terceros_identificaciones_principal");
        builder.HasOne(x => x.Tercero)
            .WithMany(x => x.Identificaciones)
            .HasForeignKey(x => x.TerceroId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoIdentificacion).WithMany()
            .HasForeignKey(x => x.TipoIdentificacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
