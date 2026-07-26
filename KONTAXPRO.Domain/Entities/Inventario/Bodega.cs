using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class Bodega
{
    public long Id { get; set; }
    public long EstablecimientoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool PermiteTransferenciasInternas { get; set; }
    public bool PermiteVentaFacturada { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Establecimiento? Establecimiento { get; set; }
    public ICollection<ProductoExistencia> ProductosExistencias { get; set; } = [];
}
