using KONTAXPRO.Domain.Entities.Configuracion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public class ConfiguracionInventarioConfiguration
    : IEntityTypeConfiguration<ConfiguracionInventario>
{
    public void Configure(
        EntityTypeBuilder<ConfiguracionInventario> builder)
    {
        builder.ToTable(
            "configuraciones_inventario",
            "s_configuracion");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.EmpresaId)
            .HasColumnName("empresa_id")
            .IsRequired();

        builder.Property(x => x.MetodoCosteo)
            .HasColumnName("metodo_costeo")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.BaseCalculoPrecio)
            .HasColumnName("base_calculo_precio")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.PoliticaActualizacionPrecio)
            .HasColumnName("politica_actualizacion_precio")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.IvaCompraFormaParteCosto)
            .HasColumnName("iva_compra_forma_parte_costo")
            .IsRequired();

        builder.Property(x => x.PreciosVentaIncluyenImpuesto)
            .HasColumnName("precios_venta_incluyen_impuesto")
            .IsRequired();

        builder.Property(x => x.PermitirVentaSinStock)
            .HasColumnName("permitir_venta_sin_stock")
            .IsRequired();

        builder.Property(x => x.UsarControlLotes)
            .HasColumnName("usar_control_lotes")
            .IsRequired();

        builder.Property(x => x.UsarControlSeries)
            .HasColumnName("usar_control_series")
            .IsRequired();

        builder.Property(x => x.UsarFechaCaducidad)
            .HasColumnName("usar_fecha_caducidad")
            .IsRequired();

        builder.Property(x => x.Estado)
            .HasColumnName("estado")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasIndex(x => x.EmpresaId)
            .IsUnique();

        builder.HasOne(x => x.Empresa)
            .WithOne(x => x.ConfiguracionInventario)
            .HasForeignKey<ConfiguracionInventario>(
                x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}