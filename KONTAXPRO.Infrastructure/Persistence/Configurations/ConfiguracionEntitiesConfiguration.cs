using KONTAXPRO.Domain.Entities.Configuracion;
using ConfiguracionFacturacionElectronica =
    KONTAXPRO.Domain.Entities.Configuracion.FacturacionElectronica;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class SecuencialComprobanteConfiguration
    : IEntityTypeConfiguration<SecuencialComprobante>
{
    public void Configure(EntityTypeBuilder<SecuencialComprobante> builder)
    {
        builder.ToTable(
            "secuenciales_comprobantes",
            "s_configuracion",
            table => table.HasCheckConstraint(
                "ck_secuenciales_comprobantes_rango",
                "ultimo_secuencial BETWEEN 0 AND 999999999"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.PuntoEmisionId)
            .HasColumnName("punto_emision_id").IsRequired();
        builder.Property(x => x.TipoComprobanteId)
            .HasColumnName("tipo_comprobante_id").IsRequired();
        builder.Property(x => x.TipoAmbienteId)
            .HasColumnName("tipo_ambiente_id").IsRequired();
        builder.Property(x => x.UltimoSecuencial)
            .HasColumnName("ultimo_secuencial").HasDefaultValue(0).IsRequired();
        ConfigurarTimestamps(builder);
        builder.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.TipoAmbienteId })
            .IsUnique()
            .HasDatabaseName(
                "ux_secuenciales_comprobantes_punto_tipo_ambiente");
        builder.HasOne(x => x.PuntoEmision)
            .WithMany(x => x.SecuencialesComprobantes)
            .HasForeignKey(x => x.PuntoEmisionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoAmbiente).WithMany()
            .HasForeignKey(x => x.TipoAmbienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    internal static void ConfigurarTimestamps<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
    }
}

public sealed class SecuencialInternoConfiguration
    : IEntityTypeConfiguration<SecuencialInterno>
{
    public void Configure(EntityTypeBuilder<SecuencialInterno> builder)
    {
        builder.ToTable(
            "secuenciales_internos",
            "s_configuracion",
            table => table.HasCheckConstraint(
                "ck_secuenciales_internos_no_negativo",
                "ultimo_secuencial >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id").IsRequired();
        builder.Property(x => x.TipoDocumentoInternoId)
            .HasColumnName("tipo_documento_interno_id").IsRequired();
        builder.Property(x => x.UltimoSecuencial)
            .HasColumnName("ultimo_secuencial").HasDefaultValue(0).IsRequired();
        SecuencialComprobanteConfiguration.ConfigurarTimestamps(builder);
        builder.HasIndex(x => new
            { x.EmpresaId, x.EstablecimientoId, x.TipoDocumentoInternoId })
            .IsUnique()
            .HasDatabaseName(
                "ux_secuenciales_internos_empresa_establecimiento_tipo");
        builder.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Establecimiento)
            .WithMany(x => x.SecuencialesInternos)
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoDocumentoInterno).WithMany()
            .HasForeignKey(x => x.TipoDocumentoInternoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UsuarioConfiguracionEmpresaConfiguration
    : IEntityTypeConfiguration<UsuarioConfiguracionEmpresa>
{
    public void Configure(
        EntityTypeBuilder<UsuarioConfiguracionEmpresa> builder)
    {
        builder.ToTable(
            "usuarios_configuracion_empresa",
            "s_configuracion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id").IsRequired();
        builder.Property(x => x.PuntoEmisionId)
            .HasColumnName("punto_emision_id");
        builder.Property(x => x.BodegaId)
            .HasColumnName("bodega_id");
        SecuencialComprobanteConfiguration.ConfigurarTimestamps(builder);
        builder.HasIndex(x => new { x.UsuarioId, x.EmpresaId })
            .IsUnique()
            .HasDatabaseName(
                "ux_usuarios_configuracion_empresa_usuario_empresa");
        builder.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Empresa)
            .WithMany(x => x.UsuariosConfiguracionesEmpresa)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Establecimiento)
            .WithMany(x => x.UsuariosConfiguracionesEmpresa)
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.PuntoEmision)
            .WithMany(x => x.UsuariosConfiguracionesEmpresa)
            .HasForeignKey(x => new { x.PuntoEmisionId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => new { x.BodegaId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class FacturacionElectronicaConfiguration
    : IEntityTypeConfiguration<ConfiguracionFacturacionElectronica>
{
    public void Configure(
        EntityTypeBuilder<ConfiguracionFacturacionElectronica> builder)
    {
        builder.ToTable("facturacion_electronica", "s_configuracion");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id")
            .UseIdentityByDefaultColumn();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.TipoAmbienteId)
            .HasColumnName("tipo_ambiente_id").IsRequired();
        builder.Property(x => x.TipoEmisionId)
            .HasColumnName("tipo_emision_id").IsRequired();
        builder.Property(x => x.CertificadoNombre)
            .HasColumnName("certificado_nombre").HasMaxLength(255);
        builder.Property(x => x.CertificadoReferencia)
            .HasColumnName("certificado_referencia").HasMaxLength(255);
        builder.Property(x => x.CertificadoTitular)
            .HasColumnName("certificado_titular").HasMaxLength(300);
        builder.Property(x => x.CertificadoEmisor)
            .HasColumnName("certificado_emisor").HasMaxLength(300);
        builder.Property(x => x.CertificadoNumeroSerie)
            .HasColumnName("certificado_numero_serie").HasMaxLength(128);
        builder.Property(x => x.CertificadoFechaInicio)
            .HasColumnName("certificado_fecha_inicio")
            .HasColumnType("date");
        builder.Property(x => x.CertificadoFechaCaducidad)
            .HasColumnName("certificado_fecha_caducidad")
            .HasColumnType("date");
        builder.Property(x => x.Habilitada)
            .HasColumnName("habilitada").HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Version)
            .IsRowVersion();
        SecuencialComprobanteConfiguration.ConfigurarTimestamps(builder);
        builder.HasIndex(x => x.EmpresaId)
            .IsUnique()
            .HasDatabaseName("ux_facturacion_electronica_empresa");
        builder.HasOne(x => x.Empresa)
            .WithOne(x => x.FacturacionElectronica)
            .HasForeignKey<ConfiguracionFacturacionElectronica>(
                x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoAmbiente).WithMany()
            .HasForeignKey(x => x.TipoAmbienteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TipoEmision).WithMany()
            .HasForeignKey(x => x.TipoEmisionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
