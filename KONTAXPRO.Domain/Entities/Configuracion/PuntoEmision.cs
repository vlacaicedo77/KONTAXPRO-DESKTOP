namespace KONTAXPRO.Domain.Entities.Configuracion;

public class PuntoEmision
{
    public long Id { get; set; }
    public long EstablecimientoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Establecimiento? Establecimiento { get; set; }
    public ICollection<SecuencialComprobante> SecuencialesComprobantes
        { get; set; } = new List<SecuencialComprobante>();
    public ICollection<UsuarioConfiguracionEmpresa>
        UsuariosConfiguracionesEmpresa { get; set; } =
        new List<UsuarioConfiguracionEmpresa>();
}
