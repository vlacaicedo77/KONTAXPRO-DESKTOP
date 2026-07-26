using System.Text;
using KONTAXPRO.Domain.Entities.Tributacion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

internal static class TributacionEf
{
    internal static void Base<TEntity>(
        EntityTypeBuilder<TEntity> b,
        string table,
        bool updated = true) where TEntity : class
    {
        b.ToTable(table, "s_tributacion");
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

    internal static void RetentionDetail<TEntity>(
        EntityTypeBuilder<TEntity> b) where TEntity : class
    {
        b.Property<string>("CodigoImpuestoSri").HasMaxLength(16).IsRequired();
        b.Property<string>("CodigoRetencionSri").HasMaxLength(16).IsRequired();
        b.Property<decimal>("BaseImponible").HasPrecision(18, 2).IsRequired();
        b.Property<decimal>("PorcentajeRetencion")
            .HasPrecision(18, 6).IsRequired();
        b.Property<decimal>("ValorRetenido").HasPrecision(18, 2).IsRequired();
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

public sealed class RetencionEmitidaConfiguration
    : IEntityTypeConfiguration<RetencionEmitida>
{
    public void Configure(EntityTypeBuilder<RetencionEmitida> b)
    {
        TributacionEf.Base(b, "retenciones_emitidas");
        b.ToTable("retenciones_emitidas", "s_tributacion", t =>
        {
            t.HasCheckConstraint("ck_retenciones_emitidas_total",
                "total_retenido > 0");
            t.HasCheckConstraint("ck_retenciones_emitidas_periodo",
                "periodo_fiscal ~ '^(0[1-9]|1[0-2])/[0-9]{4}$'");
        });
        b.Property(x => x.NumeroDocumento).HasMaxLength(32).IsRequired();
        b.Property(x => x.PeriodoFiscal).HasMaxLength(7).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.FechaEmision).HasColumnType("date");
        b.Property(x => x.AnuladaAt).HasColumnType("timestamp with time zone");
        TributacionEf.Money(b, "TotalRetenido");
        b.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.Secuencial })
            .IsUnique().HasDatabaseName(
                "ux_retenciones_emitidas_emision_secuencial");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento }).IsUnique()
            .HasDatabaseName("ux_retenciones_emitidas_empresa_numero");
        b.HasIndex(x => new { x.EmpresaId, x.TipoOrigenRetencionEmitidaId,
            x.OrigenId }).HasDatabaseName("ix_retenciones_emitidas_origen");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Establecimiento).WithMany()
            .HasForeignKey(x => new { x.EstablecimientoId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PuntoEmision).WithMany()
            .HasForeignKey(x => new { x.PuntoEmisionId, x.EstablecimientoId })
            .HasPrincipalKey(x => new { x.Id, x.EstablecimientoId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoOrigenRetencionEmitida).WithMany()
            .HasForeignKey(x => x.TipoOrigenRetencionEmitidaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RetencionEmitidaDetalleConfiguration
    : IEntityTypeConfiguration<RetencionEmitidaDetalle>
{
    public void Configure(EntityTypeBuilder<RetencionEmitidaDetalle> b)
    {
        TributacionEf.Base(b, "retenciones_emitidas_detalles", false);
        b.ToTable("retenciones_emitidas_detalles", "s_tributacion", t =>
        {
            t.HasCheckConstraint("ck_retenciones_emitidas_detalles_base",
                "base_imponible > 0");
            t.HasCheckConstraint("ck_retenciones_emitidas_detalles_valores",
                "porcentaje_retencion >= 0 AND valor_retenido > 0");
        });
        TributacionEf.RetentionDetail(b);
        b.HasOne(x => x.RetencionEmitida).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.RetencionEmitidaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConceptoRetencion).WithMany()
            .HasForeignKey(x => x.ConceptoRetencionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RetencionRecibidaConfiguration
    : IEntityTypeConfiguration<RetencionRecibida>
{
    public void Configure(EntityTypeBuilder<RetencionRecibida> b)
    {
        TributacionEf.Base(b, "retenciones_recibidas");
        b.ToTable("retenciones_recibidas", "s_tributacion", t =>
        {
            t.HasCheckConstraint("ck_retenciones_recibidas_total",
                "total_retenido > 0");
            t.HasCheckConstraint("ck_retenciones_recibidas_periodo",
                "periodo_fiscal ~ '^(0[1-9]|1[0-2])/[0-9]{4}$'");
        });
        b.Property(x => x.NumeroDocumento).HasMaxLength(64).IsRequired();
        b.Property(x => x.ClaveAcceso).HasMaxLength(49);
        b.Property(x => x.PeriodoFiscal).HasMaxLength(7).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.FechaEmision).HasColumnType("date");
        TributacionEf.Money(b, "TotalRetenido");
        b.HasIndex(x => x.DocumentoRecibidoSriId).IsUnique()
            .HasFilter("documento_recibido_sri_id IS NOT NULL")
            .HasDatabaseName("ux_retenciones_recibidas_documento_sri");
        b.HasIndex(x => x.ClaveAcceso).IsUnique()
            .HasFilter("clave_acceso IS NOT NULL")
            .HasDatabaseName("ux_retenciones_recibidas_clave_acceso");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento })
            .HasDatabaseName("ix_retenciones_recibidas_empresa_numero");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentoRecibidoSri).WithMany()
            .HasForeignKey(x => new { x.DocumentoRecibidoSriId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RetencionRecibidaDocumentoConfiguration
    : IEntityTypeConfiguration<RetencionRecibidaDocumento>
{
    public void Configure(EntityTypeBuilder<RetencionRecibidaDocumento> b)
    {
        TributacionEf.Base(b, "retenciones_recibidas_documentos", false);
        b.ToTable("retenciones_recibidas_documentos", "s_tributacion", t =>
            t.HasCheckConstraint("ck_retenciones_recibidas_documentos_valor",
                "valor_retenido_aplicado > 0"));
        TributacionEf.Money(b, "ValorRetenidoAplicado");
        b.HasIndex(x => new { x.RetencionRecibidaId, x.FacturaId })
            .IsUnique()
            .HasDatabaseName("ux_retenciones_recibidas_documentos_par");
        b.HasOne(x => x.RetencionRecibida).WithMany(x => x.Documentos)
            .HasForeignKey(x => x.RetencionRecibidaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Factura).WithMany()
            .HasForeignKey(x => x.FacturaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class RetencionRecibidaDetalleConfiguration
    : IEntityTypeConfiguration<RetencionRecibidaDetalle>
{
    public void Configure(EntityTypeBuilder<RetencionRecibidaDetalle> b)
    {
        TributacionEf.Base(b, "retenciones_recibidas_detalles", false);
        b.ToTable("retenciones_recibidas_detalles", "s_tributacion", t =>
        {
            t.HasCheckConstraint("ck_retenciones_recibidas_detalles_base",
                "base_imponible > 0");
            t.HasCheckConstraint("ck_retenciones_recibidas_detalles_valores",
                "porcentaje_retencion >= 0 AND valor_retenido > 0");
        });
        TributacionEf.RetentionDetail(b);
        b.HasOne(x => x.RetencionRecibida).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.RetencionRecibidaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConceptoRetencion).WithMany()
            .HasForeignKey(x => x.ConceptoRetencionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
