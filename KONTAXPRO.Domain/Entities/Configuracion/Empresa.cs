using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Configuracion;

public class Empresa
{
    public long Id { get; set; }
    public long RegimenTributarioId { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public bool ObligadoContabilidad { get; set; }
    public string? ContribuyenteEspecialNumero { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public RegimenTributario? RegimenTributario { get; set; }
    public ICollection<UsuarioEmpresa> UsuariosEmpresas { get; set; } =
        new List<UsuarioEmpresa>();
    public ICollection<Establecimiento> Establecimientos { get; set; } =
        new List<Establecimiento>();
    public ICollection<UsuarioConfiguracionEmpresa>
        UsuariosConfiguracionesEmpresa { get; set; } =
        new List<UsuarioConfiguracionEmpresa>();
    public FacturacionElectronica? FacturacionElectronica { get; set; }

    // Navegaciones de módulos que todavía conservan el modelo previo.
    public ConfiguracionInventario? ConfiguracionInventario { get; set; }
    public ICollection<CategoriaProducto> CategoriasProducto { get; set; } =
        new List<CategoriaProducto>();
    public ICollection<ListaPrecio> ListasPrecio { get; set; } =
        new List<ListaPrecio>();
    public ICollection<Bodega> Bodegas { get; set; } = new List<Bodega>();
    public ICollection<Producto> Productos { get; set; } =
        new List<Producto>();
}
