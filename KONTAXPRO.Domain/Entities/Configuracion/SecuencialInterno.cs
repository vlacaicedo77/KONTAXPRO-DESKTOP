using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Configuracion;

public class SecuencialInterno
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long TipoDocumentoInternoId { get; set; }
    public long UltimoSecuencial { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public TipoDocumentoInterno? TipoDocumentoInterno { get; set; }
}
