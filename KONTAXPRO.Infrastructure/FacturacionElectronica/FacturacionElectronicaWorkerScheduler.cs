using KONTAXPRO.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class FacturacionElectronicaWorkerOptions
{
    public TimeSpan Intervalo { get; init; } = TimeSpan.FromSeconds(15);
}

public sealed class FacturacionElectronicaWorkerScheduler(
    IWorkerFacturacionElectronica worker,
    IContextoInstalacion contextoInstalacion,
    FacturacionElectronicaWorkerOptions options,
    ILogger<FacturacionElectronicaWorkerScheduler> logger)
    : IFacturacionElectronicaWorkerScheduler
{
    private readonly object gate = new();
    private CancellationTokenSource? lifetime;
    private Task? loop;
    private volatile bool running;

    public bool EstaEjecutandose => running;

    public Task IniciarAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!contextoInstalacion.EsServidor) return Task.CompletedTask;
        lock (gate)
        {
            if (loop is { IsCompleted: false }) return Task.CompletedTask;
            lifetime?.Dispose();
            lifetime = new CancellationTokenSource();
            running = true;
            loop = EjecutarAsync(lifetime.Token);
        }
        return Task.CompletedTask;
    }

    public async Task DetenerAsync(CancellationToken cancellationToken = default)
    {
        Task? current;
        lock (gate)
        {
            lifetime?.Cancel();
            current = loop;
        }
        if (current is not null)
        {
            try
            {
                await current.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (
                lifetime?.IsCancellationRequested == true)
            {
            }
        }
        lock (gate)
        {
            running = false;
            loop = null;
            lifetime?.Dispose();
            lifetime = null;
        }
    }

    private async Task EjecutarAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timer = new PeriodicTimer(options.Intervalo);
            do
            {
                try
                {
                    var result = await worker.EjecutarCicloAsync(
                        cancellationToken);
                    logger.LogInformation(
                        "Ciclo automático SRI: {Mensaje}", result.Mensaje);
                }
                catch (OperationCanceledException) when (
                    cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception,
                        "El ciclo automático SRI falló y se reintentará.");
                }
            } while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (
            cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            running = false;
        }
    }
}
