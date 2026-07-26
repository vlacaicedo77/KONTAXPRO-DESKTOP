using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Configuracion;

public class SecuencialComprobante
{
    public long Id { get; set; }
    public long PuntoEmisionId { get; set; }
    public long TipoComprobanteId { get; set; }
    public long TipoAmbienteId { get; set; }
    public int UltimoSecuencial { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public PuntoEmision? PuntoEmision { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public TipoAmbiente? TipoAmbiente { get; set; }
}
