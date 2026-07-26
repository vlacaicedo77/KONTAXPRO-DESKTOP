using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class BodegaConfiguration
    : IEntityTypeConfiguration<Bodega>
{
    public void Configure(EntityTypeBuilder<Bodega> builder)
    {
        builder.ToTable("bodegas", "s_inventario");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_bodegas_id_empresa");

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(500);

        builder.Property(x => x.EsPrincipal)
            .HasColumnName("es_principal")
            .IsRequired();

        builder.Property(x => x.PermiteVentas)
            .HasColumnName("permite_ventas")
            .IsRequired();

        builder.Property(x => x.PermiteCompras)
            .HasColumnName("permite_compras")
            .IsRequired();

        builder.Property(x => x.Estado)
            .HasColumnName("estado")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(x => new
        {
            x.EmpresaId,
            x.Codigo
        })
        .IsUnique();

        builder.HasIndex(x => new
        {
            x.EmpresaId,
            x.Nombre
        })
        .IsUnique();

        builder.HasOne(x => x.Empresa)
            .WithMany(x => x.Bodegas)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
