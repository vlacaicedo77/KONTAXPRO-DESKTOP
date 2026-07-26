using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ProductoConfiguration
    : IEntityTypeConfiguration<Producto>
{
    public void Configure(EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("productos", "s_inventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(x => x.CategoriaProductoId)
            .HasColumnName("categoria_producto_id");

        builder.Property(x => x.MarcaId)
            .HasColumnName("marca_id");

        builder.Property(x => x.UnidadMedidaBaseId)
            .HasColumnName("unidad_medida_base_id")
            .IsRequired();

        builder.Property(x => x.TarifaImpuestoId)
            .HasColumnName("tarifa_impuesto_id")
            .IsRequired();

        builder.Property(x => x.Codigo)
            .HasColumnName("codigo")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Nombre)
            .HasColumnName("nombre")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Modelo)
            .HasColumnName("modelo")
            .HasMaxLength(150);

        builder.Property(x => x.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(1000);

        builder.Property(x => x.TipoProducto)
            .HasColumnName("tipo_producto")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.TipoControlInventario)
            .HasColumnName("tipo_control_inventario")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ManejaInventario)
            .HasColumnName("maneja_inventario")
            .IsRequired();

        builder.Property(x => x.PermiteVentaSinStock)
            .HasColumnName("permite_venta_sin_stock")
            .IsRequired();

        builder.Property(x => x.ManejaLotes)
            .HasColumnName("maneja_lotes")
            .IsRequired();

        builder.Property(x => x.ManejaSeries)
            .HasColumnName("maneja_series")
            .IsRequired();

        builder.Property(x => x.ManejaFechaCaducidad)
            .HasColumnName("maneja_fecha_caducidad")
            .IsRequired();

        builder.Property(x => x.AlertaStockMinimo)
            .HasColumnName("alerta_stock_minimo")
            .IsRequired();

        builder.Property(x => x.AlertaCaducidad)
            .HasColumnName("alerta_caducidad")
            .IsRequired();

        builder.Property(x => x.StockMinimo)
            .HasColumnName("stock_minimo")
            .HasPrecision(18, 6)
            .IsRequired();

        builder.Property(x => x.DiasAlertaCaducidad)
            .HasColumnName("dias_alerta_caducidad")
            .IsRequired();

        builder.Property(x => x.Observacion)
            .HasColumnName("observacion")
            .HasMaxLength(1000);

        builder.Property(x => x.Estado)
            .HasColumnName("estado")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => new
        {
            x.EmpresaId,
            x.Codigo
        })
        .IsUnique();

        builder.HasOne(x => x.Empresa)
            .WithMany(x => x.Productos)
            .HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CategoriaProducto)
            .WithMany()
            .HasForeignKey(x => x.CategoriaProductoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Marca)
            .WithMany()
            .HasForeignKey(x => x.MarcaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.UnidadMedidaBase)
            .WithMany()
            .HasForeignKey(x => x.UnidadMedidaBaseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.TarifaImpuesto)
            .WithMany()
            .HasForeignKey(x => x.TarifaImpuestoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}