using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ClienteSriComprobantesElectronicos(
    IClienteRecepcionSri recepcion,
    IClienteAutorizacionSri autorizacion) : IClienteSriComprobantesElectronicos
{
    public bool TieneConfiguracionSegura => true;

    public async Task<ResultadoSri> EnviarAsync(
        string claveAcceso,
        Stream xmlFirmado,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        await xmlFirmado.CopyToAsync(memory, cancellationToken);
        return await recepcion.EnviarAsync(Ambiente(claveAcceso), claveAcceso,
            memory.ToArray(), cancellationToken);
    }

    public Task<ResultadoSri> ConsultarAutorizacionAsync(
        string claveAcceso,
        CancellationToken cancellationToken = default) =>
        autorizacion.ConsultarAsync(Ambiente(claveAcceso), claveAcceso,
            cancellationToken);

    private static int Ambiente(string key)
    {
        if (key.Length != 49 || !key.All(char.IsDigit) || key[23] is not ('1' or '2'))
            throw new ArgumentException("La clave de acceso no contiene un ambiente válido.");
        return key[23] - '0';
    }
}
