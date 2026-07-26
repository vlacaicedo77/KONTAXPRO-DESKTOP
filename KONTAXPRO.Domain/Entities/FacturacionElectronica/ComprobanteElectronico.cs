using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Configuracion;

namespace KONTAXPRO.Domain.Entities.FacturacionElectronica;

public static class EstadosComprobanteElectronico
{
    public const string Pendiente = "PENDIENTE";
    public const string Procesando = "PROCESANDO";
    public const string Generado = "GENERADO";
    public const string Firmado = "FIRMADO";
    public const string Enviado = "ENVIADO";
    public const string Recibido = "RECIBIDO";
    public const string Autorizado = "AUTORIZADO";
    public const string NoAutorizado = "NO_AUTORIZADO";
    public const string Error = "ERROR";
}

public static class TiposEventoComprobanteElectronico
{
    public const string Generacion = "GENERACION";
    public const string Firma = "FIRMA";
    public const string Envio = "ENVIO";
    public const string Recepcion = "RECEPCION";
    public const string Autorizacion = "AUTORIZACION";
    public const string Reintento = "REINTENTO";
    public const string Error = "ERROR";
}

public sealed class ComprobanteElectronico
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long TipoComprobanteId { get; set; }
    public long TipoOrigenComprobanteElectronicoId { get; set; }
    public long OrigenId { get; set; }
    public long TipoAmbienteId { get; set; }
    public long TipoEmisionId { get; set; }
    public string ClaveAcceso { get; set; } = string.Empty;
    public long EstadoComprobanteElectronicoId { get; set; }
    public DateTime? ProcesamientoIniciadoAt { get; set; }
    public Guid? ProcesadoPorInstalacionUuid { get; set; }
    public DateTime? XmlGeneradoAt { get; set; }
    public DateTime? XmlFirmadoAt { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public DateTime? FechaAutorizacion { get; set; }
    public string? NumeroAutorizacion { get; set; }
    public DateTime? XmlAutorizadoAt { get; set; }
    public DateTime? RideGeneradoAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public TipoComprobante? TipoComprobante { get; set; }
    public TipoOrigenComprobanteElectronico? TipoOrigenComprobanteElectronico
        { get; set; }
    public TipoAmbiente? TipoAmbiente { get; set; }
    public TipoEmision? TipoEmision { get; set; }
    public EstadoComprobanteElectronico? EstadoComprobanteElectronico
        { get; set; }
    public ICollection<ComprobanteElectronicoEvento> Eventos { get; set; } = [];

    public static bool PuedeCambiarEstado(string estadoActual, string nuevoEstado)
    {
        if (estadoActual == EstadosComprobanteElectronico.Autorizado)
            return nuevoEstado == EstadosComprobanteElectronico.Autorizado;

        return (estadoActual, nuevoEstado) switch
        {
            (EstadosComprobanteElectronico.Pendiente,
                EstadosComprobanteElectronico.Procesando) => true,
            (EstadosComprobanteElectronico.Procesando,
                EstadosComprobanteElectronico.Generado) => true,
            (EstadosComprobanteElectronico.Generado,
                EstadosComprobanteElectronico.Firmado) => true,
            (EstadosComprobanteElectronico.Firmado,
                EstadosComprobanteElectronico.Enviado) => true,
            (EstadosComprobanteElectronico.Enviado,
                EstadosComprobanteElectronico.Recibido) => true,
            (EstadosComprobanteElectronico.Recibido,
                EstadosComprobanteElectronico.Autorizado) => true,
            (EstadosComprobanteElectronico.Recibido,
                EstadosComprobanteElectronico.NoAutorizado) => true,
            (_, EstadosComprobanteElectronico.Error) => true,
            (EstadosComprobanteElectronico.Error,
                EstadosComprobanteElectronico.Procesando) => true,
            (EstadosComprobanteElectronico.Procesando,
                EstadosComprobanteElectronico.Procesando) => true,
            _ => estadoActual == nuevoEstado
        };
    }
}

public sealed class ComprobanteElectronicoEvento
{
    public long Id { get; set; }
    public long ComprobanteElectronicoId { get; set; }
    public string TipoEvento { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? Mensaje { get; set; }
    public string? InformacionAdicional { get; set; }
    public DateTime CreatedAt { get; set; }
    public ComprobanteElectronico? ComprobanteElectronico { get; set; }
}
