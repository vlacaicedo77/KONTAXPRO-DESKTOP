using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;

namespace KONTAXPRO.Domain.Entities.Catalogos;

public class MotivoOperacionInventario
{
    public long Id { get; set; }
    public long? EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string TipoOperacion { get; set; } = string.Empty;
    public bool EsSistema { get; set; }
    public int Orden { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public ICollection<AjusteInventario> Ajustes { get; set; } = [];
    public ICollection<ConversionControlInventario> Conversiones { get; set; } = [];
    public ICollection<CorreccionDatoInventario> Correcciones { get; set; } = [];
}
