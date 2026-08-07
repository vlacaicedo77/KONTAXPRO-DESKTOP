using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;

namespace KONTAXPRO.Domain.Entities.Comercial;

public class EmpresaTercero
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long TerceroId { get; set; }
    public long? ListaPrecioId { get; set; }
    public bool CreditoHabilitado { get; set; }
    public decimal? CupoCredito { get; set; }
    public int? DiasCredito { get; set; }
    public string? MotivoBloqueoCredito { get; set; }
    public string? Observacion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public Tercero? Tercero { get; set; }
    public ListaPrecio? ListaPrecio { get; set; }
}
