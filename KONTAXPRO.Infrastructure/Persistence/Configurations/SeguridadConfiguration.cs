using KONTAXPRO.Domain.Entities.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class SeguridadConfiguration
{
    internal static void Id<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<long>("Id").HasColumnName("id")
            .UseIdentityByDefaultColumn();
    }

    internal static void Timestamps<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        bool updatedAt = true)
        where TEntity : class
    {
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        if (updatedAt)
        {
            builder.Property<DateTime?>("UpdatedAt")
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");
        }
    }
}

public sealed class UsuarioConfiguration
    : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable(
            "usuarios",
            "s_seguridad",
            table => table.HasCheckConstraint(
                "ck_usuarios_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.NumeroIdentificacion)
            .HasColumnName("numero_identificacion")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.NombreCompleto)
            .HasColumnName("nombre_completo")
            .HasMaxLength(256).IsRequired();
        builder.Property(x => x.Correo)
            .HasColumnName("correo").HasMaxLength(254);
        builder.Property(x => x.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(255).IsRequired();
        builder.Property(x => x.RequiereCambioClave)
            .HasColumnName("requiere_cambio_clave")
            .HasDefaultValue(false).IsRequired();
        builder.Property(x => x.UltimoAccesoAt)
            .HasColumnName("ultimo_acceso_at")
            .HasColumnType("timestamp with time zone");
        builder.Property(x => x.Estado)
            .HasColumnName("estado").HasDefaultValue(1).IsRequired();
        SeguridadConfiguration.Timestamps(builder);
        builder.HasIndex(x => x.NumeroIdentificacion)
            .IsUnique()
            .HasDatabaseName("ux_usuarios_numero_identificacion");
    }
}

public sealed class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable(
            "roles",
            "s_seguridad",
            table => table.HasCheckConstraint(
                "ck_roles_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(64).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        builder.Property(x => x.EsSistema).HasColumnName("es_sistema")
            .HasDefaultValue(false).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado")
            .HasDefaultValue(1).IsRequired();
        SeguridadConfiguration.Timestamps(builder);
        builder.HasIndex(x => x.Codigo).IsUnique()
            .HasDatabaseName("ux_roles_codigo");
    }
}

public sealed class PermisoConfiguration
    : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable(
            "permisos",
            "s_seguridad",
            table => table.HasCheckConstraint(
                "ck_permisos_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(96).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        builder.Property(x => x.Modulo).HasColumnName("modulo")
            .HasMaxLength(64).IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado")
            .HasDefaultValue(1).IsRequired();
        SeguridadConfiguration.Timestamps(builder);
        builder.HasIndex(x => x.Codigo).IsUnique()
            .HasDatabaseName("ux_permisos_codigo");
    }
}

public sealed class RolPermisoConfiguration
    : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("roles_permisos", "s_seguridad");
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.RolId).HasColumnName("rol_id").IsRequired();
        builder.Property(x => x.PermisoId)
            .HasColumnName("permiso_id").IsRequired();
        SeguridadConfiguration.Timestamps(builder, updatedAt: false);
        builder.HasIndex(x => new { x.RolId, x.PermisoId })
            .IsUnique()
            .HasDatabaseName("ux_roles_permisos_rol_permiso");
        builder.HasOne(x => x.Rol).WithMany(x => x.RolesPermisos)
            .HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Permiso).WithMany(x => x.RolesPermisos)
            .HasForeignKey(x => x.PermisoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UsuarioEmpresaConfiguration
    : IEntityTypeConfiguration<UsuarioEmpresa>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresa> builder)
    {
        builder.ToTable(
            "usuarios_empresas",
            "s_seguridad",
            table => table.HasCheckConstraint(
                "ck_usuarios_empresas_estado", "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.UsuarioId)
            .HasColumnName("usuario_id").IsRequired();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id").IsRequired();
        builder.Property(x => x.Estado).HasColumnName("estado")
            .HasDefaultValue(1).IsRequired();
        SeguridadConfiguration.Timestamps(builder);
        builder.HasIndex(x => new { x.UsuarioId, x.EmpresaId })
            .IsUnique()
            .HasDatabaseName("ux_usuarios_empresas_usuario_empresa");
        builder.HasOne(x => x.Usuario).WithMany(x => x.UsuariosEmpresas)
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Empresa).WithMany(x => x.UsuariosEmpresas)
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UsuarioEmpresaRolConfiguration
    : IEntityTypeConfiguration<UsuarioEmpresaRol>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresaRol> builder)
    {
        builder.ToTable("usuarios_empresas_roles", "s_seguridad");
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.UsuarioEmpresaId)
            .HasColumnName("usuario_empresa_id").IsRequired();
        builder.Property(x => x.RolId).HasColumnName("rol_id").IsRequired();
        SeguridadConfiguration.Timestamps(builder, updatedAt: false);
        builder.HasIndex(x => new { x.UsuarioEmpresaId, x.RolId })
            .IsUnique()
            .HasDatabaseName(
                "ux_usuarios_empresas_roles_usuario_empresa_rol");
        builder.HasOne(x => x.UsuarioEmpresa)
            .WithMany(x => x.UsuariosEmpresasRoles)
            .HasForeignKey(x => x.UsuarioEmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Rol).WithMany(x => x.UsuariosEmpresasRoles)
            .HasForeignKey(x => x.RolId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UsuarioEmpresaEstablecimientoConfiguration
    : IEntityTypeConfiguration<UsuarioEmpresaEstablecimiento>
{
    public void Configure(
        EntityTypeBuilder<UsuarioEmpresaEstablecimiento> builder)
    {
        builder.ToTable(
            "usuarios_empresas_establecimientos",
            "s_seguridad");
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.UsuarioEmpresaId)
            .HasColumnName("usuario_empresa_id").IsRequired();
        builder.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id").IsRequired();
        SeguridadConfiguration.Timestamps(builder, updatedAt: false);
        builder.HasIndex(x => new
            { x.UsuarioEmpresaId, x.EstablecimientoId })
            .IsUnique()
            .HasDatabaseName(
                "ux_usuarios_empresas_establecimientos_usuario_establecimiento");
        builder.HasOne(x => x.UsuarioEmpresa)
            .WithMany(x => x.UsuariosEmpresasEstablecimientos)
            .HasForeignKey(x => x.UsuarioEmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => x.EstablecimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AuditoriaConfiguration
    : IEntityTypeConfiguration<Auditoria>
{
    public void Configure(EntityTypeBuilder<Auditoria> builder)
    {
        builder.ToTable("auditoria", "s_seguridad");
        builder.HasKey(x => x.Id);
        SeguridadConfiguration.Id(builder);
        builder.Property(x => x.UsuarioId).HasColumnName("usuario_id");
        builder.Property(x => x.EmpresaId).HasColumnName("empresa_id");
        builder.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id");
        builder.Property(x => x.InstalacionUuid)
            .HasColumnName("instalacion_uuid").HasColumnType("uuid");
        builder.Property(x => x.NombreEquipo)
            .HasColumnName("nombre_equipo").HasMaxLength(255);
        builder.Property(x => x.IpEquipo)
            .HasColumnName("ip_equipo").HasMaxLength(45);
        builder.Property(x => x.Accion).HasColumnName("accion")
            .HasMaxLength(128).IsRequired();
        builder.Property(x => x.Entidad).HasColumnName("entidad")
            .HasMaxLength(128);
        builder.Property(x => x.EntidadId).HasColumnName("entidad_id");
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasColumnType("text");
        SeguridadConfiguration.Timestamps(builder, updatedAt: false);
        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("idx_auditoria_created_at");
        builder.HasIndex(x => new { x.EmpresaId, x.CreatedAt })
            .HasDatabaseName("idx_auditoria_empresa_created_at");
        builder.HasOne(x => x.Usuario).WithMany()
            .HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => x.EstablecimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
