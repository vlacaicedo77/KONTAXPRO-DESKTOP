namespace KONTAXPRO.Domain.Entities.Configuracion;

public class Establecimiento
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Prefijo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public bool EsMatriz { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public ICollection<PuntoEmision> PuntosEmision { get; set; } =
        new List<PuntoEmision>();
    public ICollection<SecuencialInterno> SecuencialesInternos { get; set; } =
        new List<SecuencialInterno>();
    public ICollection<UsuarioConfiguracionEmpresa>
        UsuariosConfiguracionesEmpresa { get; set; } =
        new List<UsuarioConfiguracionEmpresa>();
}
