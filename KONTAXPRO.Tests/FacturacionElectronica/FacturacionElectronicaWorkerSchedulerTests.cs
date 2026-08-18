using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using Microsoft.Extensions.Logging.Abstractions;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class FacturacionElectronicaWorkerSchedulerTests
{
    [Fact]
    public async Task Servidor_inicia_worker_y_lo_detiene_de_forma_ordenada()
    {
        var worker = new WorkerFake();
        var scheduler = Crear(worker, "SERVIDOR");

        await scheduler.IniciarAsync();
        await worker.PrimerCiclo.Task.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.True(scheduler.EstaEjecutandose);
        Assert.Equal(1, worker.Ciclos);
        await scheduler.DetenerAsync();
        Assert.False(scheduler.EstaEjecutandose);
    }

    [Fact]
    public async Task Cliente_no_inicia_worker_local()
    {
        var worker = new WorkerFake();
        var scheduler = Crear(worker, "CLIENTE");

        await scheduler.IniciarAsync();

        Assert.False(scheduler.EstaEjecutandose);
        Assert.Equal(0, worker.Ciclos);
    }

    private static FacturacionElectronicaWorkerScheduler Crear(
        WorkerFake worker,
        string tipo) => new(
        worker,
        new ContextFake(tipo),
        new FacturacionElectronicaWorkerOptions
        {
            Intervalo = TimeSpan.FromMinutes(1)
        },
        NullLogger<FacturacionElectronicaWorkerScheduler>.Instance);

    private sealed class WorkerFake : IWorkerFacturacionElectronica
    {
        public int Ciclos { get; private set; }
        public TaskCompletionSource PrimerCiclo { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ResultadoCicloFacturacionElectronica> EjecutarCicloAsync(
            CancellationToken cancellationToken = default)
        {
            Ciclos++;
            PrimerCiclo.TrySetResult();
            return Task.FromResult(new ResultadoCicloFacturacionElectronica(
                true, 0, "OK"));
        }
    }

    private sealed class ContextFake(string tipo) : IContextoInstalacion
    {
        public Guid InstalacionId { get; } = Guid.NewGuid();
        public string TipoInstalacion => tipo;
        public string DirectorioBase => Path.GetTempPath();
    }
}
