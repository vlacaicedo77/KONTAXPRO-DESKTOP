namespace KONTAXPRO.Application.Models.Compras;

public sealed class CompraCatalogoDto
{
    public IReadOnlyList<CompraCatalogoItemDto> Items { get; init; } = [];
    public int Total { get; init; }
    public int TotalFiltrado { get; init; }
    public int PendientesRecepcion { get; init; }
    public int Parciales { get; init; }
    public int Recibidas { get; init; }
    public decimal TotalCompras { get; init; }
}

public sealed class CompraCatalogoItemDto
{
    public long Id { get; init; }
    public DateOnly FechaEmision { get; init; }
    public string NumeroDocumento { get; init; } = string.Empty;
    public string Proveedor { get; init; } = string.Empty;
    public string IdentificacionProveedor { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string Estado { get; init; } = string.Empty;
    public bool TieneXml { get; init; }
    public string Origen { get; init; } = string.Empty;
    public int LineasPendientes { get; init; }
    public int RecepcionesConfirmadas { get; init; }
    public bool PuedeEditar { get; init; }
}

public sealed class CompraDetalleDto
{
    public long Id { get; init; }
    public string TipoCompra { get; init; } = string.Empty;
    public string NumeroDocumento { get; init; } = string.Empty;
    public string Proveedor { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public IReadOnlyList<CompraDetalleLineaDto> Lineas { get; init; } = [];
}

public sealed class CompraDetalleLineaDto
{
    public long Id { get; init; }
    public int Orden { get; init; }
    public long ProductoId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public string? Producto { get; init; }
    public string? Presentacion { get; init; }
    public decimal CantidadFacturada { get; init; }
    public decimal CantidadRecibida { get; init; }
    public decimal FactorConversion { get; init; }
    public decimal CantidadPendiente => CantidadFacturada - CantidadRecibida;
    public bool EsInventariable { get; init; }
    public bool ManejaLotes { get; init; }
    public bool ManejaSeries { get; init; }
    public bool ManejaFechaCaducidad { get; init; }
}

public sealed class GuardarCompraManualRequest
{
    public long TerceroProveedorId { get; init; }
    public long EstablecimientoId { get; init; }
    public long? TipoComprobanteId { get; init; }
    public string TipoCompra { get; init; } = "FACTURADA";
    public string? NumeroDocumento { get; init; }
    public DateOnly FechaEmision { get; init; }
    public bool EsCredito { get; init; }
    public DateOnly? FechaVencimiento { get; init; }
    public string? Observacion { get; init; }
    public IReadOnlyList<GuardarCompraManualLineaRequest> Lineas { get; init; }
        = [];
}

public sealed class CompraManualEdicionDto
{
    public long Id { get; init; }
    public long TerceroProveedorId { get; init; }
    public long EstablecimientoId { get; init; }
    public long? TipoComprobanteId { get; init; }
    public string TipoCompra { get; init; } = string.Empty;
    public string? NumeroDocumento { get; init; }
    public DateOnly FechaEmision { get; init; }
    public bool EsCredito { get; init; }
    public DateOnly? FechaVencimiento { get; init; }
    public string? Observacion { get; init; }
    public IReadOnlyList<CompraManualEdicionLineaDto> Lineas { get; init; } = [];
}

public sealed class CompraManualEdicionLineaDto
{
    public string Descripcion { get; init; } = string.Empty;
    public bool EsInventariable { get; init; }
    public long? ProductoPresentacionId { get; init; }
    public string ClasificacionContable { get; init; } = string.Empty;
    public long? CuentaContableId { get; init; }
    public decimal CantidadPresentacion { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal DescuentoValor { get; init; }
    public long? TarifaImpuestoId { get; init; }
    public bool EsBonificacion { get; init; }
}

public sealed class GuardarCompraManualLineaRequest
{
    public string Descripcion { get; init; } = string.Empty;
    public bool EsInventariable { get; init; }
    public long? ProductoPresentacionId { get; init; }
    public string ClasificacionContable { get; init; } = "INVENTARIO";
    public long? CuentaContableId { get; init; }
    public decimal CantidadPresentacion { get; init; }
    public decimal PrecioUnitario { get; init; }
    public decimal DescuentoValor { get; init; }
    public long? TarifaImpuestoId { get; init; }
    public bool EsBonificacion { get; init; }
}

public sealed class CompraFormularioCatalogosDto
{
    public IReadOnlyList<CompraProveedorItemDto> Proveedores { get; init; } = [];
    public IReadOnlyList<CompraCatalogoItemBasicoDto> Establecimientos
        { get; init; } = [];
    public IReadOnlyList<CompraCatalogoItemBasicoDto> Bodegas { get; init; } = [];
    public IReadOnlyList<CompraCatalogoItemBasicoDto> TiposComprobante
        { get; init; } = [];
    public IReadOnlyList<CompraPresentacionItemDto> Presentaciones
        { get; init; } = [];
    public IReadOnlyList<CompraCuentaContableItemDto> CuentasContables
        { get; init; } = [];
    public IReadOnlyList<CompraTarifaImpuestoItemDto> TarifasImpuesto
        { get; init; } = [];
}

public sealed class CompraTarifaImpuestoItemDto
{
    public long Id { get; init; }
    public string CodigoSri { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public decimal Porcentaje { get; init; }
    public string Display => $"{Porcentaje:0.##} %";
}

public sealed class CompraCuentaContableItemDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Display => $"{Codigo} · {Nombre}";
}

public sealed class CompraProveedorItemDto
{
    public long Id { get; init; }
    public string Identificacion { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string Display => $"{Identificacion} · {Nombre}";
}

public sealed class CompraCatalogoItemBasicoDto
{
    public long Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public bool PermiteVentaFacturada { get; init; }
    public string Display => string.IsNullOrWhiteSpace(Codigo)
        ? Nombre : $"{Codigo} · {Nombre}";
}

public sealed class CompraPresentacionItemDto
{
    public long Id { get; init; }
    public long ProductoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Producto { get; init; } = string.Empty;
    public string Presentacion { get; init; } = string.Empty;
    public decimal FactorConversion { get; init; }
    public bool EsPresentacionBase { get; init; }
    public long? TarifaImpuestoId { get; init; }
    public string? CodigoPorcentajeSri { get; init; }
    public decimal? PorcentajeImpuesto { get; init; }
    public string? NombreImpuesto { get; init; }
    public string ConversionDisplay => EsPresentacionBase
        ? "BASE · ×1"
        : $"×{FactorConversion:0.######}";
    public string PresentationBadgeDisplay => EsPresentacionBase
        ? ConversionDisplay
        : $"{Codigo} · {ConversionDisplay}";
    public string Display =>
        $"{Codigo} · {Producto} · {Presentacion} · {ConversionDisplay}";
}
