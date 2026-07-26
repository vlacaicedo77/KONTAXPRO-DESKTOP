namespace KONTAXPRO.Domain.Entities.Inventario;

public class ProductoCosto
{
    public long Id { get; set; }

    public long ProductoId { get; set; }

    public decimal UltimoPrecioCompra { get; set; }

    public decimal UltimoCostoEfectivo { get; set; }

    public decimal CostoPromedio { get; set; }

    public decimal CostoMaximoExistencia { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Producto? Producto { get; set; }
}