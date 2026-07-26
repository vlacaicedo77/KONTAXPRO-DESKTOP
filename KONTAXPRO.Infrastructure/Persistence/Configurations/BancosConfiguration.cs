using KONTAXPRO.Domain.Entities.Bancos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class CuentaBancariaConfiguration
    : IEntityTypeConfiguration<CuentaBancaria>
{
    public void Configure(EntityTypeBuilder<CuentaBancaria> b)
    {
        FinanzasEf.Base(b, "cuentas_bancarias", "s_bancos");
        b.ToTable("cuentas_bancarias", "s_bancos", t =>
        {
            t.HasCheckConstraint("ck_cuentas_bancarias_tipo",
                "tipo_cuenta IN ('CORRIENTE', 'AHORROS', 'OTRA')");
            t.HasCheckConstraint("ck_cuentas_bancarias_estado",
                "estado IN (0, 1)");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_cuentas_bancarias_id_empresa");
        b.Property(x => x.Banco).HasMaxLength(150).IsRequired();
        b.Property(x => x.TipoCuenta).HasMaxLength(16).IsRequired();
        b.Property(x => x.NumeroCuenta).HasMaxLength(64).IsRequired();
        b.Property(x => x.Nombre).HasMaxLength(150).IsRequired();
        b.HasIndex(x => new { x.EmpresaId, x.Banco, x.NumeroCuenta })
            .IsUnique()
            .HasDatabaseName("ux_cuentas_bancarias_empresa_banco_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaContable).WithMany()
            .HasForeignKey(x => new { x.CuentaContableId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MovimientoBancarioConfiguration
    : IEntityTypeConfiguration<MovimientoBancario>
{
    public void Configure(EntityTypeBuilder<MovimientoBancario> b)
    {
        FinanzasEf.Base(b, "movimientos_bancarios", "s_bancos", false);
        b.ToTable("movimientos_bancarios", "s_bancos", t =>
        {
            t.HasCheckConstraint("ck_movimientos_bancarios_valor", "valor > 0");
            t.HasCheckConstraint("ck_movimientos_bancarios_medio",
                "NOT (cobro_medio_id IS NOT NULL AND pago_medio_id IS NOT NULL)");
        });
        b.Property(x => x.OrigenTipo).HasMaxLength(32);
        b.Property(x => x.Referencia).HasMaxLength(255);
        b.Property(x => x.Concepto).HasMaxLength(500).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.FechaMovimiento)
            .HasColumnType("timestamp with time zone");
        FinanzasEf.Money(b, "Valor");
        b.HasIndex(x => x.MovimientoReversoId).IsUnique()
            .HasFilter("movimiento_reverso_id IS NOT NULL")
            .HasDatabaseName("ux_movimientos_bancarios_reverso");
        b.HasOne(x => x.CuentaBancaria).WithMany(x => x.Movimientos)
            .HasForeignKey(x => x.CuentaBancariaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoMovimientoBancario).WithMany()
            .HasForeignKey(x => x.TipoMovimientoBancarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CobroMedio).WithMany()
            .HasForeignKey(x => x.CobroMedioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PagoMedio).WithMany()
            .HasForeignKey(x => x.PagoMedioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoReverso)
            .WithOne(x => x.MovimientoOrigenReversado)
            .HasForeignKey<MovimientoBancario>(x => x.MovimientoReversoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TransferenciaBancariaConfiguration
    : IEntityTypeConfiguration<TransferenciaBancaria>
{
    public void Configure(EntityTypeBuilder<TransferenciaBancaria> b)
    {
        FinanzasEf.Base(b, "transferencias_bancarias", "s_bancos");
        b.ToTable("transferencias_bancarias", "s_bancos", t =>
        {
            t.HasCheckConstraint("ck_transferencias_bancarias_valor",
                "valor > 0");
            t.HasCheckConstraint("ck_transferencias_bancarias_cuentas",
                "cuenta_bancaria_origen_id <> cuenta_bancaria_destino_id");
        });
        b.Property(x => x.NumeroTransferencia).HasMaxLength(64).IsRequired();
        b.Property(x => x.Referencia).HasMaxLength(255);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.FechaTransferencia)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.AnuladaAt)
            .HasColumnType("timestamp with time zone");
        FinanzasEf.Money(b, "Valor");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroTransferencia }).IsUnique()
            .HasDatabaseName("ux_transferencias_bancarias_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaBancariaOrigen).WithMany()
            .HasForeignKey(x => new { x.CuentaBancariaOrigenId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaBancariaDestino).WithMany()
            .HasForeignKey(x => new { x.CuentaBancariaDestinoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
