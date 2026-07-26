using KONTAXPRO.Domain.Entities.Cartera;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class CuentaPorPagarConfiguration
    : IEntityTypeConfiguration<CuentaPorPagar>
{
    public void Configure(EntityTypeBuilder<CuentaPorPagar> b)
    {
        CarteraEf.Cuenta(b, "cuentas_por_pagar");
        b.ToTable("cuentas_por_pagar", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_cuentas_por_pagar_valor",
                "valor_original > 0");
            t.HasCheckConstraint("ck_cuentas_por_pagar_saldo",
                "saldo_actual >= 0");
            t.HasCheckConstraint("ck_cuentas_por_pagar_estado",
                "estado IN ('PENDIENTE', 'PARCIAL', 'CANCELADA', 'ANULADA')");
        });
        b.HasIndex(x => new { x.EmpresaId, x.OrigenTipo, x.OrigenId })
            .IsUnique().HasDatabaseName("ux_cuentas_por_pagar_origen");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PagoConfiguration : IEntityTypeConfiguration<Pago>
{
    public void Configure(EntityTypeBuilder<Pago> b)
    {
        CarteraEf.Documento(b, "pagos", "NumeroPago", "FechaPago");
        b.ToTable("pagos", "s_cartera", t =>
            t.HasCheckConstraint("ck_pagos_valor", "valor_total > 0"));
        b.HasIndex(x => new { x.EmpresaId, x.NumeroPago }).IsUnique()
            .HasDatabaseName("ux_pagos_empresa_numero");
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

public sealed class PagoMedioConfiguration
    : IEntityTypeConfiguration<PagoMedio>
{
    public void Configure(EntityTypeBuilder<PagoMedio> b)
    {
        CarteraEf.Base(b, "pagos_medios", false);
        CarteraEf.Money(b, "Valor");
        b.Property(x => x.Referencia).HasMaxLength(255);
        b.ToTable("pagos_medios", "s_cartera", t =>
            t.HasCheckConstraint("ck_pagos_medios_valor", "valor > 0"));
        b.HasOne(x => x.Pago).WithMany(x => x.Medios)
            .HasForeignKey(x => x.PagoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MedioPago).WithMany()
            .HasForeignKey(x => x.MedioPagoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PagoAplicacionConfiguration
    : IEntityTypeConfiguration<PagoAplicacion>
{
    public void Configure(EntityTypeBuilder<PagoAplicacion> b)
    {
        CarteraEf.Base(b, "pagos_aplicaciones", false);
        CarteraEf.Money(b, "ValorAplicado", "SaldoAnterior", "SaldoNuevo");
        b.ToTable("pagos_aplicaciones", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_pagos_aplicaciones_valor",
                "valor_aplicado > 0");
            t.HasCheckConstraint("ck_pagos_aplicaciones_saldos",
                "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND " +
                "saldo_nuevo = saldo_anterior - valor_aplicado");
        });
        b.HasIndex(x => new { x.PagoId, x.CuentaPorPagarId }).IsUnique()
            .HasDatabaseName("ux_pagos_aplicaciones_pago_cuenta");
        b.HasOne(x => x.Pago).WithMany(x => x.Aplicaciones)
            .HasForeignKey(x => x.PagoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaPorPagar).WithMany(x => x.Aplicaciones)
            .HasForeignKey(x => x.CuentaPorPagarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PagoAplicacionReversoConfiguration
    : IEntityTypeConfiguration<PagoAplicacionReverso>
{
    public void Configure(EntityTypeBuilder<PagoAplicacionReverso> b)
    {
        CarteraEf.Base(b, "pagos_aplicaciones_reversos", false);
        CarteraEf.Money(b, "ValorReversado", "SaldoAnterior", "SaldoNuevo");
        b.Property(x => x.Motivo).HasMaxLength(500).IsRequired();
        b.ToTable("pagos_aplicaciones_reversos", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_pagos_aplicaciones_reversos_valor",
                "valor_reversado > 0");
            t.HasCheckConstraint("ck_pagos_aplicaciones_reversos_saldos",
                "saldo_anterior >= 0 AND saldo_nuevo >= 0 AND " +
                "saldo_nuevo = saldo_anterior + valor_reversado");
        });
        b.HasIndex(x => x.PagoAplicacionId).IsUnique()
            .HasDatabaseName("ux_pagos_aplicaciones_reversos_aplicacion");
        b.HasOne(x => x.PagoAplicacion).WithOne(x => x.Reverso)
            .HasForeignKey<PagoAplicacionReverso>(x => x.PagoAplicacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CuentaPorPagarMovimientoConfiguration
    : IEntityTypeConfiguration<CuentaPorPagarMovimiento>
{
    public void Configure(EntityTypeBuilder<CuentaPorPagarMovimiento> b)
    {
        CarteraEf.Base(b, "cuentas_por_pagar_movimientos", false);
        CarteraEf.Money(b, "Valor", "SaldoAnterior", "SaldoNuevo");
        b.Property(x => x.OrigenTipo).HasMaxLength(32);
        b.Property(x => x.Descripcion).HasMaxLength(500);
        b.ToTable("cuentas_por_pagar_movimientos", "s_cartera", t =>
        {
            t.HasCheckConstraint("ck_cuentas_por_pagar_movimientos_secuencia",
                "secuencia > 0");
            t.HasCheckConstraint("ck_cuentas_por_pagar_movimientos_valor",
                "valor > 0 AND saldo_anterior >= 0 AND saldo_nuevo >= 0");
            t.HasCheckConstraint("ck_cuentas_por_pagar_movimientos_aplicacion",
                "NOT (pago_aplicacion_id IS NOT NULL AND " +
                "reverso_aplicacion_id IS NOT NULL)");
        });
        b.HasIndex(x => new { x.CuentaPorPagarId, x.Secuencia }).IsUnique()
            .HasDatabaseName("ux_cuentas_por_pagar_movimientos_secuencia");
        b.HasOne(x => x.CuentaPorPagar).WithMany(x => x.Movimientos)
            .HasForeignKey(x => x.CuentaPorPagarId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoMovimientoCuentaPorPagar).WithMany()
            .HasForeignKey(x => x.TipoMovimientoCuentaPorPagarId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PagoAplicacion).WithMany()
            .HasForeignKey(x => x.PagoAplicacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReversoAplicacion).WithMany()
            .HasForeignKey(x => x.ReversoAplicacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
