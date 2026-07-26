using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class WorkerFacturacionElectronica(
    IContextoInstalacion contextoInstalacion,
    IClienteSriComprobantesElectronicos clienteSri,
    IFirmadorXmlComprobanteElectronico firmador,
    IProcesadorFacturacionElectronica procesador)
    : IWorkerFacturacionElectronica
{
    public async Task<ResultadoCicloFacturacionElectronica> EjecutarCicloAsync(
        CancellationToken cancellationToken = default)
    {
        if (!contextoInstalacion.EsServidor)
            return ResultadoCicloFacturacionElectronica.Omitido(
                "La facturación automática sólo se ejecuta en un nodo SERVIDOR.");

        if (!clienteSri.TieneConfiguracionSegura ||
            !firmador.TieneConfiguracionSegura)
            return ResultadoCicloFacturacionElectronica.Omitido(
                "No existe configuración segura de firma y conexión SRI.");

        var recuperados =
            await procesador.RecuperarProcesamientosAbandonadosAsync(
                contextoInstalacion.InstalacionId,
                TimeSpan.FromMinutes(15),
                cancellationToken);

        var procesados = await procesador.ProcesarPendientesAsync(
            contextoInstalacion.InstalacionId,
            cancellationToken);

        return new ResultadoCicloFacturacionElectronica(
            true,
            procesados,
            $"Ciclo finalizado. Recuperados: {recuperados}; " +
            $"procesados: {procesados}.");
    }
}
