namespace KONTAXPRO.Domain.Entities.Catalogos;

public class TarifaImpuesto
{
    public long Id { get; set; }

    public long ImpuestoId { get; set; }

    public string CodigoSri { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string TipoCalculo { get; set; } = string.Empty;

    public decimal? Porcentaje { get; set; }

    public decimal? ValorEspecifico { get; set; }

    public DateOnly VigenteDesde { get; set; }

    public DateOnly? VigenteHasta { get; set; }

    public string? Descripcion { get; set; }

    public int Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Impuesto? Impuesto { get; set; }
}
