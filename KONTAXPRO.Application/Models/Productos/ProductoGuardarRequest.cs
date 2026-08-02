namespace KONTAXPRO.Application.Models.Productos;

public class ProductoGuardarRequest
{
    public long? Id { get; set; }

    public string? CodigoInterno { get; set; }

    public long EmpresaId { get; set; }

    public long? EstablecimientoId { get; set; }

    public long? CategoriaProductoId { get; set; }

    public long? MarcaId { get; set; }

    public long UnidadMedidaBaseId { get; set; }

    public List<long> TarifasImpuestoIds { get; set; } = [];

    public string Nombre { get; set; } = string.Empty;

    public string? Modelo { get; set; }

    public string PresentacionNombre { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public bool SinCodigoBarras { get; set; }

    public string? Descripcion { get; set; }

    public string TipoProducto { get; set; } = "PRODUCTO";

    public string TipoControlInventario { get; set; } = "NORMAL";

    public bool ManejaInventario { get; set; } = true;

    public bool ManejaLotes { get; set; }

    public bool ManejaSeries { get; set; }

    public bool ManejaFechaCaducidad { get; set; }

    public bool AlertaCaducidad { get; set; } = true;

    public int DiasAlertaCaducidad { get; set; } = 30;

    public string? Observacion { get; set; }

    public List<ProductoPresentacionDto> Presentaciones { get; set; } = [];

    public List<ProductoExistenciaDto> Existencias { get; set; } = [];

    public List<ProductoInventarioInicialRequest> InventariosIniciales { get; set; } = [];
}

public sealed class ProductoInventarioInicialRequest
{
    public long? MovimientoId { get; set; }
    public long BodegaId { get; set; }
    public long UsuarioId { get; set; }
    public string PresentacionCodigo { get; set; } = "BASE";
    public decimal CantidadPresentaciones { get; set; }
    public decimal CostoUnitarioPresentacion { get; set; }
    public string? Ubicacion { get; set; }
    public decimal StockMinimo { get; set; }
    public string? NumeroLote { get; set; }
    public DateOnly? FechaElaboracion { get; set; }
    public DateOnly? FechaCaducidad { get; set; }
    public List<string> NumerosSerie { get; set; } = [];
    public List<ProductoInventarioInicialLoteRequest> Lotes { get; set; } = [];
    public string BodegaCodigo { get; set; } = string.Empty;
    public string BodegaNombre { get; set; } = string.Empty;
    public string BodegaDisplay =>
        BodegaDisplayFormatter.Format(BodegaCodigo, BodegaNombre);
    public string PresentacionNombre { get; set; } = string.Empty;
    public bool EsHistorico { get; set; }
    public string EstadoTexto => EsHistorico ? "CONFIRMADA" : "PENDIENTE";
    public string ResumenLotes => string.Join(Environment.NewLine, Lotes.Select(x =>
        $"{x.NumeroLote}: {x.CantidadBase:0.######}" +
        FormatearFechasLote(x)));
    public string ResumenSeries => string.Join(
        Environment.NewLine,
        NumerosSerie.Select(FormatearSerie));

    private static string FormatearSerie(string valor)
    {
        var partes = valor.Split('|', 2, StringSplitOptions.TrimEntries);
        return partes.Length == 2
            ? $"{partes[0]} → {partes[1]}"
            : valor;
    }

    private static string FormatearFechasLote(
        ProductoInventarioInicialLoteRequest lote)
    {
        if (lote.FechaElaboracion.HasValue &&
            lote.FechaCaducidad.HasValue)
            return $" ({lote.FechaElaboracion:dd/MM/yyyy} → " +
                   $"{lote.FechaCaducidad:dd/MM/yyyy})";

        if (lote.FechaElaboracion.HasValue)
            return $" (Elab. {lote.FechaElaboracion:dd/MM/yyyy})";

        return lote.FechaCaducidad.HasValue
            ? $" (Cad. {lote.FechaCaducidad:dd/MM/yyyy})"
            : string.Empty;
    }
}

public sealed class ProductoInventarioInicialLoteRequest
{
    public string NumeroLote { get; set; } = string.Empty;
    public decimal CantidadBase { get; set; }
    public DateTime? FechaElaboracion { get; set; }
    public DateTime? FechaCaducidad { get; set; }
}
