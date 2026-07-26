namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoLote
{
    public long Id { get; set; }

    public long ProductoId { get; set; }

    public string NumeroLote { get; set; } = string.Empty;

    public DateOnly? FechaFabricacion { get; set; }

    public DateOnly? FechaCaducidad { get; set; }

    public decimal? CostoUnitarioBase { get; set; }

    public string? Observacion { get; set; }

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Producto? Producto { get; set; }

    public ICollection<ProductoLoteExistencia> Existencias { get; set; }
        = new List<ProductoLoteExistencia>();

    public ICollection<ProductoSerie> Series { get; set; }
        = new List<ProductoSerie>();
}
