using System.Text;
using KONTAXPRO.Domain.Entities.Contabilidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class ContabilidadEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        bool updated = true) where TEntity : class
    {
        b.ToTable(table, "s_contabilidad");
        b.HasKey("Id");
        b.Property<long>("Id").HasColumnName("id")
            .UseIdentityByDefaultColumn();
        foreach (var property in typeof(TEntity).GetProperties()
                     .Where(x => IsScalar(x.PropertyType)))
            b.Property(property.Name).HasColumnName(Snake(property.Name));
        if (typeof(TEntity).GetProperty("CreatedAt") is not null)
            b.Property<DateTime>("CreatedAt")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        if (updated && typeof(TEntity).GetProperty("UpdatedAt") is not null)
            b.Property<DateTime?>("UpdatedAt")
                .HasColumnType("timestamp with time zone");
    }

    internal static void Money<TEntity>(
        EntityTypeBuilder<TEntity> b,
        params string[] names) where TEntity : class
    {
        foreach (var name in names)
            b.Property<decimal>(name).HasPrecision(18, 2).IsRequired();
    }

    private static bool IsScalar(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive || type.IsEnum || type == typeof(string) ||
               type == typeof(decimal) || type == typeof(DateTime) ||
               type == typeof(DateOnly) || type == typeof(Guid);
    }

    private static string Snake(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            if (i > 0 && char.IsUpper(value[i])) result.Append('_');
            result.Append(char.ToLowerInvariant(value[i]));
        }
        return result.ToString();
    }
}

public sealed class PlanCuentaConfiguration
    : IEntityTypeConfiguration<PlanCuenta>
{
    public void Configure(EntityTypeBuilder<PlanCuenta> b)
    {
        ContabilidadEf.Base(b, "plan_cuentas");
        b.ToTable("plan_cuentas", "s_contabilidad", t =>
        {
            t.HasCheckConstraint("ck_plan_cuentas_naturaleza",
                "naturaleza IN ('DEUDORA', 'ACREEDORA')");
            t.HasCheckConstraint("ck_plan_cuentas_estado", "estado IN (0, 1)");
            t.HasCheckConstraint("ck_plan_cuentas_padre",
                "cuenta_padre_id IS NULL OR cuenta_padre_id <> id");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_plan_cuentas_id_empresa");
        b.Property(x => x.Codigo).HasMaxLength(64).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(200).IsRequired();
        b.Property(x => x.Naturaleza).HasMaxLength(16).IsRequired();
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique()
            .HasDatabaseName("ux_plan_cuentas_empresa_codigo");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaPadre).WithMany(x => x.CuentasHijas)
            .HasForeignKey(x => new { x.CuentaPadreId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConfiguracionCuentaConfiguration
    : IEntityTypeConfiguration<ConfiguracionCuenta>
{
    public void Configure(EntityTypeBuilder<ConfiguracionCuenta> b)
    {
        ContabilidadEf.Base(b, "configuracion_cuentas");
        b.ToTable("configuracion_cuentas", "s_contabilidad", t =>
            t.HasCheckConstraint("ck_configuracion_cuentas_estado",
                "estado IN (0, 1)"));
        b.HasIndex(x => new { x.EmpresaId, x.TipoConfiguracionContableId })
            .IsUnique()
            .HasDatabaseName("ux_configuracion_cuentas_empresa_tipo");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoConfiguracionContable).WithMany()
            .HasForeignKey(x => x.TipoConfiguracionContableId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaContable).WithMany()
            .HasForeignKey(x => new { x.CuentaContableId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PeriodoContableConfiguration
    : IEntityTypeConfiguration<PeriodoContable>
{
    public void Configure(EntityTypeBuilder<PeriodoContable> b)
    {
        ContabilidadEf.Base(b, "periodos_contables");
        b.ToTable("periodos_contables", "s_contabilidad", t =>
        {
            t.HasCheckConstraint("ck_periodos_contables_mes",
                "mes BETWEEN 1 AND 12");
            t.HasCheckConstraint("ck_periodos_contables_fechas",
                "fecha_fin >= fecha_inicio");
            t.HasCheckConstraint("ck_periodos_contables_estado",
                "estado IN ('ABIERTO', 'CERRADO')");
            t.HasCheckConstraint("ck_periodos_contables_cierre",
                "(estado = 'ABIERTO' AND cerrado_por_usuario_id IS NULL " +
                "AND cerrado_at IS NULL) OR " +
                "(estado = 'CERRADO' AND cerrado_por_usuario_id IS NOT NULL " +
                "AND cerrado_at IS NOT NULL)");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_periodos_contables_id_empresa");
        b.Property(x => x.Estado).HasMaxLength(16).IsRequired();
        b.Property(x => x.FechaInicio).HasColumnType("date");
        b.Property(x => x.FechaFin).HasColumnType("date");
        b.Property(x => x.CerradoAt).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.EmpresaId, x.Anio, x.Mes }).IsUnique()
            .HasDatabaseName("ux_periodos_contables_empresa_anio_mes");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CerradoPorUsuario).WithMany()
            .HasForeignKey(x => x.CerradoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SecuencialAsientoConfiguration
    : IEntityTypeConfiguration<SecuencialAsiento>
{
    public void Configure(EntityTypeBuilder<SecuencialAsiento> b)
    {
        ContabilidadEf.Base(b, "secuenciales_asientos");
        b.ToTable("secuenciales_asientos", "s_contabilidad", t =>
            t.HasCheckConstraint("ck_secuenciales_asientos_valor",
                "ultimo_secuencial >= 0"));
        b.HasIndex(x => new { x.EmpresaId, x.Anio }).IsUnique()
            .HasDatabaseName("ux_secuenciales_asientos_empresa_anio");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AsientoConfiguration : IEntityTypeConfiguration<Asiento>
{
    public void Configure(EntityTypeBuilder<Asiento> b)
    {
        ContabilidadEf.Base(b, "asientos");
        b.ToTable("asientos", "s_contabilidad", t =>
        {
            t.HasCheckConstraint("ck_asientos_tipo",
                "tipo_asiento IN ('AUTOMATICO', 'MANUAL')");
            t.HasCheckConstraint("ck_asientos_estado",
                "estado IN ('CONTABILIZADO', 'ANULADO')");
            t.HasCheckConstraint("ck_asientos_numero",
                "numero_asiento ~ '^ASI-[0-9]{4}-[0-9]{6,}$'");
        });
        b.Property(x => x.NumeroAsiento).HasMaxLength(32).IsRequired();
        b.Property(x => x.TipoAsiento).HasMaxLength(16).IsRequired();
        b.Property(x => x.Concepto).HasMaxLength(500).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(16).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Fecha).HasColumnType("date");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroAsiento }).IsUnique()
            .HasDatabaseName("ux_asientos_empresa_numero");
        b.HasIndex(x => new { x.EmpresaId, x.TipoOrigenAsientoId, x.OrigenId })
            .HasFilter("origen_id IS NOT NULL")
            .HasDatabaseName("ix_asientos_origen");
        b.HasIndex(x => x.AsientoOrigenReversadoId).IsUnique()
            .HasFilter("asiento_origen_reversado_id IS NOT NULL")
            .HasDatabaseName("ux_asientos_origen_reversado");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Periodo).WithMany(x => x.Asientos)
            .HasForeignKey(x => new { x.PeriodoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoOrigenAsiento).WithMany()
            .HasForeignKey(x => x.TipoOrigenAsientoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AsientoOrigenReversado).WithOne(x => x.AsientoReverso)
            .HasForeignKey<Asiento>(x => x.AsientoOrigenReversadoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AsientoDetalleConfiguration
    : IEntityTypeConfiguration<AsientoDetalle>
{
    public void Configure(EntityTypeBuilder<AsientoDetalle> b)
    {
        ContabilidadEf.Base(b, "asientos_detalles", false);
        b.ToTable("asientos_detalles", "s_contabilidad", t =>
        {
            t.HasCheckConstraint("ck_asientos_detalles_orden", "orden > 0");
            t.HasCheckConstraint("ck_asientos_detalles_debe_haber",
                "(debe > 0 AND haber = 0) OR (haber > 0 AND debe = 0)");
        });
        b.Property(x => x.Descripcion).HasMaxLength(500);
        ContabilidadEf.Money(b, "Debe", "Haber");
        b.HasIndex(x => new { x.AsientoId, x.Orden }).IsUnique()
            .HasDatabaseName("ux_asientos_detalles_asiento_orden");
        b.HasOne(x => x.Asiento).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.AsientoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaContable).WithMany()
            .HasForeignKey(x => x.CuentaContableId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => x.EmpresaTerceroId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
