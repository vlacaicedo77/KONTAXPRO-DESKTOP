namespace KONTAXPRO.Application.Models.FacturacionElectronica;

public sealed record SolicitudClaveAccesoSri(
    DateOnly FechaEmision,
    string CodigoTipoComprobante,
    string Ruc,
    int Ambiente,
    string Establecimiento,
    string PuntoEmision,
    int Secuencial,
    string CodigoNumerico,
    int TipoEmision = 1);

