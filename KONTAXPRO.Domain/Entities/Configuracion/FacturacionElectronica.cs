using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Configuracion;

public class FacturacionElectronica
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long TipoAmbienteId { get; set; }
    public long TipoEmisionId { get; set; }
    public string? CertificadoNombre { get; set; }
    public DateOnly? CertificadoFechaCaducidad { get; set; }
    public bool Habilitada { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public TipoAmbiente? TipoAmbiente { get; set; }
    public TipoEmision? TipoEmision { get; set; }
}
