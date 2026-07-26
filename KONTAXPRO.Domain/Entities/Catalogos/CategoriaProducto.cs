using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;

namespace KONTAXPRO.Domain.Entities.Catalogos;

public class CategoriaProducto
{
    public long Id { get; set; }
    public Guid Uuid { get; set; } = Guid.NewGuid();
    public long EmpresaId { get; set; }
    public long? CategoriaPadreId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public CategoriaProducto? CategoriaPadre { get; set; }
    public ICollection<CategoriaProducto> Subcategorias { get; set; } =
        new List<CategoriaProducto>();
    public ICollection<Producto> Productos { get; set; } =
        new List<Producto>();
}
