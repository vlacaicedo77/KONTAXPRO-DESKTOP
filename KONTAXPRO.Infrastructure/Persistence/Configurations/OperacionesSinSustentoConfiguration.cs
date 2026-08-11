using KONTAXPRO.Domain.Entities.Tesoreria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class OperacionSinSustentoConfiguration
    : IEntityTypeConfiguration<OperacionSinSustento>
{
    public void Configure(EntityTypeBuilder<OperacionSinSustento> b)
    {
        FinanzasEf.Base(b, "operaciones_sin_sustento", "s_tesoreria");
        b.ToTable("operaciones_sin_sustento", "s_tesoreria", t =>
        {
            t.HasCheckConstraint("ck_operaciones_sin_sustento_tipo",
                "tipo_operacion IN ('GASTO','INVENTARIO')");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_medio",
                "medio_salida IN ('CAJA','BANCO')");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_fondo",
                "(medio_salida = 'CAJA' AND caja_sesion_id IS NOT NULL AND cuenta_bancaria_id IS NULL) OR " +
                "(medio_salida = 'BANCO' AND cuenta_bancaria_id IS NOT NULL AND caja_sesion_id IS NULL)");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_inventario",
                "(tipo_operacion = 'GASTO' AND bodega_id IS NULL AND movimiento_inventario_id IS NULL) OR " +
                "(tipo_operacion = 'INVENTARIO' AND bodega_id IS NOT NULL AND movimiento_inventario_id IS NOT NULL)");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_no_deducible",
                "es_deducible = FALSE");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_total", "total > 0");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_estado",
                "estado IN ('CONFIRMADO','ANULADO')");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_evidencia",
                "(evidencia_ruta_relativa IS NULL AND evidencia_sha256 IS NULL AND evidencia_tamano IS NULL) OR " +
                "(evidencia_ruta_relativa IS NOT NULL AND evidencia_sha256 IS NOT NULL AND evidencia_tamano > 0)");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_operaciones_sin_sustento_id_empresa");
        b.Property(x => x.NumeroOperacion).HasMaxLength(64).IsRequired();
        b.Property(x => x.TipoOperacion).HasMaxLength(16).IsRequired();
        b.Property(x => x.MedioSalida).HasMaxLength(16).IsRequired();
        b.Property(x => x.Beneficiario).HasMaxLength(250).IsRequired();
        b.Property(x => x.Motivo).HasMaxLength(1000).IsRequired();
        b.Property(x => x.Referencia).HasMaxLength(255);
        b.Property(x => x.EvidenciaRutaRelativa).HasMaxLength(500);
        b.Property(x => x.EvidenciaNombre).HasMaxLength(255);
        b.Property(x => x.EvidenciaSha256).HasMaxLength(64);
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Fecha).HasColumnType("date");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.Total).HasPrecision(18, 2);
        b.HasIndex(x => new { x.EmpresaId, x.NumeroOperacion }).IsUnique()
            .HasDatabaseName("ux_operaciones_sin_sustento_empresa_numero");
        b.HasIndex(x => x.MovimientoCajaId).IsUnique()
            .HasFilter("movimiento_caja_id IS NOT NULL");
        b.HasIndex(x => x.MovimientoBancarioId).IsUnique()
            .HasFilter("movimiento_bancario_id IS NOT NULL");
        b.HasIndex(x => x.MovimientoInventarioId).IsUnique()
            .HasFilter("movimiento_inventario_id IS NOT NULL");
        b.HasIndex(x => x.AsientoId).IsUnique();
        b.HasIndex(x => x.OperacionSustituidaId).IsUnique()
            .HasFilter("operacion_sustituida_id IS NOT NULL");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CajaSesion).WithMany().HasForeignKey(x => x.CajaSesionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaBancaria).WithMany()
            .HasForeignKey(x => x.CuentaBancariaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany().HasForeignKey(x => x.BodegaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoCaja).WithMany()
            .HasForeignKey(x => x.MovimientoCajaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoBancario).WithMany()
            .HasForeignKey(x => x.MovimientoBancarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MovimientoInventario).WithMany()
            .HasForeignKey(x => x.MovimientoInventarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Asiento).WithMany().HasForeignKey(x => x.AsientoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperacionSustituida).WithOne(x => x.OperacionSustituta)
            .HasForeignKey<OperacionSinSustento>(x => x.OperacionSustituidaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OperacionSinSustentoDetalleConfiguration
    : IEntityTypeConfiguration<OperacionSinSustentoDetalle>
{
    public void Configure(EntityTypeBuilder<OperacionSinSustentoDetalle> b)
    {
        FinanzasEf.Base(b, "operaciones_sin_sustento_detalles", "s_tesoreria", false);
        b.ToTable("operaciones_sin_sustento_detalles", "s_tesoreria", t =>
        {
            t.HasCheckConstraint("ck_operaciones_sin_sustento_detalle_costo",
                "costo_total > 0 AND cantidad_presentacion > 0 AND factor_conversion > 0 AND cantidad_base > 0 AND costo_unitario_base >= 0");
            t.HasCheckConstraint("ck_operaciones_sin_sustento_detalle_destino",
                "(cuenta_contable_id IS NOT NULL AND producto_id IS NULL AND producto_presentacion_id IS NULL) OR " +
                "(cuenta_contable_id IS NULL AND producto_id IS NOT NULL AND producto_presentacion_id IS NOT NULL)");
        });
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.Property(x => x.CantidadPresentacion).HasPrecision(18, 6);
        b.Property(x => x.FactorConversion).HasPrecision(18, 6);
        b.Property(x => x.CantidadBase).HasPrecision(18, 6);
        b.Property(x => x.CostoUnitarioBase).HasPrecision(18, 6);
        b.Property(x => x.CostoTotal).HasPrecision(18, 2);
        b.HasOne(x => x.OperacionSinSustento).WithMany(x => x.Detalles)
            .HasForeignKey(x => new { x.OperacionSinSustentoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CuentaContable).WithMany()
            .HasForeignKey(x => new { x.CuentaContableId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany()
            .HasForeignKey(x => new { x.ProductoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => new { x.ProductoPresentacionId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
