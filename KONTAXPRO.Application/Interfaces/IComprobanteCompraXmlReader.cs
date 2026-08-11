using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Interfaces;

public interface IComprobanteCompraXmlReader
{
    Task<LecturaComprobanteCompraResultado> LeerFacturaAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default);
}

public interface IConsultaAutorizacionComprobanteSri
{
    Task<ConsultaAutorizacionSriDto> ConsultarAsync(
        string claveAcceso,
        string ambiente,
        CancellationToken cancellationToken = default);
}
