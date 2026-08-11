using System.Text;
using KONTAXPRO.Domain.Entities.Compras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class ComprasEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        bool updated = true) where TEntity : class
    {
        b.ToTable(table, "s_compras");
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

    internal static void Quantity<TEntity>(
        EntityTypeBuilder<TEntity> b,
        params string[] names) where TEntity : class
    {
        foreach (var name in names)
            b.Property<decimal>(name).HasPrecision(18, 6).IsRequired();
    }

    internal static void Money<TEntity>(
        EntityTypeBuilder<TEntity> b,
        params string[] names) where TEntity : class
    {
        foreach (var name in names)
            b.Property<decimal>(name).HasPrecision(18, 2).IsRequired();
    }

    internal static void Tax<TEntity>(EntityTypeBuilder<TEntity> b)
        where TEntity : class
    {
        b.Property<string>("CodigoImpuestoSri").HasMaxLength(16).IsRequired();
        b.Property<string>("CodigoPorcentajeSri").HasMaxLength(16).IsRequired();
        b.Property<string>("NombreImpuesto").HasMaxLength(150).IsRequired();
        b.Property<string>("TipoCalculo").HasMaxLength(16).IsRequired();
        b.Property<decimal?>("Porcentaje").HasPrecision(18, 6);
        b.Property<decimal?>("ValorEspecifico").HasPrecision(18, 6);
        b.Property<decimal>("BaseImponible").HasPrecision(18, 2).IsRequired();
        b.Property<decimal>("ValorImpuesto").HasPrecision(18, 2).IsRequired();
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

public sealed class DocumentoRecibidoSriConfiguration
    : IEntityTypeConfiguration<DocumentoRecibidoSri>
{
    public void Configure(EntityTypeBuilder<DocumentoRecibidoSri> b)
    {
        ComprasEf.Base(b, "documentos_recibidos_sri");
        b.ToTable("documentos_recibidos_sri", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_documentos_recibidos_sri_clasificacion",
                "clasificacion IS NULL OR clasificacion IN " +
                "('INVENTARIO', 'GASTO', 'ACTIVO', 'OTRO')");
            t.HasCheckConstraint("ck_documentos_recibidos_sri_estado",
                "estado_procesamiento IN ('PENDIENTE', 'PROCESADO', 'NO_APLICA')");
            t.HasCheckConstraint("ck_documentos_recibidos_sri_validacion",
                "estado_validacion IN ('AUTORIZADO_SRI', 'VALIDADO_LOCALMENTE', 'ADVERTENCIA', 'RECHAZADO')");
            t.HasCheckConstraint("ck_documentos_recibidos_sri_archivo",
                "archivo_tamano > 0 AND length(archivo_sha256) = 64");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_documentos_recibidos_sri_id_empresa");
        b.Property(x => x.NumeroDocumento).HasMaxLength(64).IsRequired();
        b.Property(x => x.ClaveAcceso).HasMaxLength(49).IsRequired();
        b.Property(x => x.Ambiente).HasMaxLength(16).IsRequired();
        b.Property(x => x.TipoEmision).HasMaxLength(16).IsRequired();
        b.Property(x => x.RucEmisor).HasMaxLength(20).IsRequired();
        b.Property(x => x.RazonSocialEmisor).HasMaxLength(256).IsRequired();
        b.Property(x => x.NombreComercialEmisor).HasMaxLength(256);
        b.Property(x => x.DireccionMatriz).HasMaxLength(500);
        b.Property(x => x.DireccionEstablecimiento).HasMaxLength(500);
        b.Property(x => x.EstablecimientoCodigo).HasMaxLength(3).IsRequired();
        b.Property(x => x.PuntoEmisionCodigo).HasMaxLength(3).IsRequired();
        b.Property(x => x.Secuencial).HasMaxLength(9).IsRequired();
        b.Property(x => x.IdentificacionReceptor).HasMaxLength(20).IsRequired();
        b.Property(x => x.RazonSocialReceptor).HasMaxLength(256).IsRequired();
        b.Property(x => x.Moneda).HasMaxLength(16);
        b.Property(x => x.EstadoValidacion).HasMaxLength(24).IsRequired();
        b.Property(x => x.ArchivoRutaRelativa).HasMaxLength(500).IsRequired();
        b.Property(x => x.ArchivoSha256).HasMaxLength(64).IsRequired();
        b.Property(x => x.NumeroDocumentoModificado).HasMaxLength(64);
        b.Property(x => x.Clasificacion).HasMaxLength(16);
        b.Property(x => x.EstadoProcesamiento).HasMaxLength(16).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnType("date");
        b.Property(x => x.FechaAutorizacion)
            .HasColumnType("timestamp with time zone");
        b.Property(x => x.XmlObtenidoAt)
            .HasColumnType("timestamp with time zone");
        ComprasEf.Money(b, "ValorSinImpuestos", "Iva", "Propina",
            "ImporteTotal");
        b.HasIndex(x => x.ClaveAcceso).IsUnique()
            .HasDatabaseName("ux_documentos_recibidos_sri_clave_acceso");
        b.HasIndex(x => x.ArchivoSha256).IsUnique()
            .HasDatabaseName("ux_documentos_recibidos_sri_archivo_sha256");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Tercero).WithMany().HasForeignKey(x => x.TerceroId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraConfiguration : IEntityTypeConfiguration<Compra>
{
    public void Configure(EntityTypeBuilder<Compra> b)
    {
        ComprasEf.Base(b, "compras");
        b.ToTable("compras", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_compras_tipo",
                "tipo_compra = 'FACTURADA'");
            t.HasCheckConstraint("ck_compras_documento",
                "(tipo_compra = 'FACTURADA' AND tipo_comprobante_id IS NOT NULL " +
                "AND numero_documento IS NOT NULL)");
            t.HasCheckConstraint("ck_compras_estado",
                "estado IN ('BORRADOR', 'PENDIENTE_RECEPCION', " +
                "'PARCIALMENTE_RECIBIDA', 'RECIBIDA', 'ANULADA')");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_compras_id_empresa");
        b.Property(x => x.TipoCompra).HasMaxLength(16).IsRequired();
        b.Property(x => x.NumeroDocumento).HasMaxLength(64);
        b.Property(x => x.ProveedorIdentificacion).HasMaxLength(20).IsRequired();
        b.Property(x => x.ProveedorRazonSocial).HasMaxLength(256).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaEmision).HasColumnType("date");
        b.Property(x => x.FechaIngreso).HasColumnType("timestamp with time zone");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        b.Property(x => x.FechaVencimiento).HasColumnType("date");
        b.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        ComprasEf.Money(b, "SubtotalSinImpuestos", "DescuentoTotal",
            "Subtotal", "ImpuestoTotal", "Total");
        b.HasIndex(x => x.DocumentoRecibidoSriId).IsUnique()
            .HasFilter("documento_recibido_sri_id IS NOT NULL")
            .HasDatabaseName("ux_compras_documento_recibido_sri");
        b.HasIndex(x => x.CompraSustituidaId).IsUnique()
            .HasFilter("compra_sustituida_id IS NOT NULL")
            .HasDatabaseName("ux_compras_compra_sustituida");
        b.HasIndex(x => new
            {
                x.EmpresaId,
                x.EmpresaTerceroId,
                x.TipoComprobanteId,
                x.NumeroDocumento
            })
            .IsUnique()
            .HasFilter("numero_documento IS NOT NULL AND estado <> 'ANULADA'")
            .HasDatabaseName("ux_compras_empresa_proveedor_tipo_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentoRecibidoSri).WithMany()
            .HasForeignKey(x => new { x.DocumentoRecibidoSriId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CompraSustituida)
            .WithOne(x => x.CompraSustituta)
            .HasForeignKey<Compra>(x =>
                new { x.CompraSustituidaId, x.EmpresaId })
            .HasPrincipalKey<Compra>(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraDetalleConfiguration
    : IEntityTypeConfiguration<CompraDetalle>
{
    public void Configure(EntityTypeBuilder<CompraDetalle> b)
    {
        ComprasEf.Base(b, "compras_detalles");
        b.ToTable("compras_detalles", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_compras_detalles_cantidades",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0");
            t.HasCheckConstraint("ck_compras_detalles_producto",
                "(es_inventariable AND producto_id IS NOT NULL AND " +
                "producto_presentacion_id IS NOT NULL AND " +
                "estado_reconocimiento = 'RECONOCIDA') OR " +
                "(NOT es_inventariable AND producto_id IS NULL AND " +
                "producto_presentacion_id IS NULL AND " +
                "estado_reconocimiento = 'NO_INVENTARIABLE') OR " +
                "(estado_reconocimiento IN ('SUGERIDA', 'NO_RECONOCIDA'))");
            t.HasCheckConstraint("ck_compras_detalles_reconocimiento",
                "estado_reconocimiento IN ('RECONOCIDA', 'SUGERIDA', " +
                "'NO_RECONOCIDA', 'NO_INVENTARIABLE')");
            t.HasCheckConstraint("ck_compras_detalles_clasificacion_contable",
                "clasificacion_contable IN ('INVENTARIO', 'GASTO', " +
                "'ACTIVO', 'OTRO')");
            t.HasCheckConstraint("ck_compras_detalles_clasificacion_producto",
                "(es_inventariable AND clasificacion_contable = 'INVENTARIO') " +
                "OR (NOT es_inventariable AND " +
                "clasificacion_contable <> 'INVENTARIO')");
            t.HasCheckConstraint("ck_compras_detalles_orden",
                "orden > 0");
        });
        b.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_compras_detalles_id_empresa");
        b.HasAlternateKey(x => new { x.Id, x.CompraId, x.EmpresaId })
            .HasName("ak_compras_detalles_id_compra_empresa");
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.Property(x => x.CodigoPrincipalProveedor).HasMaxLength(100);
        b.Property(x => x.CodigoAuxiliarProveedor).HasMaxLength(100);
        b.Property(x => x.EstadoReconocimiento).HasMaxLength(24).IsRequired();
        b.Property(x => x.ClasificacionContable).HasMaxLength(16).IsRequired();
        ComprasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase", "PrecioUnitarioCompra", "DescuentoPorcentaje",
            "CostoUnitarioBase");
        ComprasEf.Money(b, "DescuentoValor", "PrecioTotalSinImpuesto",
            "CostoTotalLinea");
        b.HasIndex(x => new { x.CompraId, x.Orden }).IsUnique()
            .HasDatabaseName("ux_compras_detalles_compra_orden");
        b.HasOne(x => x.Compra).WithMany(x => x.Detalles)
            .HasForeignKey(x => new { x.CompraId, x.EmpresaId })
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
            .HasForeignKey(x => new
            {
                x.ProductoPresentacionId,
                x.ProductoId,
                x.EmpresaId
            })
            .HasPrincipalKey(x => new { x.Id, x.ProductoId, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DocumentoRecibidoSriPagoConfiguration
    : IEntityTypeConfiguration<DocumentoRecibidoSriPago>
{
    public void Configure(EntityTypeBuilder<DocumentoRecibidoSriPago> b)
    {
        ComprasEf.Base(b, "documentos_recibidos_sri_pagos", false);
        b.ToTable("documentos_recibidos_sri_pagos", "s_compras", t =>
        {
            t.HasCheckConstraint("ck_documentos_sri_pagos_valor", "valor > 0");
            t.HasCheckConstraint("ck_documentos_sri_pagos_plazo",
                "plazo IS NULL OR plazo >= 0");
        });
        b.Property(x => x.CodigoFormaPagoSri).HasMaxLength(8).IsRequired();
        b.Property(x => x.UnidadTiempo).HasMaxLength(32);
        ComprasEf.Money(b, "Valor");
        b.HasOne(x => x.DocumentoRecibidoSri)
            .WithMany(x => x.PagosDeclarados)
            .HasForeignKey(x => x.DocumentoRecibidoSriId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CompraDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<CompraDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<CompraDetalleImpuesto> b)
    {
        ComprasEf.Base(b, "compras_detalles_impuestos", false);
        ComprasEf.Tax(b);
        b.HasOne(x => x.CompraDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.CompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto).WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
