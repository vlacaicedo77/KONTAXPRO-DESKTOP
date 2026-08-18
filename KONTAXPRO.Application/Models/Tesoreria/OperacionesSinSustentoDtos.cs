using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Models.Tesoreria;

public enum OperacionSinSustentoCatalogoOrden
{
    Fecha,
    Operacion,
    Beneficiario,
    Fondo,
    Total,
    Estado
}

public sealed class OperacionSinSustentoRequest
{
    public long? OperacionSustituidaId { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long UsuarioId { get; set; }
    public string TipoOperacion { get; set; } = "GASTO";
    public string MedioSalida { get; set; } = "CAJA";
    public long? CajaSesionId { get; set; }
    public long? CuentaBancariaId { get; set; }
    public long? BodegaId { get; set; }
    public DateOnly Fecha { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string Beneficiario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public string? EvidenciaNombre { get; set; }
    public byte[]? EvidenciaContenido { get; set; }
    public List<OperacionSinSustentoDetalleRequest> Detalles { get; set; } = [];
}

public sealed class OperacionSinSustentoCatalogoRequest
{
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long UsuarioId { get; set; }
    public string? Busqueda { get; set; }
    public string Estado { get; set; } = "TODAS";
    public string Tipo { get; set; } = "TODOS";
    public OperacionSinSustentoCatalogoOrden Orden { get; set; } =
        OperacionSinSustentoCatalogoOrden.Fecha;
    public bool OrdenDescendente { get; set; } = true;
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; set; } = 50;
}

public sealed class OperacionSinSustentoCatalogoDto
{
    public List<OperacionSinSustentoItemDto> Items { get; set; } = [];
    public int Total { get; set; }
    public int Confirmadas { get; set; }
    public int Gastos { get; set; }
    public int Inventario { get; set; }
    public int Anuladas { get; set; }
    public decimal TotalConfirmado { get; set; }
}

public sealed class OperacionSinSustentoItemDto
{
    public long Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string Beneficiario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string MedioSalida { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public bool TieneEvidencia { get; set; }
    public int Detalles { get; set; }
}

public sealed class OperacionSinSustentoDetalleDto
{
    public long Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public string MedioSalida { get; set; } = string.Empty;
    public long? CajaSesionId { get; set; }
    public long? CuentaBancariaId { get; set; }
    public long? BodegaId { get; set; }
    public string Beneficiario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public decimal Total { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoAnulacion { get; set; }
    public long? SustituyeAId { get; set; }
    public long? SustituidaPorId { get; set; }
    public bool TieneEvidencia { get; set; }
    public string? EvidenciaNombre { get; set; }
    public List<OperacionSinSustentoLineaDto> Lineas { get; set; } = [];
}

public sealed class OperacionSinSustentoLineaDto
{
    public long? CuentaContableId { get; set; }
    public string? Cuenta { get; set; }
    public long? ProductoId { get; set; }
    public long? ProductoPresentacionId { get; set; }
    public string? Producto { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal Cantidad { get; set; }
    public decimal Factor { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public decimal Total { get; set; }
    public List<IngresoInventarioLoteRequest> Lotes { get; set; } = [];
    public List<IngresoInventarioSerieRequest> Series { get; set; } = [];
}

public sealed record AnularOperacionSinSustentoRequest(
    long EmpresaId, long UsuarioId, long OperacionId, string Motivo);

public sealed class OperacionSinSustentoDetalleRequest
{
    public bool ProductoCreadoContextualmente { get; set; }
    public long? CuentaContableId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public long? ProductoId { get; set; }
    public long? ProductoPresentacionId { get; set; }
    public decimal Cantidad { get; set; } = 1;
    public decimal CostoTotal { get; set; }
    public string? Ubicacion { get; set; }
    public decimal StockMinimo { get; set; }
    public List<IngresoInventarioLoteRequest> Lotes { get; set; } = [];
    public List<IngresoInventarioSerieRequest> Series { get; set; } = [];
    public List<OperacionSinSustentoPrecioRequest> Precios { get; set; } = [];
}

public sealed class OperacionSinSustentoPrecioRequest
{
    public long ProductoPresentacionId { get; set; }
    public long ListaPrecioId { get; set; }
    public string MetodoCalculo { get; set; } = "PRECIO_FIJO";
    public decimal? Porcentaje { get; set; }
    public decimal? Precio { get; set; }
    public int Estado { get; set; } = 1;
}

public sealed class OperacionSinSustentoCatalogosDto
{
    public List<FondoSalidaDto> CajasAbiertas { get; set; } = [];
    public List<FondoSalidaDto> CuentasBancarias { get; set; } = [];
    public List<CuentaGastoDto> CuentasGasto { get; set; } = [];
    public List<FondoSalidaDto> BodegasNoFacturables { get; set; } = [];
}

public sealed record FondoSalidaDto(long Id, string Nombre);
public sealed record CuentaGastoDto(long Id, string Codigo, string Nombre);
public sealed record SoporteSinSustentoGuardadoDto(
    string RutaRelativa, string Sha256, long Tamano);
public sealed record SoporteSinSustentoContenidoDto(
    string NombreArchivo, string TipoContenido, byte[] Contenido);
public sealed record OperacionSinSustentoResult(bool Success, long? Id, string Message)
{
    public static OperacionSinSustentoResult Ok(long id, string message) =>
        new(true, id, message);
    public static OperacionSinSustentoResult Fail(string message) =>
        new(false, null, message);
}
