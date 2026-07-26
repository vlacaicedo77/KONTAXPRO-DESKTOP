using KONTAXPRO.Domain.Entities.Ventas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class GuiaRemisionConfiguration
    : IEntityTypeConfiguration<GuiaRemision>
{
    public void Configure(EntityTypeBuilder<GuiaRemision> b)
    {
        VentasEf.Base(b, "guias_remision");
        b.ToTable("guias_remision", "s_ventas", t =>
            t.HasCheckConstraint("ck_guias_remision_fechas",
                "fecha_fin_traslado >= fecha_inicio_traslado"));
        b.Property(x => x.NumeroDocumento).HasMaxLength(32).IsRequired();
        b.Property(x => x.MotivoTraslado).HasMaxLength(500).IsRequired();
        b.Property(x => x.DireccionPartida).HasMaxLength(500).IsRequired();
        b.Property(x => x.DireccionDestino).HasMaxLength(500).IsRequired();
        b.Property(x => x.TransportistaTipoIdentificacion)
            .HasMaxLength(16).IsRequired();
        b.Property(x => x.TransportistaNumeroIdentificacion)
            .HasMaxLength(20).IsRequired();
        b.Property(x => x.TransportistaRazonSocial)
            .HasMaxLength(256).IsRequired();
        b.Property(x => x.Placa).HasMaxLength(16);
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.MotivoAnulacion).HasMaxLength(500);
        b.Property(x => x.Observacion).HasMaxLength(1000);
        b.Property(x => x.FechaEmision).HasColumnType("timestamp with time zone");
        b.Property(x => x.FechaInicioTraslado).HasColumnType("date");
        b.Property(x => x.FechaFinTraslado).HasColumnType("date");
        b.HasIndex(x => new
            { x.PuntoEmisionId, x.TipoComprobanteId, x.Secuencial })
            .IsUnique().HasDatabaseName("ux_guias_remision_emision_secuencial");
        b.HasIndex(x => new { x.EmpresaId, x.NumeroDocumento }).IsUnique()
            .HasDatabaseName("ux_guias_remision_empresa_numero");
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
        b.HasOne(x => x.TipoOrigenGuiaRemision).WithMany()
            .HasForeignKey(x => x.TipoOrigenGuiaRemisionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AnuladoPorUsuario).WithMany()
            .HasForeignKey(x => x.AnuladoPorUsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class GuiaRemisionDetalleConfiguration
    : IEntityTypeConfiguration<GuiaRemisionDetalle>
{
    public void Configure(EntityTypeBuilder<GuiaRemisionDetalle> b)
    {
        VentasEf.Base(b, "guias_remision_detalles");
        b.ToTable("guias_remision_detalles", "s_ventas", t =>
            t.HasCheckConstraint("ck_guias_remision_detalles_cantidad",
                "cantidad_presentacion > 0 AND factor_conversion > 0 " +
                "AND cantidad_base > 0"));
        VentasEf.Quantity(b, "CantidadPresentacion", "FactorConversion",
            "CantidadBase");
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        b.HasOne(x => x.GuiaRemision).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.GuiaRemisionId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ProductoPresentacion).WithMany()
            .HasForeignKey(x => x.ProductoPresentacionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
