namespace KONTAXPRO.Application.Models.FacturacionElectronica;

public sealed record FacturaElectronicaSri(
    string ClaveAcceso,
    int Ambiente,
    int TipoEmision,
    string RucEmisor,
    string RazonSocialEmisor,
    string? NombreComercialEmisor,
    string DireccionMatriz,
    string Establecimiento,
    string PuntoEmision,
    int Secuencial,
    DateOnly FechaEmision,
    string? DireccionEstablecimiento,
    string? ContribuyenteEspecial,
    bool? ObligadoContabilidad,
    string? LeyendaRegimen,
    string TipoIdentificacionComprador,
    string RazonSocialComprador,
    string IdentificacionComprador,
    string? DireccionComprador,
    decimal TotalSinImpuestos,
    decimal TotalDescuento,
    decimal ImporteTotal,
    IReadOnlyList<TotalImpuestoFacturaSri> TotalesImpuestos,
    IReadOnlyList<DetalleFacturaSri> Detalles,
    IReadOnlyList<PagoFacturaSri> Pagos,
    IReadOnlyList<CampoAdicionalFacturaSri> InformacionAdicional,
    string Moneda = "DOLAR",
    string Version = "2.1.0",
    string? AgenteRetencion = null);

public sealed record TotalImpuestoFacturaSri(
    string Codigo,
    string CodigoPorcentaje,
    decimal BaseImponible,
    decimal Tarifa,
    decimal Valor);

public sealed record DetalleFacturaSri(
    string Descripcion,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal Descuento,
    decimal PrecioTotalSinImpuesto,
    IReadOnlyList<ImpuestoDetalleFacturaSri> Impuestos,
    string? CodigoPrincipal = null,
    string? CodigoAuxiliar = null,
    string? UnidadMedida = null,
    IReadOnlyList<DetalleAdicionalFacturaSri>? DetallesAdicionales = null);

public sealed record ImpuestoDetalleFacturaSri(
    string Codigo,
    string CodigoPorcentaje,
    decimal Tarifa,
    decimal BaseImponible,
    decimal Valor);

public sealed record DetalleAdicionalFacturaSri(string Nombre, string Valor);

public sealed record PagoFacturaSri(
    string CodigoFormaPagoSri,
    decimal Total,
    decimal? Plazo = null,
    string? UnidadTiempo = null);

public sealed record CampoAdicionalFacturaSri(string Nombre, string Valor);

public sealed record ResultadoValidacionXmlSri(
    bool EsValido,
    IReadOnlyList<string> Errores);

public sealed record CertificadoFirmaSriInfo(
    string Titular,
    string Emisor,
    string NumeroSerie,
    string? Identificacion,
    DateTimeOffset ValidoDesde,
    DateTimeOffset ValidoHasta,
    bool TieneClavePrivada,
    bool EsRsa,
    bool Vigente,
    int DiasParaCaducar)
{
    public string? Thumbprint { get; init; }
}

public enum EstadoCertificadoSri
{
    Valido,
    ProximoACaducar,
    Caducado,
    AunNoVigente,
    Revocado,
    EstadoRevocacionDesconocido,
    CadenaNoValida,
    SinClavePrivada,
    ArchivoOContrasenaInvalida,
    Error
}

public enum EstadoCadenaCertificadoSri
{
    Valida,
    NoValida,
    NoComprobada
}

public enum EstadoRevocacionCertificadoSri
{
    NoRevocado,
    Revocado,
    Desconocido,
    NoComprobado
}

public sealed record ResultadoValidacionCertificadoSri(
    bool EsValido,
    CertificadoFirmaSriInfo? Certificado,
    IReadOnlyList<string> Errores)
{
    public EstadoCertificadoSri Estado { get; init; } =
        EstadoCertificadoSri.Error;
    public EstadoCadenaCertificadoSri EstadoCadena { get; init; } =
        EstadoCadenaCertificadoSri.NoComprobada;
    public EstadoRevocacionCertificadoSri EstadoRevocacion { get; init; } =
        EstadoRevocacionCertificadoSri.NoComprobado;
    public DateTimeOffset? FechaRevocacion { get; init; }
    public IReadOnlyList<string> Advertencias { get; init; } = [];
    public IReadOnlyList<string> DetallesCadena { get; init; } = [];

    public bool ValidacionCompleta => EsValido &&
        EstadoRevocacion == EstadoRevocacionCertificadoSri.NoRevocado;
}

public sealed record ResultadoVerificacionFirmaSri(
    bool EsValida,
    IReadOnlyList<string> Errores);

public sealed record ConfiguracionFacturacionElectronicaDto(
    long EmpresaId,
    long TipoAmbienteId,
    string AmbienteCodigo,
    long TipoEmisionId,
    string TipoEmisionCodigo,
    bool Habilitada,
    string? CertificadoNombre,
    string? CertificadoTitular,
    string? CertificadoEmisor,
    string? CertificadoNumeroSerie,
    DateOnly? CertificadoFechaInicio,
    DateOnly? CertificadoFechaCaducidad,
    uint Version);

public sealed record SecuencialComprobanteSriDto(
    long Id,
    long EstablecimientoId,
    string EstablecimientoCodigo,
    string EstablecimientoNombre,
    long PuntoEmisionId,
    string PuntoEmisionCodigo,
    string PuntoEmisionNombre,
    long TipoComprobanteId,
    string TipoComprobanteCodigoSri,
    string TipoComprobanteNombre,
    long TipoAmbienteId,
    string Ambiente,
    int UltimoSecuencial,
    bool TieneComprobantesKontax)
{
    public int? ProximoSecuencial => UltimoSecuencial < 999_999_999
        ? UltimoSecuencial + 1
        : null;

    public string UltimoSecuencialTexto =>
        UltimoSecuencial.ToString("D9",
            System.Globalization.CultureInfo.InvariantCulture);

    public string ProximoSecuencialTexto => ProximoSecuencial.HasValue
        ? ProximoSecuencial.Value.ToString("D9",
            System.Globalization.CultureInfo.InvariantCulture)
        : "AGOTADO";

    public string NumeroCompletoProximo => ProximoSecuencial.HasValue
        ? $"{EstablecimientoCodigo}-{PuntoEmisionCodigo}-{ProximoSecuencialTexto}"
        : "SECUENCIAL AGOTADO";
}

public sealed record EstablecimientoPuntoEmisionSriDto(
    long Id,
    string Codigo,
    string Nombre)
{
    public string Descripcion => $"{Codigo} · {Nombre}";
}

public sealed record PuntoEmisionSriDto(
    long Id,
    long EstablecimientoId,
    string EstablecimientoCodigo,
    string Codigo,
    string Nombre,
    bool Activo,
    bool EsPredeterminado,
    bool TieneComprobantesKontax)
{
    public string Descripcion => $"{Codigo} · {Nombre}";
    public string EstadoTexto => Activo ? "ACTIVO" : "INACTIVO";
    public string EstadoPresentacion => EsPredeterminado
        ? $"{EstadoTexto} · PREFERIDO"
        : EstadoTexto;
}

public sealed record AdministracionPuntosEmisionSriDto(
    IReadOnlyList<EstablecimientoPuntoEmisionSriDto> Establecimientos,
    IReadOnlyList<PuntoEmisionSriDto> Puntos,
    IReadOnlyList<PlantillaSecuencialPuntoEmisionSriDto>
        PlantillasSecuenciales);

public sealed record PlantillaSecuencialPuntoEmisionSriDto(
    long TipoComprobanteId,
    string TipoComprobanteCodigoSri,
    string TipoComprobanteNombre,
    long TipoAmbienteId,
    string Ambiente);

public sealed record SecuencialInicialPuntoEmisionSriDto(
    long TipoComprobanteId,
    long TipoAmbienteId,
    int UltimoSecuencial);

public sealed record CrearPuntoEmisionSriDto(
    long EstablecimientoId,
    string Codigo,
    string Nombre,
    bool EstablecerPredeterminado,
    bool ContinuarNumeracionExistente,
    IReadOnlyList<SecuencialInicialPuntoEmisionSriDto>
        SecuencialesIniciales);

public sealed record ResultadoDiagnosticoFacturacionElectronica(
    bool ListoParaFacturar,
    IReadOnlyList<ItemDiagnosticoFacturacionElectronica> Items);

public sealed record ItemDiagnosticoFacturacionElectronica(
    string Codigo,
    string Descripcion,
    bool Correcto,
    string Detalle)
{
    public string Nivel { get; init; } = Correcto ? "CORRECTO" : "ERROR";
}

public sealed record ResultadoDiagnosticoComunicacionSri(
    bool Disponible,
    IReadOnlyList<ItemDiagnosticoFacturacionElectronica> Items);

public sealed record MaterialCertificadoSri(
    byte[] Pkcs12,
    string Password);

public sealed record ResultadoMotorFacturaSri(
    string ClaveAcceso,
    byte[] XmlGenerado,
    byte[] XmlFirmado,
    ResultadoSri Recepcion,
    ResultadoSri? Autorizacion);
