using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ListaPrecioConfiguration
    : IEntityTypeConfiguration<ListaPrecio>
{
    public void Configure(
        EntityTypeBuilder<ListaPrecio> builder)
    {
        builder.ToTable(
            "listas_precio",
            "s_inventario");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.Id, x.EmpresaId })
            .HasName("ak_listas_precio_id_empresa");

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
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(300);

        builder.Property(x => x.EsPredeterminada)
            .HasColumnName("es_predeterminada")
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

        builder.HasOne(x => x.Empresa)
            .WithMany(x => x.ListasPrecio)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
