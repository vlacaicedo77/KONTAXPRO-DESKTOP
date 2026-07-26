using System.Text;
using KONTAXPRO.Domain.Entities.Cartera;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class CarteraEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        bool updated = true) where TEntity : class
    {
        b.ToTable(table, "s_cartera");
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

    internal static void Cuenta<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table) where TEntity : class
    {
        Base(b, table);
        Money(b, "ValorOriginal", "SaldoActual");
        b.Property<string>("OrigenTipo").HasMaxLength(32).IsRequired();
        b.Property<string>("Estado").HasMaxLength(16).IsRequired();
        b.Property<DateOnly>("FechaOrigen").HasColumnType("date");
        b.Property<DateOnly?>("FechaVencimiento").HasColumnType("date");
    }

    internal static void Documento<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        string numberProperty,
        string dateProperty) where TEntity : class
    {
        Base(b, table);
        Money(b, "ValorTotal");
        b.Property<string>(numberProperty).HasMaxLength(64).IsRequired();
        b.Property<DateTime>(dateProperty)
            .HasColumnType("timestamp with time zone");
        b.Property<string>("Referencia").HasMaxLength(255);
        b.Property<string>("Observacion").HasMaxLength(1000);
        b.Property<string>("Estado").HasMaxLength(24).IsRequired();
        b.Property<DateTime?>("AnuladaAt")
            .HasColumnType("timestamp with time zone");
        b.Property<string>("MotivoAnulacion").HasMaxLength(500);
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

public sealed class CuentaPorCobrarConfiguration
    : IEntityTypeConfiguration<CuentaPorCobrar>
{
    public void Configure(EntityTypeBuilder<CuentaPorCobrar> b)
    {
        CarteraEf.Cuenta(b, "cuentas_por_cobrar");
        b.ToTable("cuentas_por_cobrar", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_cuentas_por_cobrar_valor",
                "valor_original > 0");
            t.HasCheckConstraint("ck_cuentas_por_cobrar_saldo",
                "saldo_actual >= 0");
            t.HasCheckConstraint("ck_cuentas_por_cobrar_estado",
                "estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')");
        });
        b.HasIndex(x => new { x.EmpresaId, x.OrigenTipo, x.OrigenId })
            .IsUnique().HasDatabaseName("ux_cuentas_por_cobrar_origen");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CobroConfiguration : IEntityTypeConfiguration<Cobro>
{
    public void Configure(EntityTypeBuilder<Cobro> b)
    {
        CarteraEf.Documento(b, "cobros", "NumeroCobro", "FechaCobro");
        b.ToTable("cobros", "s_cartera", t =>
            t.HasCheckConstraint("ck_cobros_valor", "valor_total > 0"));
        b.HasIndex(x => new { x.EmpresaId, x.NumeroCobro }).IsUnique()
            .HasDatabaseName("ux_cobros_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CobroMedioConfiguration
    : IEntityTypeConfiguration<CobroMedio>
{
    public void Configure(EntityTypeBuilder<CobroMedio> b)
    {
        CarteraEf.Base(b, "cobros_medios", false);
        CarteraEf.Money(b, "Valor");
        b.Property(x => x.Referencia).HasMaxLength(255);
        b.ToTable("cobros_medios", "s_cartera", t =>
            t.HasCheckConstraint("ck_cobros_medios_valor", "valor > 0"));
        b.HasOne(x => x.Cobro).WithMany(x => x.Medios)
            .HasForeignKey(x => x.CobroId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MedioPago).WithMany()
            .HasForeignKey(x => x.MedioPagoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CobroAplicacionConfiguration
    : IEntityTypeConfiguration<CobroAplicacion>
{
    public void Configure(EntityTypeBuilder<CobroAplicacion> b)
    {
        CarteraEf.Base(b, "cobros_aplicaciones", false);
        CarteraEf.Money(b, "ValorAplicado", "SaldoAnterior", "SaldoNuevo");
        b.ToTable("cobros_aplicaciones", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_cobros_aplicaciones_valor",
                "valor_aplicado > 0");
            t.HasCheckConstraint("ck_cobros_aplicaciones_saldos",
                "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND " +
                "saldo_nuevo = saldo_anterior - valor_aplicado");
        });
        b.HasIndex(x => new { x.CobroId, x.CuentaPorCobrarId }).IsUnique()
            .HasDatabaseName("ux_cobros_aplicaciones_cobro_cuenta");
        b.HasOne(x => x.Cobro).WithMany(x => x.Aplicaciones)
            .HasForeignKey(x => x.CobroId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaPorCobrar).WithMany(x => x.Aplicaciones)
            .HasForeignKey(x => x.CuentaPorCobrarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CobroAplicacionReversoConfiguration
    : IEntityTypeConfiguration<CobroAplicacionReverso>
{
    public void Configure(EntityTypeBuilder<CobroAplicacionReverso> b)
    {
        CarteraEf.Base(b, "cobros_aplicaciones_reversos", false);
        CarteraEf.Money(b, "ValorReversado", "SaldoAnterior", "SaldoNuevo");
        b.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        b.ToTable("cobros_aplicaciones_reversos", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_cobros_aplicaciones_reversos_valor",
                "valor_reversado > 0");
            t.HasCheckConstraint("ck_cobros_aplicaciones_reversos_saldos",
                "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND " +
                "saldo_nuevo = saldo_anterior + valor_reversado");
        });
        b.HasIndex(x => x.CobroAplicacionId).IsUnique()
            .HasDatabaseName("ux_cobros_aplicaciones_reversos_aplicacion");
        b.HasOne(x => x.CobroAplicacion).WithOne(x => x.Reverso)
            .HasForeignKey<CobroAplicacionReverso>(x => x.CobroAplicacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CuentaPorCobrarMovimientoConfiguration
    : IEntityTypeConfiguration<CuentaPorCobrarMovimiento>
{
    public void Configure(EntityTypeBuilder<CuentaPorCobrarMovimiento> b)
    {
        CarteraEf.Base(b, "cuentas_por_cobrar_movimientos", false);
        CarteraEf.Money(b, "Valor", "SaldoAnterior", "SaldoNuevo");
        b.Property(x => x.OrigenTipo).HasMaxLength(32);
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.ToTable("cuentas_por_cobrar_movimientos", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_cuentas_por_cobrar_movimientos_secuencia",
                "secuencia > 0");
            t.HasCheckConstraint("ck_cuentas_por_cobrar_movimientos_valor",
                "valor > 0 AND saldo_anterior >= 0 AND saldo_nuevo >= 0");
            t.HasCheckConstraint("ck_cuentas_por_cobrar_movimientos_aplicacion",
                "NOT (cobro_aplicacion_id IS NOT NULL AND " +
                "reverso_aplicacion_id IS NOT NULL)");
        });
        b.HasIndex(x => new { x.CuentaPorCobrarId, x.Secuencia }).IsUnique()
            .HasDatabaseName("ux_cuentas_por_cobrar_movimientos_secuencia");
        b.HasOne(x => x.CuentaPorCobrar).WithMany(x => x.Movimientos)
            .HasForeignKey(x => x.CuentaPorCobrarId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoMovimientoCartera).WithMany()
            .HasForeignKey(x => x.TipoMovimientoCarteraId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CobroAplicacion).WithMany()
            .HasForeignKey(x => x.CobroAplicacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReversoAplicacion).WithMany()
            .HasForeignKey(x => x.ReversoAplicacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
