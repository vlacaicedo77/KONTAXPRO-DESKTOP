using KONTAXPRO.Domain.Entities.FacturacionElectronica;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class ComprobanteElectronicoConfiguration
    : IEntityTypeConfiguration<ComprobanteElectronico>
{
    public void Configure(EntityTypeBuilder<ComprobanteElectronico> b)
    {
        b.ToTable("comprobantes_electronicos", "s_facturacion_electronica");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.TipoComprobanteId)
            .HasColumnName("tipo_comprobante_id").IsRequired();
        b.Property(x => x.TipoOrigenComprobanteElectronicoId)
            .HasColumnName("tipo_origen_comprobante_electronico_id").IsRequired();
        b.Property(x => x.OrigenId).HasColumnName("origen_id").IsRequired();
        b.Property(x => x.TipoAmbienteId)
            .HasColumnName("tipo_ambiente_id").IsRequired();
        b.Property(x => x.TipoEmisionId)
            .HasColumnName("tipo_emision_id").IsRequired();
        b.Property(x => x.ClaveAcceso).HasColumnName("clave_acceso")
            .HasMaxLength(49).IsRequired();
        b.Property(x => x.EstadoComprobanteElectronicoId)
            .HasColumnName("estado_comprobante_electronico_id").IsRequired();
        Timestamp(b, x => x.ProcesamientoIniciadoAt,
            "procesamiento_iniciado_at");
        b.Property(x => x.ProcesadoPorInstalacionUuid)
            .HasColumnName("procesado_por_instalacion_uuid")
            .HasColumnType("uuid");
        Timestamp(b, x => x.XmlGeneradoAt, "xml_generado_at");
        Timestamp(b, x => x.XmlFirmadoAt, "xml_firmado_at");
        Timestamp(b, x => x.FechaEnvio, "fecha_envio");
        Timestamp(b, x => x.FechaAutorizacion, "fecha_autorizacion");
        b.Property(x => x.NumeroAutorizacion)
            .HasColumnName("numero_autorizacion").HasMaxLength(64);
        Timestamp(b, x => x.XmlAutorizadoAt, "xml_autorizado_at");
        Timestamp(b, x => x.RideGeneradoAt, "ride_generado_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");
        b.HasIndex(x => x.ClaveAcceso).IsUnique()
            .HasDatabaseName("ux_comprobantes_electronicos_clave_acceso");
        b.HasIndex(x => new
            { x.TipoOrigenComprobanteElectronicoId, x.OrigenId })
            .IsUnique().HasDatabaseName(
                "ux_comprobantes_electronicos_origen");
        b.HasIndex(x => new
            { x.EstadoComprobanteElectronicoId, x.ProcesamientoIniciadoAt })
            .HasDatabaseName("ix_comprobantes_electronicos_procesamiento");
        b.HasOne(x => x.Empresa).WithMany()
            .HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoComprobante).WithMany()
            .HasForeignKey(x => x.TipoComprobanteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoOrigenComprobanteElectronico).WithMany()
            .HasForeignKey(x => x.TipoOrigenComprobanteElectronicoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoAmbiente).WithMany()
            .HasForeignKey(x => x.TipoAmbienteId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TipoEmision).WithMany()
            .HasForeignKey(x => x.TipoEmisionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EstadoComprobanteElectronico).WithMany()
            .HasForeignKey(x => x.EstadoComprobanteElectronicoId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    private static void Timestamp(
        EntityTypeBuilder<ComprobanteElectronico> b,
        System.Linq.Expressions.Expression<
            Func<ComprobanteElectronico, DateTime?>> property,
        string column) =>
        b.Property(property).HasColumnName(column)
            .HasColumnType("timestamp with time zone");
}

public sealed class ComprobanteElectronicoEventoConfiguration
    : IEntityTypeConfiguration<ComprobanteElectronicoEvento>
{
    public void Configure(EntityTypeBuilder<ComprobanteElectronicoEvento> b)
    {
        b.ToTable("comprobantes_electronicos_eventos",
            "s_facturacion_electronica", t =>
            t.HasCheckConstraint("ck_comprobantes_electronicos_eventos_tipo",
                "tipo_evento IN ('GENERACION', 'FIRMA', 'ENVIO', " +
                "'RECEPCION', 'AUTORIZACION', 'REINTENTO', 'ERROR')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.ComprobanteElectronicoId)
            .HasColumnName("comprobante_electronico_id").IsRequired();
        b.Property(x => x.TipoEvento).HasColumnName("tipo_evento")
            .HasMaxLength(16).IsRequired();
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(64);
        b.Property(x => x.Mensaje).HasColumnName("mensaje")
            .HasColumnType("text");
        b.Property(x => x.InformacionAdicional)
            .HasColumnName("informacion_adicional").HasColumnType("text");
        b.Property(x => x.CreatedAt).HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        b.HasOne(x => x.ComprobanteElectronico).WithMany(x => x.Eventos)
            .HasForeignKey(x => x.ComprobanteElectronicoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
