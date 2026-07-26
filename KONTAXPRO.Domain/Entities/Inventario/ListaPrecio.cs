using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class ListaPrecio
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsListaBase { get; set; }
    public decimal? PorcentajeDescuentoPredeterminado { get; set; }
    public int Orden { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public ICollection<ProductoPresentacionPrecio> Precios { get; set; } = [];
}
