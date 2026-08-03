using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Inventario;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KONTAXPRO.Infrastructure.Persistence.Configurations;

public sealed class MotivoOperacionInventarioConfiguration
    : IEntityTypeConfiguration<MotivoOperacionInventario>
{
    public void Configure(EntityTypeBuilder<MotivoOperacionInventario> b)
    {
        b.ToTable("motivos_operacion_inventario", "s_catalogos", t =>
        {
            t.HasCheckConstraint("ck_motivos_operacion_inventario_estado",
                "estado IN (0,1)");
            t.HasCheckConstraint("ck_motivos_operacion_inventario_tipo",
                "tipo_operacion IN ('INVENTARIO_INICIAL_ADICIONAL','AJUSTE_ENTRADA','AJUSTE_SALIDA','CONVERSION_CONTROL','CORRECCION_LOTE_SERIE')");
            t.HasCheckConstraint("ck_motivos_operacion_inventario_sistema",
                "(es_sistema AND empresa_id IS NULL) OR (NOT es_sistema AND empresa_id IS NOT NULL)");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id");
        b.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(80).IsRequired();
        b.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(160).IsRequired();
        b.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500);
        b.Property(x => x.TipoOperacion).HasColumnName("tipo_operacion").HasMaxLength(50).IsRequired();
        b.Property(x => x.EsSistema).HasColumnName("es_sistema").IsRequired();
        b.Property(x => x.Orden).HasColumnName("orden").IsRequired();
        b.Property(x => x.Estado).HasColumnName("estado").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone");
        b.HasIndex(x => x.Codigo).IsUnique().HasFilter("empresa_id IS NULL");
        b.HasIndex(x => new { x.EmpresaId, x.Codigo }).IsUnique()
            .HasFilter("empresa_id IS NOT NULL");
        b.HasIndex(x => new { x.TipoOperacion, x.Nombre }).IsUnique()
            .HasFilter("empresa_id IS NULL");
        b.HasIndex(x => new { x.EmpresaId, x.TipoOperacion, x.Nombre }).IsUnique()
            .HasFilter("empresa_id IS NOT NULL");
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CorreccionDatoInventarioConfiguration
    : IEntityTypeConfiguration<CorreccionDatoInventario>
{
    public void Configure(EntityTypeBuilder<CorreccionDatoInventario> b)
    {
        b.ToTable("correcciones_datos_inventario", "s_inventario", t =>
            t.HasCheckConstraint("ck_correcciones_datos_inventario_tipo",
                "tipo_entidad IN ('LOTE','SERIE')"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        b.Property(x => x.EmpresaId).HasColumnName("empresa_id").IsRequired();
        b.Property(x => x.ProductoId).HasColumnName("producto_id").IsRequired();
        b.Property(x => x.TipoEntidad).HasColumnName("tipo_entidad").HasMaxLength(10).IsRequired();
        b.Property(x => x.EntidadId).HasColumnName("entidad_id").IsRequired();
        b.Property(x => x.MotivoOperacionInventarioId).HasColumnName("motivo_operacion_inventario_id").IsRequired();
        b.Property(x => x.Motivo).HasColumnName("motivo").HasMaxLength(160).IsRequired();
        b.Property(x => x.UsuarioId).HasColumnName("usuario_id").IsRequired();
        b.Property(x => x.FechaCorreccion).HasColumnName("fecha_correccion").HasColumnType("timestamp with time zone").IsRequired();
        b.Property(x => x.ValorAnterior).HasColumnName("valor_anterior").HasMaxLength(1000).IsRequired();
        b.Property(x => x.ValorNuevo).HasColumnName("valor_nuevo").HasMaxLength(1000).IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp with time zone").IsRequired();
        b.HasOne(x => x.Empresa).WithMany().HasForeignKey(x => x.EmpresaId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Producto).WithMany().HasForeignKey(x => x.ProductoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MotivoOperacionInventario).WithMany(x => x.Correcciones)
            .HasForeignKey(x => x.MotivoOperacionInventarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EmpresaId, x.ProductoId, x.FechaCorreccion });
    }
}
