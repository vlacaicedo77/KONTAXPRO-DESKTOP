using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class InventarioEf
{
    internal static void Id<TEntity>(EntityTypeBuilder<TEntity> b)
        where TEntity : class =>
        b.Property<long>("Id").HasColumnName("id")
            .UseIdentityByDefaultColumn();

    internal static void Timestamps<TEntity>(
        EntityTypeBuilder<TEntity> b,
        bool updated = true)
        where TEntity : class
    {
        b.Property<DateTime>("CreatedAt").HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        if (updated)
            b.Property<DateTime?>("UpdatedAt").HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone");
    }

    internal static void Estado<TEntity>(EntityTypeBuilder<TEntity> b)
        where TEntity : class =>
        b.Property<int>("Estado").HasColumnName("estado")
            .HasDefaultValue(1).IsRequired();
}

public sealed class ProductoConfiguration
    : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> b)
    {
        b.ToTable("productos", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_productos_estado", "estado IN (0, 1)");
            t.HasCheckConstraint(
                "ck_productos_tipo", "tipo_producto IN ('PRODUCTO', 'SERVICIO')");
            t.HasCheckConstraint(
                "ck_productos_alerta_caducidad",
                "dias_alerta_caducidad IS NULL OR dias_alerta_caducidad >= 0");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.Uuid).HasColumnName("uuid")
            .HasColumnType("uuid").IsRequired();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.CategoriaProductoId)
            .HasColumnName("categoria_producto_id");
        b.Property(x => x.MarcaId).HasColumnName("marca_id");
        b.Property(x => x.UnidadMedidaBaseId)
            .HasColumnName("unidad_medida_base_id").IsRequired();
        b.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(64).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(200).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(1000);
        b.Property(x => x.Modelo).HasColumnName("modelo").HasMaxLength(150);
        b.Property(x => x.TipoProducto).HasColumnName("tipo_producto")
            .HasMaxLength(16).IsRequired();
        b.Property(x => x.ManejaInventario)
            .HasColumnName("maneja_inventario").IsRequired();
        b.Property(x => x.ManejaLotes).HasColumnName("maneja_lotes").IsRequired();
        b.Property(x => x.ManejaSeries).HasColumnName("maneja_series").IsRequired();
        b.Property(x => x.ManejaFechaCaducidad)
            .HasColumnName("maneja_fecha_caducidad").IsRequired();
        b.Property(x => x.AlertaCaducidad)
            .HasColumnName("alerta_caducidad").IsRequired();
        b.Property(x => x.DiasAlertaCaducidad)
            .HasColumnName("dias_alerta_caducidad");
        b.Property(x => x.Observacion).HasColumnName("observacion")
            .HasMaxLength(1000);
        InventarioEf.Estado(b); InventarioEf.Timestamps(b);
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_productos_id_empresa");
        b.HasIndex(x => x.Uuid).IsUnique()
            .HasDatabaseName("ux_productos_uuid");
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique()
            .HasDatabaseName("ux_productos_empresa_codigo");
        b.HasOne(x => x.Empresa).WithMany(x => x.Productos)
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CategoriaProducto).WithMany(x => x.Productos)
            .HasForeignKey(x => new { x.CategoriaProductoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Marca).WithMany()
            .HasForeignKey(x => x.MarcaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UnidadMedidaBase).WithMany()
            .HasForeignKey(x => x.UnidadMedidaBaseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoPresentacionConfiguration
    : IEntityTypeConfiguration<ProductoPresentacion>
{
    public void Configure(EntityTypeBuilder<ProductoPresentacion> b)
    {
        b.ToTable("productos_presentaciones", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_productos_presentaciones_estado",
                "estado IN (0, 1)");
            t.HasCheckConstraint("ck_productos_presentaciones_factor",
                "factor_conversion > 0");
            t.HasCheckConstraint("ck_productos_presentaciones_base_factor",
                "NOT es_presentacion_base OR factor_conversion = 1");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.HasAlternateKey(x => new { x.Id, x.ProductoId, x.EmpresaId })
            .HasName("ak_productos_presentaciones_id_producto_empresa");
        b.Property(x => x.Uuid).HasColumnName("uuid")
            .HasColumnType("uuid").IsRequired();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(64).IsRequired();
        b.Property(x => x.CodigoBarras).HasColumnName("codigo_barras")
            .HasMaxLength(128);
        b.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        b.Property(x => x.FactorConversion).HasColumnName("factor_conversion")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.EsPresentacionBase)
            .HasColumnName("es_presentacion_base").IsRequired();
        b.Property(x => x.PermiteCompra)
            .HasColumnName("permite_compra").IsRequired();
        b.Property(x => x.PermiteVenta)
            .HasColumnName("permite_venta").IsRequired();
        InventarioEf.Estado(b); InventarioEf.Timestamps(b);
        b.HasIndex(x => x.Uuid).IsUnique()
            .HasDatabaseName("ux_productos_presentaciones_uuid");
        b.HasIndex(x => new { x.ProductoId, x.Codigo }).IsUnique()
            .HasDatabaseName("ux_productos_presentaciones_producto_codigo");
        b.HasIndex(x => new { x.EmpresaId, x.CodigoBarras }).IsUnique()
            .HasFilter("codigo_barras IS NOT NULL")
            .HasDatabaseName("ux_productos_presentaciones_empresa_barcode");
        b.HasIndex(x => x.ProductoId).IsUnique()
            .HasFilter("es_presentacion_base")
            .HasDatabaseName("ux_productos_presentaciones_base");
        b.HasOne(x => x.Producto).WithMany(x => x.Presentaciones)
            .HasForeignKey(x => new { x.ProductoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoImpuestoConfiguration
    : IEntityTypeConfiguration<ProductoImpuesto>
{
    public void Configure(EntityTypeBuilder<ProductoImpuesto> b)
    {
        b.ToTable("productos_impuestos", "s_inventario", t =>
            t.HasCheckConstraint(
                "ck_productos_impuestos_estado",
                "estado IN (0, 1)"));
        b.HasKey(x => x.Id);
        InventarioEf.Id(b);
        b.Property(x => x.ProductoId)
            .HasColumnName("producto_id").IsRequired();
        b.Property(x => x.TarifaImpuestoId)
            .HasColumnName("tarifa_impuesto_id").IsRequired();
        InventarioEf.Estado(b);
        InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.ProductoId, x.TarifaImpuestoId })
            .IsUnique()
            .HasDatabaseName("ux_productos_impuestos_producto_tarifa");
        b.HasOne(x => x.Producto)
            .WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto)
            .WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ListaPrecioConfiguration
    : IEntityTypeConfiguration<ListaPrecio>
{
    public void Configure(EntityTypeBuilder<ListaPrecio> b)
    {
        b.ToTable("listas_precio", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_listas_precio_estado", "estado IN (0, 1)");
            t.HasCheckConstraint("ck_listas_precio_descuento",
                "porcentaje_descuento_predeterminado IS NULL OR " +
                "porcentaje_descuento_predeterminado BETWEEN 0 AND 100");
            t.HasCheckConstraint("ck_listas_precio_orden", "orden >= 0");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_listas_precio_id_empresa");
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(64).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        b.Property(x => x.EsListaBase).HasColumnName("es_lista_base").IsRequired();
        b.Property(x => x.PorcentajeDescuentoPredeterminado)
            .HasColumnName("porcentaje_descuento_predeterminado")
            .HasPrecision(18, 6);
        b.Property(x => x.Orden).HasColumnName("orden").IsRequired();
        InventarioEf.Estado(b); InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique()
            .HasDatabaseName("ux_listas_precio_empresa_codigo");
        b.HasIndex(x => x.EmpresaId).IsUnique().HasFilter("es_lista_base")
            .HasDatabaseName("ux_listas_precio_empresa_base");
        b.HasOne(x => x.Empresa).WithMany(x => x.ListasPrecio)
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoPresentacionPrecioConfiguration
    : IEntityTypeConfiguration<ProductoPresentacionPrecio>
{
    public void Configure(EntityTypeBuilder<ProductoPresentacionPrecio> b)
    {
        b.ToTable("productos_presentaciones_precios", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_productos_presentaciones_precios_estado",
                "estado IN (0, 1)");
            t.HasCheckConstraint("ck_productos_presentaciones_precios_metodo",
                "metodo_calculo IN ('PORCENTAJE_COSTO', 'PRECIO_FIJO', " +
                "'DESCUENTO_PORCENTAJE')");
            t.HasCheckConstraint("ck_productos_presentaciones_precios_valores",
                "(metodo_calculo IN ('PORCENTAJE_COSTO', 'DESCUENTO_PORCENTAJE') " +
                "AND porcentaje IS NOT NULL AND precio IS NULL) OR " +
                "(metodo_calculo = 'PRECIO_FIJO' AND precio IS NOT NULL)");
            t.HasCheckConstraint("ck_productos_presentaciones_precios_no_negativo",
                "(porcentaje IS NULL OR porcentaje >= 0) AND " +
                "(precio IS NULL OR precio >= 0)");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.ProductoPresentacionId)
            .HasColumnName("producto_presentacion_id").IsRequired();
        b.Property(x => x.ListaPrecioId)
            .HasColumnName("lista_precio_id").IsRequired();
        b.Property(x => x.MetodoCalculo).HasColumnName("metodo_calculo")
            .HasMaxLength(32).IsRequired();
        b.Property(x => x.Porcentaje).HasColumnName("porcentaje")
            .HasPrecision(18, 6);
        b.Property(x => x.Precio).HasColumnName("precio").HasPrecision(18, 2);
        InventarioEf.Estado(b); InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.ProductoPresentacionId, x.ListaPrecioId })
            .IsUnique()
            .HasDatabaseName("ux_productos_presentaciones_precios_par");
        b.HasOne(x => x.ProductoPresentacion).WithMany(x => x.Precios)
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ListaPrecio).WithMany(x => x.Precios)
            .HasForeignKey(x => x.ListaPrecioId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoCostoConfiguration
    : IEntityTypeConfiguration<ProductoCosto>
{
    public void Configure(EntityTypeBuilder<ProductoCosto> b)
    {
        b.ToTable("productos_costos", "s_inventario", t =>
            t.HasCheckConstraint("ck_productos_costos_no_negativos",
                "ultimo_precio_compra >= 0 AND ultimo_costo_efectivo >= 0 " +
                "AND costo_promedio >= 0"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.UltimoPrecioCompra).HasColumnName("ultimo_precio_compra")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.UltimoCostoEfectivo).HasColumnName("ultimo_costo_efectivo")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.CostoPromedio).HasColumnName("costo_promedio")
            .HasPrecision(18, 6).IsRequired();
        InventarioEf.Timestamps(b);
        b.HasIndex(x => x.ProductoId).IsUnique()
            .HasDatabaseName("ux_productos_costos_producto");
        b.HasOne(x => x.Producto).WithOne(x => x.Costo)
            .HasForeignKey<ProductoCosto>(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BodegaConfiguration : IEntityTypeConfiguration<Bodega>
{
    public void Configure(EntityTypeBuilder<Bodega> b)
    {
        b.ToTable("bodegas", "s_inventario", t =>
            t.HasCheckConstraint("ck_bodegas_estado", "estado IN (0, 1)"));
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.HasAlternateKey(x => new { x.Id, x.EstablecimientoId })
            .HasName("ak_bodegas_id_establecimiento");
        b.Property(x => x.EstablecimientoId)
            .HasColumnName("establecimiento_id").IsRequired();
        b.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(64).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        b.Property(x => x.PermiteTransferenciasInternas)
            .HasColumnName("permite_transferencias_internas").IsRequired();
        b.Property(x => x.PermiteVentaFacturada)
            .HasColumnName("permite_venta_facturada").IsRequired();
        InventarioEf.Estado(b); InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.EstablecimientoId, x.Codigo }).IsUnique()
            .HasDatabaseName("ux_bodegas_establecimiento_codigo");
        b.HasOne(x => x.Establecimiento).WithMany(x => x.Bodegas)
            .HasForeignKey(x => x.EstablecimientoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductoExistenciaConfiguration
    : IEntityTypeConfiguration<ProductoExistencia>
{
    public void Configure(EntityTypeBuilder<ProductoExistencia> b)
    {
        b.ToTable("productos_existencias", "s_inventario", t =>
        {
            t.HasCheckConstraint("ck_productos_existencias_reservado",
                "stock_reservado >= 0");
            t.HasCheckConstraint("ck_productos_existencias_minimo",
                "stock_minimo >= 0");
        });
        b.HasKey(x => x.Id); InventarioEf.Id(b);
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.BodegaId).HasColumnName("bodega_id").IsRequired();
        b.Property(x => x.StockActual).HasColumnName("stock_actual")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.StockReservado).HasColumnName("stock_reservado")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.StockMinimo).HasColumnName("stock_minimo")
            .HasPrecision(18, 6).IsRequired();
        b.Property(x => x.Ubicacion).HasColumnName("ubicacion").HasMaxLength(255);
        b.Ignore(x => x.StockDisponible); InventarioEf.Timestamps(b);
        b.HasIndex(x => new { x.ProductoId, x.BodegaId }).IsUnique()
            .HasDatabaseName("ux_productos_existencias_producto_bodega");
        b.HasOne(x => x.Producto).WithMany(x => x.Existencias)
            .HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Bodega).WithMany(x => x.ProductosExistencias)
            .HasForeignKey(x => x.BodegaId).OnDelete(DeleteBehavior.Restrict);
    }
}
