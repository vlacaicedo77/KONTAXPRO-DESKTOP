namespace KONTAXPRO.Application.Models.Compras;

public sealed class ConfirmarCompraRecepcionRequest
{
    public Guid OperacionUuid { get; init; }
    public long CompraId { get; init; }
    public long BodegaId { get; init; }
    public DateTime FechaRecepcion { get; init; } = DateTime.Now;
    public string? Observacion { get; init; }
    public IReadOnlyList<ConfirmarCompraRecepcionLineaRequest> Lineas { get; init; }
        = [];
}

public sealed class ConfirmarCompraRecepcionLineaRequest
{
    public long CompraDetalleId { get; init; }
    public decimal CantidadPresentacion { get; init; }
    public IReadOnlyList<CompraRecepcionLoteRequest> Lotes { get; init; } = [];
    public IReadOnlyList<CompraRecepcionSerieRequest> Series { get; init; } = [];
}

public sealed class CompraRecepcionLoteRequest
{
    public string NumeroLote { get; init; } = string.Empty;
    public decimal CantidadBase { get; init; }
    public DateOnly? FechaElaboracion { get; init; }
    public DateOnly? FechaCaducidad { get; init; }
    public bool PermitirCrearLoteSimilar { get; init; }
}

public sealed class CompraRecepcionSerieRequest
{
    public string NumeroSerie { get; init; } = string.Empty;
    public string? NumeroLote { get; init; }
}

public sealed class CompraRecepcionResumenDto
{
    public long Id { get; init; }
    public string NumeroRecepcion { get; init; } = string.Empty;
    public DateTime FechaRecepcion { get; init; }
    public string Bodega { get; init; } = string.Empty;
    public int Lineas { get; init; }
    public decimal CantidadBase { get; init; }
    public string Display =>
        $"{NumeroRecepcion} · {FechaRecepcion.ToLocalTime():dd/MM/yyyy HH:mm} · {Bodega}";
}

public sealed class AnularCompraRecepcionRequest
{
    public long RecepcionId { get; init; }
    public string Motivo { get; init; } = string.Empty;
}
