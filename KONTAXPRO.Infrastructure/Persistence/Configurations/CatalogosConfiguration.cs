using KONTAXPRO.Domain.Entities.Catalogos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class CatalogoConfiguration
{
    internal static void Id<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<long>("Id")
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();
    }

    internal static void EstadoYTimestamps<TEntity>(
        EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        builder.Property<int>("Estado")
            .HasColumnName("estado")
            .HasDefaultValue(1)
            .IsRequired();
        builder.Property<DateTime>("CreatedAt")
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .IsRequired();
        builder.Property<DateTime?>("UpdatedAt")
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
    }

    internal static void Basico<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tabla)
        where TEntity : class, ICatalogoBasico
    {
        builder.ToTable(
            tabla,
            "s_catalogos",
            table => table.HasCheckConstraint(
                $"ck_{tabla}_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        Id(builder);
        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(x => x.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500);
        EstadoYTimestamps(builder);
        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName($"ux_{tabla}_codigo");
    }

    internal static void Numerico<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tabla)
        where TEntity : class, ICatalogoNumerico
    {
        builder.ToTable(
            tabla,
            "s_catalogos",
            table => table.HasCheckConstraint(
                $"ck_{tabla}_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        Id(builder);
        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .IsRequired();
        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(100)
            .IsRequired();
        EstadoYTimestamps(builder);
        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName($"ux_{tabla}_codigo");
    }

    internal static void ConCodigoSri<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tabla)
        where TEntity : class, ICatalogoConCodigoSri
    {
        Basico(builder, tabla);
        builder.Property(x => x.CodigoSri)
            .HasColumnName("codigo_sri")
            .HasMaxLength(16)
            .IsRequired();
        builder.HasIndex(x => x.CodigoSri)
            .IsUnique()
            .HasDatabaseName($"ux_{tabla}_codigo_sri");
    }

    internal static void ConNaturaleza<TEntity>(
        EntityTypeBuilder<TEntity> builder,
        string tabla,
        string valores)
        where TEntity : class, ICatalogoConNaturaleza
    {
        Basico(builder, tabla);
        builder.Property(x => x.Naturaleza)
            .HasColumnName("naturaleza")
            .HasMaxLength(16)
            .IsRequired();
        builder.ToTable(
            tabla,
            "s_catalogos",
            table => table.HasCheckConstraint(
                $"ck_{tabla}_naturaleza",
                $"naturaleza IN ({valores})"));
    }
}

public sealed class CategoriaProductoConfiguration
    : IEntityTypeConfiguration<CategoriaProducto>
{
    public void Configure(EntityTypeBuilder<CategoriaProducto> builder)
    {
        builder.ToTable(
            "categorias_productos",
            "s_catalogos",
            table => table.HasCheckConstraint(
                "ck_categorias_productos_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.Uuid)
            .HasColumnName("uuid")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();
        builder.Property(x => x.CategoriaPadreId)
            .HasColumnName("categoria_padre_id");
        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(x => x.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500);
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => x.Uuid)
            .IsUnique()
            .HasDatabaseName("ux_categorias_productos_uuid");
        builder.HasIndex(x => new { x.EmpresaId, x.Codigo })
            .IsUnique()
            .HasDatabaseName("ux_categorias_productos_empresa_codigo");
        builder.HasOne(x => x.Empresa)
            .WithMany(x => x.CategoriasProducto)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CategoriaPadre)
            .WithMany(x => x.Subcategorias)
            .HasForeignKey(x => x.CategoriaPadreId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class MarcaConfiguration : IEntityTypeConfiguration<Marca>
{
    public void Configure(EntityTypeBuilder<Marca> builder)
    {
        builder.ToTable(
            "marcas",
            "s_catalogos",
            table => table.HasCheckConstraint(
                "ck_marcas_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.Uuid)
            .HasColumnName("uuid")
            .HasColumnType("uuid")
            .IsRequired();
        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();
        builder.Property(x => x.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500);
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => x.Uuid)
            .IsUnique()
            .HasDatabaseName("ux_marcas_uuid");
        builder.HasIndex(x => x.Codigo)
            .IsUnique()
            .HasDatabaseName("ux_marcas_codigo");
    }
}

public sealed class UnidadMedidaConfiguration
    : IEntityTypeConfiguration<UnidadMedida>
{
    public void Configure(EntityTypeBuilder<UnidadMedida> builder)
    {
        builder.ToTable(
            "unidades_medida",
            "s_catalogos",
            table => table.HasCheckConstraint(
                "ck_unidades_medida_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(32).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(100).IsRequired();
        builder.Property(x => x.Abreviatura).HasColumnName("abreviatura")
            .HasMaxLength(20).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => x.Codigo).IsUnique()
            .HasDatabaseName("ux_unidades_medida_codigo");
    }
}

public sealed class ImpuestoConfiguration : IEntityTypeConfiguration<Impuesto>
{
    public void Configure(EntityTypeBuilder<Impuesto> builder)
    {
        builder.ToTable(
            "impuestos",
            "s_catalogos",
            table => table.HasCheckConstraint(
                "ck_impuestos_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.CodigoSri).HasColumnName("codigo_sri")
            .HasMaxLength(16).IsRequired();
        builder.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(32).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(100).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => x.CodigoSri).IsUnique()
            .HasDatabaseName("ux_impuestos_codigo_sri");
        builder.HasIndex(x => x.Codigo).IsUnique()
            .HasDatabaseName("ux_impuestos_codigo");
    }
}

public sealed class TarifaImpuestoConfiguration
    : IEntityTypeConfiguration<TarifaImpuesto>
{
    public void Configure(EntityTypeBuilder<TarifaImpuesto> builder)
    {
        builder.ToTable(
            "tarifas_impuesto",
            "s_catalogos",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_tarifas_impuesto_estado", "estado IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_tarifas_impuesto_tipo_calculo",
                    "tipo_calculo IN ('NINGUNO', 'PORCENTAJE', 'ESPECIFICO', 'MIXTO')");
                table.HasCheckConstraint(
                    "ck_tarifas_impuesto_vigencia",
                    "vigente_hasta IS NULL OR vigente_hasta >= vigente_desde");
                table.HasCheckConstraint(
                    "ck_tarifas_impuesto_valores",
                    "(tipo_calculo = 'NINGUNO' AND porcentaje IS NULL AND valor_especifico IS NULL) OR " +
                    "(tipo_calculo = 'PORCENTAJE' AND porcentaje IS NOT NULL AND valor_especifico IS NULL) OR " +
                    "(tipo_calculo = 'ESPECIFICO' AND porcentaje IS NULL AND valor_especifico IS NOT NULL) OR " +
                    "(tipo_calculo = 'MIXTO' AND porcentaje IS NOT NULL AND valor_especifico IS NOT NULL)");
                table.HasCheckConstraint(
                    "ck_tarifas_impuesto_porcentaje",
                    "porcentaje IS NULL OR porcentaje BETWEEN 0 AND 100");
                table.HasCheckConstraint(
                    "ck_tarifas_impuesto_valor_especifico",
                    "valor_especifico IS NULL OR valor_especifico >= 0");
            });
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.ImpuestoId).HasColumnName("impuesto_id")
            .IsRequired();
        builder.Property(x => x.CodigoSri).HasColumnName("codigo_sri")
            .HasMaxLength(16).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        builder.Property(x => x.TipoCalculo).HasColumnName("tipo_calculo")
            .HasMaxLength(16).IsRequired();
        builder.Property(x => x.Porcentaje).HasColumnName("porcentaje")
            .HasPrecision(18, 6);
        builder.Property(x => x.ValorEspecifico)
            .HasColumnName("valor_especifico").HasPrecision(18, 2);
        builder.Property(x => x.VigenteDesde).HasColumnName("vigente_desde")
            .HasColumnType("date").IsRequired();
        builder.Property(x => x.VigenteHasta).HasColumnName("vigente_hasta")
            .HasColumnType("date");
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => new { x.ImpuestoId, x.CodigoSri, x.VigenteDesde })
            .IsUnique()
            .HasDatabaseName("ux_tarifas_impuesto_impuesto_codigo_vigencia");
        builder.HasOne(x => x.Impuesto).WithMany(x => x.Tarifas)
            .HasForeignKey(x => x.ImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TipoIdentificacionConfiguration
    : IEntityTypeConfiguration<TipoIdentificacion>
{
    public void Configure(EntityTypeBuilder<TipoIdentificacion> builder)
    {
        builder.ToTable(
            "tipos_identificacion",
            "s_catalogos",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_tipos_identificacion_estado", "estado IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_tipos_identificacion_longitudes",
                    "longitud_minima > 0 AND longitud_maxima >= longitud_minima");
            });
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.CodigoSri).HasColumnName("codigo_sri")
            .HasMaxLength(2).IsRequired();
        builder.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(32).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(100).IsRequired();
        builder.Property(x => x.LongitudMinima).HasColumnName("longitud_minima")
            .IsRequired();
        builder.Property(x => x.LongitudMaxima).HasColumnName("longitud_maxima")
            .IsRequired();
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => x.CodigoSri).IsUnique()
            .HasDatabaseName("ux_tipos_identificacion_codigo_sri");
        builder.HasIndex(x => x.Codigo).IsUnique()
            .HasDatabaseName("ux_tipos_identificacion_codigo");
    }
}

public sealed class RegimenTributarioConfiguration
    : IEntityTypeConfiguration<RegimenTributario>
{
    public void Configure(EntityTypeBuilder<RegimenTributario> builder)
    {
        builder.ToTable(
            "regimenes_tributarios",
            "s_catalogos",
            table => table.HasCheckConstraint(
                "ck_regimenes_tributarios_estado",
                "estado IN (0, 1)"));
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.Codigo).HasColumnName("codigo")
            .HasMaxLength(64).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => x.Codigo).IsUnique()
            .HasDatabaseName("ux_regimenes_tributarios_codigo");
    }
}

public sealed class TipoComprobanteConfiguration
    : IEntityTypeConfiguration<TipoComprobante>
{
    public void Configure(EntityTypeBuilder<TipoComprobante> builder) =>
        CatalogoConfiguration.ConCodigoSri(builder, "tipos_comprobante");
}

public sealed class FormaPagoConfiguration
    : IEntityTypeConfiguration<FormaPago>
{
    public void Configure(EntityTypeBuilder<FormaPago> builder) =>
        CatalogoConfiguration.ConCodigoSri(builder, "formas_pago");
}

public sealed class MedioPagoConfiguration
    : IEntityTypeConfiguration<MedioPago>
{
    public void Configure(EntityTypeBuilder<MedioPago> builder)
    {
        CatalogoConfiguration.Basico(builder, "medios_pago");
        builder.Property(x => x.FormaPagoSriId)
            .HasColumnName("forma_pago_sri_id").IsRequired();
        builder.HasOne(x => x.FormaPagoSri).WithMany()
            .HasForeignKey(x => x.FormaPagoSriId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TipoAmbienteConfiguration
    : IEntityTypeConfiguration<TipoAmbiente>
{
    public void Configure(EntityTypeBuilder<TipoAmbiente> builder) =>
        CatalogoConfiguration.Numerico(builder, "tipos_ambiente");
}

public sealed class TipoEmisionConfiguration
    : IEntityTypeConfiguration<TipoEmision>
{
    public void Configure(EntityTypeBuilder<TipoEmision> builder) =>
        CatalogoConfiguration.Numerico(builder, "tipos_emision");
}

public sealed class EstadoSerieConfiguration
    : IEntityTypeConfiguration<EstadoSerie>
{
    public void Configure(EntityTypeBuilder<EstadoSerie> builder) =>
        CatalogoConfiguration.Basico(builder, "estados_serie");
}

public sealed class TipoDocumentoInternoConfiguration
    : IEntityTypeConfiguration<TipoDocumentoInterno>
{
    public void Configure(EntityTypeBuilder<TipoDocumentoInterno> builder)
    {
        CatalogoConfiguration.Basico(builder, "tipos_documento_interno");
        builder.Property(x => x.PrefijoDefault)
            .HasColumnName("prefijo_default")
            .HasMaxLength(8)
            .IsRequired();
    }
}

public sealed class TipoMovimientoInventarioConfiguration
    : IEntityTypeConfiguration<TipoMovimientoInventario>
{
    public void Configure(EntityTypeBuilder<TipoMovimientoInventario> builder) =>
        CatalogoConfiguration.ConNaturaleza(
            builder, "tipos_movimiento_inventario", "'ENTRADA', 'SALIDA'");
}

public sealed class TipoOrigenMovimientoInventarioConfiguration
    : IEntityTypeConfiguration<TipoOrigenMovimientoInventario>
{
    public void Configure(
        EntityTypeBuilder<TipoOrigenMovimientoInventario> builder) =>
        CatalogoConfiguration.Basico(
            builder, "tipos_origen_movimiento_inventario");
}

public sealed class TipoMovimientoCarteraConfiguration
    : IEntityTypeConfiguration<TipoMovimientoCartera>
{
    public void Configure(EntityTypeBuilder<TipoMovimientoCartera> builder) =>
        CatalogoConfiguration.ConNaturaleza(
            builder, "tipos_movimiento_cartera", "'DEBITO', 'CREDITO'");
}

public sealed class TipoMovimientoCuentaPorPagarConfiguration
    : IEntityTypeConfiguration<TipoMovimientoCuentaPorPagar>
{
    public void Configure(
        EntityTypeBuilder<TipoMovimientoCuentaPorPagar> builder) =>
        CatalogoConfiguration.ConNaturaleza(
            builder,
            "tipos_movimiento_cuentas_por_pagar",
            "'DEBITO', 'CREDITO'");
}

public sealed class TipoConfiguracionContableConfiguration
    : IEntityTypeConfiguration<TipoConfiguracionContable>
{
    public void Configure(
        EntityTypeBuilder<TipoConfiguracionContable> builder) =>
        CatalogoConfiguration.Basico(
            builder, "tipos_configuracion_contable");
}

public sealed class TipoOrigenAsientoConfiguration
    : IEntityTypeConfiguration<TipoOrigenAsiento>
{
    public void Configure(EntityTypeBuilder<TipoOrigenAsiento> builder) =>
        CatalogoConfiguration.Basico(builder, "tipos_origen_asiento");
}

public sealed class TipoMovimientoCajaConfiguration
    : IEntityTypeConfiguration<TipoMovimientoCaja>
{
    public void Configure(EntityTypeBuilder<TipoMovimientoCaja> builder) =>
        CatalogoConfiguration.ConNaturaleza(
            builder, "tipos_movimiento_caja", "'ENTRADA', 'SALIDA'");
}

public sealed class TipoMovimientoBancarioConfiguration
    : IEntityTypeConfiguration<TipoMovimientoBancario>
{
    public void Configure(EntityTypeBuilder<TipoMovimientoBancario> builder) =>
        CatalogoConfiguration.ConNaturaleza(
            builder, "tipos_movimiento_bancario", "'ENTRADA', 'SALIDA'");
}

public sealed class TipoOrigenGuiaRemisionConfiguration
    : IEntityTypeConfiguration<TipoOrigenGuiaRemision>
{
    public void Configure(EntityTypeBuilder<TipoOrigenGuiaRemision> builder) =>
        CatalogoConfiguration.Basico(builder, "tipos_origen_guia_remision");
}

public sealed class TipoOrigenDevolucionVentaConfiguration
    : IEntityTypeConfiguration<TipoOrigenDevolucionVenta>
{
    public void Configure(
        EntityTypeBuilder<TipoOrigenDevolucionVenta> builder) =>
        CatalogoConfiguration.Basico(
            builder, "tipos_origen_devolucion_venta");
}

public sealed class TipoOrigenDevolucionCompraConfiguration
    : IEntityTypeConfiguration<TipoOrigenDevolucionCompra>
{
    public void Configure(
        EntityTypeBuilder<TipoOrigenDevolucionCompra> builder) =>
        CatalogoConfiguration.Basico(
            builder, "tipos_origen_devolucion_compra");
}

public sealed class TipoOrigenRetencionEmitidaConfiguration
    : IEntityTypeConfiguration<TipoOrigenRetencionEmitida>
{
    public void Configure(
        EntityTypeBuilder<TipoOrigenRetencionEmitida> builder) =>
        CatalogoConfiguration.Basico(
            builder, "tipos_origen_retencion_emitida");
}

public sealed class TipoOrigenComprobanteElectronicoConfiguration
    : IEntityTypeConfiguration<TipoOrigenComprobanteElectronico>
{
    public void Configure(
        EntityTypeBuilder<TipoOrigenComprobanteElectronico> builder) =>
        CatalogoConfiguration.Basico(
            builder, "tipos_origen_comprobante_electronico");
}

public sealed class EstadoComprobanteElectronicoConfiguration
    : IEntityTypeConfiguration<EstadoComprobanteElectronico>
{
    public void Configure(
        EntityTypeBuilder<EstadoComprobanteElectronico> builder) =>
        CatalogoConfiguration.Basico(
            builder, "estados_comprobante_electronico");
}

public sealed class ConceptoRetencionConfiguration
    : IEntityTypeConfiguration<ConceptoRetencion>
{
    public void Configure(EntityTypeBuilder<ConceptoRetencion> builder)
    {
        builder.ToTable(
            "conceptos_retencion",
            "s_catalogos",
            table =>
            {
                table.HasCheckConstraint(
                    "ck_conceptos_retencion_estado", "estado IN (0, 1)");
                table.HasCheckConstraint(
                    "ck_conceptos_retencion_porcentaje",
                    "porcentaje BETWEEN 0 AND 100");
                table.HasCheckConstraint(
                    "ck_conceptos_retencion_vigencia",
                    "vigente_hasta IS NULL OR vigente_hasta >= vigente_desde");
            });
        builder.HasKey(x => x.Id);
        CatalogoConfiguration.Id(builder);
        builder.Property(x => x.ImpuestoId).HasColumnName("impuesto_id")
            .IsRequired();
        builder.Property(x => x.CodigoSri).HasColumnName("codigo_sri")
            .HasMaxLength(16).IsRequired();
        builder.Property(x => x.Nombre).HasColumnName("nombre")
            .HasMaxLength(150).IsRequired();
        builder.Property(x => x.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(500);
        builder.Property(x => x.Porcentaje).HasColumnName("porcentaje")
            .HasPrecision(18, 6).IsRequired();
        builder.Property(x => x.VigenteDesde).HasColumnName("vigente_desde")
            .HasColumnType("date").IsRequired();
        builder.Property(x => x.VigenteHasta).HasColumnName("vigente_hasta")
            .HasColumnType("date");
        CatalogoConfiguration.EstadoYTimestamps(builder);
        builder.HasIndex(x => new { x.ImpuestoId, x.CodigoSri, x.VigenteDesde })
            .IsUnique()
            .HasDatabaseName("ux_conceptos_retencion_impuesto_codigo_vigencia");
        builder.HasOne(x => x.Impuesto).WithMany(x => x.ConceptosRetencion)
            .HasForeignKey(x => x.ImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
