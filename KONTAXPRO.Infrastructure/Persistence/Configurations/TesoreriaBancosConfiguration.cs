using System.Text;
using KONTAXPRO.Domain.Entities.Bancos;
using KONTAXPRO.Domain.Entities.Tesoreria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class FinanzasEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        string schema,
        bool updated = true) where TEntity : class
    {
        b.ToTable(table, schema);
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

public sealed class CajaConfiguration : IEntityTypeConfiguration<Caja>
{
    public void Configure(EntityTypeBuilder<Caja> b)
    {
        FinanzasEf.Base(b, "cajas", "s_tesoreria");
        b.ToTable("cajas", "s_tesoreria", t =>
            t.HasCheckConstraint("ck_cajas_estado", "estado IN (0, 1)"));
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_cajas_id_empresa");
        b.Property(x => x.Codigo).HasMaxLength(64).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique()
            .HasDatabaseName("ux_cajas_empresa_codigo");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaContable).WithMany()
            .HasForeignKey(x => new { x.CuentaContableId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CajaSesionConfiguration
    : IEntityTypeConfiguration<CajaSesion>
{
    public void Configure(EntityTypeBuilder<CajaSesion> b)
    {
        FinanzasEf.Base(b, "cajas_sesiones", "s_tesoreria");
        b.ToTable("cajas_sesiones", "s_tesoreria", t =>
        {
            t.HasCheckConstraint("ck_cajas_sesiones_estado",
                "estado IN ('ABIERTA', 'CERRADA')");
            t.HasCheckConstraint("ck_cajas_sesiones_cierre",
                "(estado = 'ABIERTA' AND cerrada_por_usuario_id IS NULL " +
                "AND fecha_cierre IS NULL) OR " +
                "(estado = 'CERRADA' AND cerrada_por_usuario_id IS NOT NULL " +
                "AND fecha_cierre IS NOT NULL)");
            t.HasCheckConstraint("ck_cajas_sesiones_saldo_inicial",
                "saldo_inicial >= 0");
        });
        b.Property(x => x.Estado).HasMaxLength(16).IsRequired();
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaApertura)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.FechaCierre)
            .HasColumnType("timestamp with time zone");
        FinanzasEf.Money(b, "SaldoInicial");
        b.Property(x => x.SaldoSistema).HasPrecision(18, 2);
        b.Property(x => x.SaldoContado).HasPrecision(18, 2);
        b.Property(x => x.Diferencia).HasPrecision(18, 2);
        b.HasIndex(x => x.CajaId).IsUnique()
            .HasFilter("estado = 'ABIERTA'")
            .HasDatabaseName("ux_cajas_sesiones_caja_abierta");
        b.HasOne(x => x.Caja).WithMany(x => x.Sesiones)
            .HasForeignKey(x => x.CajaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AbiertaPorUsuario).WithMany()
            .HasForeignKey(x => x.AbiertaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CerradaPorUsuario).WithMany()
            .HasForeignKey(x => x.CerradaPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MovimientoCajaConfiguration
    : IEntityTypeConfiguration<MovimientoCaja>
{
    public void Configure(EntityTypeBuilder<MovimientoCaja> b)
    {
        FinanzasEf.Base(b, "movimientos_caja", "s_tesoreria", false);
        b.ToTable("movimientos_caja", "s_tesoreria", t =>
        {
            t.HasCheckConstraint("ck_movimientos_caja_valor", "valor > 0");
            t.HasCheckConstraint("ck_movimientos_caja_medio",
                "NOT (cobro_medio_id IS NOT NULL AND pago_medio_id IS NOT NULL)");
        });
        b.Property(x => x.OrigenTipo).HasMaxLength(32);
        b.Property(x => x.Concepto).HasMaxLength(500).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.FechaMovimiento)
            .HasColumnType("timestamp with time zone");
        FinanzasEf.Money(b, "Valor");
        b.HasIndex(x => x.MovimientoReversoId).IsUnique()
            .HasFilter("movimiento_reverso_id IS NOT NULL")
            .HasDatabaseName("ux_movimientos_caja_reverso");
        b.HasOne(x => x.CajaSesion).WithMany(x => x.Movimientos)
            .HasForeignKey(x => x.CajaSesionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoMovimientoCaja).WithMany()
            .HasForeignKey(x => x.TipoMovimientoCajaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CobroMedio).WithMany()
            .HasForeignKey(x => x.CobroMedioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PagoMedio).WithMany()
            .HasForeignKey(x => x.PagoMedioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoReverso)
            .WithOne(x => x.MovimientoOrigenReversado)
            .HasForeignKey<MovimientoCaja>(x => x.MovimientoReversoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DepositoCajaBancoConfiguration
    : IEntityTypeConfiguration<DepositoCajaBanco>
{
    public void Configure(EntityTypeBuilder<DepositoCajaBanco> b)
    {
        FinanzasEf.Base(b, "depositos_caja_banco", "s_tesoreria");
        b.ToTable("depositos_caja_banco", "s_tesoreria", t =>
            t.HasCheckConstraint("ck_depositos_caja_banco_valor", "valor > 0"));
        b.Property(x => x.NumeroDeposito).HasMaxLength(64).IsRequired();
        b.Property(x => x.Referencia).HasMaxLength(255);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.FechaDeposito)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.AnuladaAt)
            .HasColumnType("timestamp with time zone");
        FinanzasEf.Money(b, "Valor");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDeposito }).IsUnique()
            .HasDatabaseName("ux_depositos_caja_banco_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CajaSesion).WithMany()
            .HasForeignKey(x => x.CajaSesionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaBancaria).WithMany()
            .HasForeignKey(x => new { x.CuentaBancariaId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
