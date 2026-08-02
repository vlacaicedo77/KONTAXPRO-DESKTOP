namespace KONTAXPRO.Application.Models.Inventario;

public class IngresoInventarioDetalleRequest
{
    public long ProductoId { get; set; }

    public long ProductoPresentacionId { get; set; }

    /// <summary>
    /// Cantidad expresada en la presentación elegida.
    /// Ejemplo: 10 cajas.
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Costo total de esta línea.
    /// No es necesariamente el costo unitario.
    /// Ejemplo: 10 cajas cuestan en total $1.200.
    /// </summary>
    public decimal CostoTotal { get; set; }

    public string? Ubicacion { get; set; }
    public decimal StockMinimo { get; set; }

    public string? NumeroLote { get; set; }

    public DateOnly? FechaElaboracion { get; set; }

    public DateOnly? FechaCaducidad { get; set; }

    public List<string> NumerosSerie { get; set; } = [];

    public List<IngresoInventarioLoteRequest> Lotes { get; set; } = [];
    public List<IngresoInventarioSerieRequest> Series { get; set; } = [];

    public string? Observacion { get; set; }
}

public sealed class IngresoInventarioSerieRequest
{
    public string NumeroSerie { get; set; } = string.Empty;
    public string? NumeroLote { get; set; }
}

public sealed class IngresoInventarioLoteRequest
{
    public string NumeroLote { get; set; } = string.Empty;
    public decimal CantidadBase { get; set; }
    public DateOnly? FechaElaboracion { get; set; }
    public DateOnly? FechaCaducidad { get; set; }
    public bool PermitirCrearLoteSimilar { get; set; }
}
