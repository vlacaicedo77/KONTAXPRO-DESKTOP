using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class Bodega
{
    public long Id { get; set; }

    public long EmpresaId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EsPrincipal { get; set; }

    public bool PermiteVentas { get; set; } = true;

    public bool PermiteCompras { get; set; } = true;

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }

    public ICollection<ProductoExistencia> ProductosExistencias { get; set; }
    = new List<ProductoExistencia>();
}