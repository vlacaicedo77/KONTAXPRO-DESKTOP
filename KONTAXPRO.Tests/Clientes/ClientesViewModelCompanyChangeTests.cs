using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Clientes;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.ViewModels.Clientes;

namespace KONTAXPRO.Tests.Clientes;

public sealed class ClientesViewModelCompanyChangeTests
{
    [Fact]
    public async Task CompanyChangeReloadsCommercialDataAndKeepsGlobalSearch()
    {
        var fixture = CreateFixture();
        await fixture.ViewModel.InitializeAsync();
        fixture.ViewModel.TextoBusqueda = "cliente";
        await Task.Delay(350);

        fixture.Session.EmpresaId = 2;
        fixture.Session.NotifyEmpresaActivaChanged(1);
        await fixture.ViewModel.PendingCompanyChange;

        var item = Assert.Single(fixture.ViewModel.Clientes);
        Assert.Equal(10, item.TerceroId);
        Assert.Equal("CLIENTE GLOBAL", item.RazonSocial);
        Assert.Equal("B", item.ClasificacionPrecio);
        Assert.Equal("cliente", fixture.ViewModel.TextoBusqueda);
        Assert.Equal(1, fixture.ViewModel.PaginaActual);
        Assert.Equal(2, fixture.Service.LastRequestedCompanyId);
    }

    [Fact]
    public async Task CompanyChangeClosesOpenFormAndWarnsUser()
    {
        var fixture = CreateFixture();
        await fixture.ViewModel.NuevoClienteCommand.ExecuteAsync(null);
        Assert.True(fixture.ViewModel.IsClientFormOpen);

        fixture.Session.EmpresaId = 2;
        fixture.Session.NotifyEmpresaActivaChanged(1);
        await fixture.ViewModel.PendingCompanyChange;

        Assert.False(fixture.ViewModel.IsClientFormOpen);
        Assert.False(fixture.Form.IsPreparedForCurrentCompany);
        Assert.Equal(1, fixture.Dialog.WarningCount);
    }

    [Fact]
    public async Task OldCompanyResponseIsIgnoredAfterCompanyChange()
    {
        var fixture = CreateFixture();
        fixture.Service.DelayCompanyOne = true;
        var initialLoad = fixture.ViewModel.InitializeAsync();
        await fixture.Service.CompanyOneStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(5));

        fixture.Session.EmpresaId = 2;
        fixture.Session.NotifyEmpresaActivaChanged(1);
        await fixture.ViewModel.PendingCompanyChange;
        fixture.Service.ReleaseCompanyOne();
        await initialLoad;

        var item = Assert.Single(fixture.ViewModel.Clientes);
        Assert.Equal("B", item.ClasificacionPrecio);
        Assert.Equal(2, fixture.Service.LastRequestedCompanyId);
    }

    [Fact]
    public async Task SameCompanyAndDisposedViewModelDoNotReload()
    {
        var fixture = CreateFixture();
        await fixture.ViewModel.InitializeAsync();
        var calls = fixture.Service.CatalogCalls;

        fixture.Session.NotifyEmpresaActivaChanged(1);
        Assert.Equal(calls, fixture.Service.CatalogCalls);

        fixture.ViewModel.Dispose();
        fixture.Session.EmpresaId = 2;
        fixture.Session.NotifyEmpresaActivaChanged(1);
        await Task.Delay(50);

        Assert.Equal(calls, fixture.Service.CatalogCalls);
    }

    [Fact]
    public async Task FormCannotSaveAfterActiveCompanyChanged()
    {
        var fixture = CreateFixture();
        await fixture.Form.NuevoAsync();
        fixture.Form.NumeroIdentificacion = "AB123456";
        fixture.Form.IdentificacionPasaporte = true;
        fixture.Form.RazonSocial = "CLIENTE GLOBAL";
        fixture.Form.Direccion = "DIRECCION DE PRUEBA";

        fixture.Session.EmpresaId = 2;
        await fixture.Form.GuardarCommand.ExecuteAsync(null);

        Assert.Equal(0, fixture.Service.SaveCalls);
        Assert.Contains("empresa activa cambió", fixture.Form.MensajeFormulario);
    }

    private static Fixture CreateFixture()
    {
        var session = new CurrentSession
        {
            UsuarioId = 1,
            EmpresaId = 1,
            RazonSocial = "EMPRESA UNO",
            Permisos = [TercerosPermissions.Gestionar]
        };
        var service = new FakeClienteService();
        var dialog = new FakeDialogService();
        var form = new ClienteFormViewModel(
            service,
            new FakeIdentificationService(),
            session,
            dialog);
        var viewModel = new ClientesViewModel(
            service,
            session,
            form,
            dialog,
            new FakeNotificationService());
        return new Fixture(session, service, dialog, form, viewModel);
    }

    private sealed record Fixture(
        CurrentSession Session,
        FakeClienteService Service,
        FakeDialogService Dialog,
        ClienteFormViewModel Form,
        ClientesViewModel ViewModel);

    private sealed class FakeClienteService : IClienteService
    {
        private readonly TaskCompletionSource<bool> _releaseCompanyOne =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool DelayCompanyOne { get; set; }
        public int CatalogCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public long LastRequestedCompanyId { get; private set; }
        public TaskCompletionSource<bool> CompanyOneStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ClienteCatalogoResultadoDto> ObtenerClientesAsync(
            ClienteCatalogoQuery query,
            CancellationToken cancellationToken = default)
        {
            CatalogCalls++;
            LastRequestedCompanyId = query.EmpresaId;
            if (query.EmpresaId == 1 && DelayCompanyOne)
            {
                CompanyOneStarted.TrySetResult(true);
                await _releaseCompanyOne.Task;
            }

            var code = query.EmpresaId == 2 ? "B" : "A";
            return new ClienteCatalogoResultadoDto
            {
                Items =
                [
                    new ClienteCatalogoItemDto
                    {
                        EmpresaTerceroId = query.EmpresaId,
                        TerceroId = 10,
                        TipoIdentificacionCodigo = "CEDULA",
                        NumeroIdentificacion = "0999999999",
                        RazonSocial = "CLIENTE GLOBAL",
                        ListaPrecioCodigo = code,
                        ListaPrecioNombre = $"LISTA {code}",
                        Estado = 1
                    }
                ],
                Total = 1,
                Pagina = 1,
                TamanoPagina = query.TamanoPagina,
                Kpis = new ClienteCatalogoKpisDto { Clientes = 1 }
            };
        }

        public void ReleaseCompanyOne() => _releaseCompanyOne.TrySetResult(true);

        public Task<ClienteDetalleDto?> ObtenerClienteAsync(
            long empresaTerceroId,
            long empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ClienteDetalleDto?>(null);

        public Task<TerceroIdentificacionDto?> BuscarPorIdentificacionAsync(
            long tipoIdentificacionId,
            string numeroIdentificacion,
            long empresaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TerceroIdentificacionDto?>(null);

        public Task<IReadOnlyList<TipoIdentificacionClienteDto>>
            ObtenerTiposIdentificacionAsync(
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<TipoIdentificacionClienteDto>>(
            [
                new TipoIdentificacionClienteDto
                {
                    Id = 1,
                    Codigo = "CEDULA",
                    Nombre = "CÉDULA",
                    LongitudMinima = 10,
                    LongitudMaxima = 10
                },
                new TipoIdentificacionClienteDto
                {
                    Id = 3,
                    Codigo = "PASAPORTE",
                    Nombre = "PASAPORTE",
                    LongitudMinima = 3,
                    LongitudMaxima = 20
                }
            ]);

        public Task<IReadOnlyList<ListaPrecioClienteDto>>
            ObtenerListasPrecioAsync(
                long empresaId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ListaPrecioClienteDto>>(
            [
                new ListaPrecioClienteDto
                {
                    Id = empresaId,
                    Codigo = empresaId == 2 ? "B" : "A",
                    Nombre = "LISTA BASE",
                    EsListaBase = true
                }
            ]);

        public Task<ClienteOperationResult> GuardarAsync(
            ClienteGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(ClienteOperationResult.Ok("OK", 1, 1));
        }

        public Task<ClienteOperationResult> CambiarEstadoAsync(
            long empresaTerceroId,
            long empresaId,
            int estado,
            uint version,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ClienteOperationResult.Ok("OK", 1, 1));
    }

    private sealed class FakeIdentificationService
        : IConsultaIdentificacionService
    {
        public Task<ConsultaIdentificacionResult> ConsultarAsync(
            ConsultaIdentificacionRequest request,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("No se esperaba una consulta HTTP.");
    }

    private sealed class FakeDialogService : IMessageDialogService
    {
        public int WarningCount { get; private set; }

        public Task ShowErrorAsync(
            string title, string message, string? detail = null) =>
            Task.CompletedTask;

        public Task ShowWarningAsync(
            string title, string message, string? detail = null)
        {
            WarningCount++;
            return Task.CompletedTask;
        }

        public Task ShowInfoAsync(
            string title, string message, string? detail = null) =>
            Task.CompletedTask;

        public Task ShowSuccessAsync(
            string title, string message, string? detail = null) =>
            Task.CompletedTask;

        public Task<bool> ConfirmAsync(
            string title,
            string message,
            string primaryText = "Confirmar",
            string secondaryText = "Cancelar",
            bool isDestructive = false) => Task.FromResult(true);

        public Task<bool> ConfirmWarningAsync(
            string title,
            string message,
            string primaryText = "Continuar",
            string secondaryText = "Cancelar") => Task.FromResult(true);

        public bool Confirm(
            string title,
            string message,
            string primaryText = "Confirmar",
            string secondaryText = "Cancelar",
            bool isDestructive = false) => true;
    }

    private sealed class FakeNotificationService : INotificationService
    {
        public Task ShowSuccessAsync(string message, string? title = null) =>
            Task.CompletedTask;

        public Task ShowInfoAsync(string message, string? title = null) =>
            Task.CompletedTask;

        public Task ShowWarningAsync(string message, string? title = null) =>
            Task.CompletedTask;
    }
}
