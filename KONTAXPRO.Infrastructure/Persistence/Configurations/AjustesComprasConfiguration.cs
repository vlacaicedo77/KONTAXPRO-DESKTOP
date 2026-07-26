using KONTAXPRO.Domain.Entities.Compras;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class AjusteCompraConfiguration
    : IEntityTypeConfiguration<AjusteCompra>
{
    public void Configure(EntityTypeBuilder<AjusteCompra> b)
    {
        ComprasEf.Base(b, "ajustes_compras");
        b.ToTable("ajustes_compras", "s_compras", t =>
            t.HasCheckConstraint("ck_ajustes_compras_tipo",
                "tipo_ajuste IN ('DEVOLUCION_MERCADERIA', " +
                "'DESCUENTO_POSTERIOR', 'CORRECCION_PRECIO', 'OTRO')"));
        b.Property(x => x.TipoAjuste).HasMaxLength(32).IsRequired();
        b.Property(x => x.Estado).HasMaxLength(24).IsRequired();
        b.Property(x => x.Observacion).HasMaxLength(1000);
        ComprasEf.Money(b, "ValorSinImpuestos", "ImpuestoTotal", "ValorTotal");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EmpresaTercero).WithMany()
            .HasForeignKey(x => new { x.EmpresaTerceroId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Compra).WithMany()
            .HasForeignKey(x => new { x.CompraId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentoRecibidoSri).WithMany()
            .HasForeignKey(x => new { x.DocumentoRecibidoSriId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DevolucionCompra).WithMany()
            .HasForeignKey(x => new { x.DevolucionCompraId, x.EmpresaId })
            .HasPrincipalKey(x => new { x.Id, x.EmpresaId })
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AjusteCompraDetalleConfiguration
    : IEntityTypeConfiguration<AjusteCompraDetalle>
{
    public void Configure(EntityTypeBuilder<AjusteCompraDetalle> b)
    {
        ComprasEf.Base(b, "ajustes_compras_detalles");
        b.Property(x => x.Descripcion).HasMaxLength(500).IsRequired();
        ComprasEf.Money(b, "ValorSinImpuestos", "ImpuestoTotal", "ValorTotal");
        b.HasOne(x => x.AjusteCompra).WithMany(x => x.Detalles)
            .HasForeignKey(x => x.AjusteCompraId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AjusteCompraDetalleImpuestoConfiguration
    : IEntityTypeConfiguration<AjusteCompraDetalleImpuesto>
{
    public void Configure(EntityTypeBuilder<AjusteCompraDetalleImpuesto> b)
    {
        ComprasEf.Base(b, "ajustes_compras_detalles_impuestos", false);
        ComprasEf.Tax(b);
        b.HasOne(x => x.AjusteCompraDetalle).WithMany(x => x.Impuestos)
            .HasForeignKey(x => x.AjusteCompraDetalleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TarifaImpuesto).WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
