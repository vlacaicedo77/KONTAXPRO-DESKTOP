using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class TransferenciaInventarioConfiguration
    : IEntityTypeConfiguration<TransferenciaInventario>
{
    public void Configure(EntityTypeBuilder<TransferenciaInventario> b)
    {
        b.ToTable("transferencias_inventario", "s_inventario", t =>
            t.HasCheckConstraint("ck_transferencias_inventario_bodegas",
                "bodega_origen_id <> bodega_destino_id"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.EstablecimientoOrigenId)
            .HasColumnName("establecimiento_origen_id").IsRequired();
        b.Property(x => x.EstablecimientoDestinoId)
            .HasColumnName("establecimiento_destino_id").IsRequired();
        b.Property(x => x.BodegaOrigenId)
            .HasColumnName("bodega_origen_id").IsRequired();
        b.Property(x => x.BodegaDestinoId)
            .HasColumnName("bodega_destino_id").IsRequired();
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        b.Property(x => x.NumeroTransferencia)
            .HasColumnName("numero_transferencia").HasMaxLength(64).IsRequired();
        b.Property(x => x.FechaTransferencia)
            .HasColumnName("fecha_transferencia")
            .HasColumnType("timestamp with time zone").IsRequired();
        Workflow(b);
        b.HasIndex(x => new { x.EmpresaId, x.NumeroTransferencia }).IsUnique()
            .HasDatabaseName("ux_transferencias_inventario_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EstablecimientoOrigen).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoOrigenId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EstablecimientoDestino).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoDestinoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.BodegaOrigen).WithMany()
            .HasForeignKey(x => new { x.BodegaOrigenId, x.EstablecimientoOrigenId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.BodegaDestino).WithMany()
            .HasForeignKey(x => new { x.BodegaDestinoId, x.EstablecimientoDestinoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void Workflow(EntityTypeBuilder<TransferenciaInventario> b)
    {
        b.Property(x => x.Estado).HasColumnName("estado")
            .HasMaxLength(24).IsRequired();
        b.Property(x => x.AnuladoPorUsuarioId)
            .HasColumnName("anulado_por_usuario_id");
        b.Property(x => x.AnuladaAt).HasColumnName("anulada_at")
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion")
            .HasMaxLength(500);
        b.Property(x => x.Observacion).HasColumnName("observacion")
            .HasMaxLength(1000);
        InventarioEf.Timestamps(b);
    }
}

public sealed class TransferenciaInventarioDetalleConfiguration
    : IEntityTypeConfiguration<TransferenciaInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<TransferenciaInventarioDetalle> b)
    {
        b.ToTable("transferencias_inventario_detalles", "s_inventario", t =>
            t.HasCheckConstraint("ck_transferencias_inventario_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.TransferenciaInventarioId)
            .HasColumnName("transferencia_inventario_id").IsRequired();
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.ProductoPresentacionId)
            .HasColumnName("producto_presentacion_id").IsRequired();
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.CantidadPresentacion, "cantidad_presentacion");
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.FactorConversion, "factor_conversion");
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.CantidadBase, "cantidad_base");
        InventarioEf.Timestamps(b);
        b.HasOne(x => x.TransferenciaInventario).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.TransferenciaInventarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany()
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AjusteInventarioConfiguration
    : IEntityTypeConfiguration<AjusteInventario>
{
    public void Configure(EntityTypeBuilder<AjusteInventario> b)
    {
        b.ToTable("ajustes_inventario", "s_inventario", t =>
            t.HasCheckConstraint("ck_ajustes_inventario_tipo",
                "tipo_ajuste IN ('ENTRADA', 'SALIDA')"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id").IsRequired();
        b.Property(x => x.BodegaId).HasColumnName("bodega_id").IsRequired();
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        b.Property(x => x.NumeroAjuste).HasColumnName("numero_ajuste")
            .HasMaxLength(64).IsRequired();
        b.Property(x => x.TipoAjuste).HasColumnName("tipo_ajuste")
            .HasMaxLength(16).IsRequired();
        b.Property(x => x.FechaAjuste).HasColumnName("fecha_ajuste")
            .HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.MotivoOperacionInventarioId)
            .HasColumnName("motivo_operacion_inventario_id").IsRequired();
        b.Property(x => x.Motivo).HasColumnName("motivo")
            .HasMaxLength(500).IsRequired();
        b.Property(x => x.Estado).HasColumnName("estado")
            .HasMaxLength(24).IsRequired();
        b.Property(x => x.AnuladoPorUsuarioId)
            .HasColumnName("anulado_por_usuario_id");
        b.Property(x => x.AnuladaAt).HasColumnName("anulada_at")
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion")
            .HasMaxLength(500);
        InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.EmpresaId, x.NumeroAjuste }).IsUnique()
            .HasDatabaseName("ux_ajustes_inventario_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany()
            .HasForeignKey(x => new { x.BodegaId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MotivoOperacionInventario).WithMany(x => x.Ajustes)
            .HasForeignKey(x => x.MotivoOperacionInventarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AjusteInventarioDetalleConfiguration
    : IEntityTypeConfiguration<AjusteInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<AjusteInventarioDetalle> b)
    {
        b.ToTable("ajustes_inventario_detalles", "s_inventario", t =>
            t.HasCheckConstraint("ck_ajustes_inventario_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.AjusteInventarioId)
            .HasColumnName("ajuste_inventario_id").IsRequired();
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.ProductoPresentacionId)
            .HasColumnName("producto_presentacion_id").IsRequired();
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.CantidadPresentacion, "cantidad_presentacion");
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.FactorConversion, "factor_conversion");
        MovimientoInventarioDetalleConfiguration.Decimal6(
            b, x => x.CantidadBase, "cantidad_base");
        InventarioEf.Timestamps(b);
        b.HasOne(x => x.AjusteInventario).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.AjusteInventarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany()
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConversionControlInventarioConfiguration
    : IEntityTypeConfiguration<ConversionControlInventario>
{
    public void Configure(EntityTypeBuilder<ConversionControlInventario> b)
    {
        b.ToTable("conversiones_control_inventario", "s_inventario", table =>
        {
            table.HasCheckConstraint("ck_conversion_control_tipos", "tipo_control_anterior IN ('NORMAL','LOTE','SERIE','LOTE_Y_SERIE') AND tipo_control_nuevo IN ('NORMAL','LOTE','SERIE','LOTE_Y_SERIE') AND tipo_control_anterior <> tipo_control_nuevo");
            table.HasCheckConstraint("ck_conversion_control_motivo", "length(btrim(motivo)) > 0");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.TipoControlAnterior).HasColumnName("tipo_control_anterior").HasMaxLength(20).IsRequired();
        b.Property(x => x.TipoControlNuevo).HasColumnName("tipo_control_nuevo").HasMaxLength(20).IsRequired();
        b.Property(x => x.FechaConversion).HasColumnName("fecha_conversion").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        b.Property(x => x.MotivoOperacionInventarioId).HasColumnName("motivo_operacion_inventario_id").IsRequired();
        b.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(500).IsRequired();
        b.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsRequired();
        b.Property(x => x.AnuladoPorUsuarioId).HasColumnName("anulado_por_usuario_id");
        b.Property(x => x.AnuladoAt).HasColumnName("anulado_at").HasColumnType("timestamp with time zone");
        b.Property(x => x.MotivoAnulacion).HasColumnName("motivo_anulacion").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany().HasForeignKey(x => x.AnuladoPorUsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MotivoOperacionInventario).WithMany(x => x.Conversiones).HasForeignKey(x => x.MotivoOperacionInventarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EmpresaId, x.ProductoId, x.FechaConversion });
    }
}

public sealed class ConversionControlInventarioDetalleConfiguration
    : IEntityTypeConfiguration<ConversionControlInventarioDetalle>
{
    public void Configure(EntityTypeBuilder<ConversionControlInventarioDetalle> b)
    {
        b.ToTable("conversiones_control_inventario_detalles", "s_inventario", table =>
            table.HasCheckConstraint("ck_conversion_control_detalle_cantidad", "cantidad_base >= 0"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.ConversionControlInventarioId).HasColumnName("conversion_control_inventario_id").IsRequired();
        b.Property(x => x.BodegaId).HasColumnName("bodega_id").IsRequired();
        b.Property(x => x.ProductoLoteId).HasColumnName("producto_lote_id");
        b.Property(x => x.CantidadBase).HasColumnName("cantidad_base").HasPrecision(18, 6).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.HasOne(x => x.ConversionControlInventario).WithMany(x => x.Detalles).HasForeignKey(x => x.ConversionControlInventarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany().HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoLote).WithMany().HasForeignKey(x => x.ProductoLoteId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConversionControlInventarioSerieConfiguration
    : IEntityTypeConfiguration<ConversionControlInventarioSerie>
{
    public void Configure(EntityTypeBuilder<ConversionControlInventarioSerie> b)
    {
        b.ToTable("conversiones_control_inventario_series", "s_inventario");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.ConversionControlInventarioId).HasColumnName("conversion_control_inventario_id").IsRequired();
        b.Property(x => x.ProductoSerieId).HasColumnName("producto_serie_id").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.HasIndex(x => new { x.ConversionControlInventarioId, x.ProductoSerieId }).IsUnique();
        b.HasOne(x => x.ConversionControlInventario).WithMany(x => x.Series).HasForeignKey(x => x.ConversionControlInventarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoSerie).WithMany().HasForeignKey(x => x.ProductoSerieId).OnDelete(DeleteBehavior.Restrict);
    }
}
