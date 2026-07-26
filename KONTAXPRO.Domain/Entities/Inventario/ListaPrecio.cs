using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class ListaPrecio
{
    public long Id { get; set; }

    public long EmpresaId { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool EsPredeterminada { get; set; }

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
}