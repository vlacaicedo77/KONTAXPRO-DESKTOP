using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.ViewModels.Proveedores;

namespace KONTAXPRO.Tests.Proveedores;

public sealed class ProveedorFormViewModelTests
{
    [Fact]
    public void SupplierViewsUseValidMaterialIcons()
    {
        string[] iconNames =
        [
            "AccountSearch", "CardAccountDetailsOutline",
            "CardAccountPhoneOutline", "ChevronLeft", "ChevronRight", "Close",
            "ContentSaveOutline", "EmailOffOutline", "LockOutline", "Magnify",
            "PencilOutline", "Plus", "Power", "Refresh", "ShieldAlert",
            "StopCircleOutline", "Truck", "TruckCheck", "TruckOutline"
        ];

        Assert.All(iconNames, name => Assert.True(
            Enum.TryParse<MaterialDesignThemes.Wpf.PackIconKind>(name, out _),
            $"El icono MaterialDesign '{name}' no existe."));
    }

    [Fact]
    public async Task VerificationUsesSupplierPurposeAndCompletesOfficialData()
    {
        var identification = new FakeIdentificationService(_ => new()
        {
            Estado = EstadoConsultaIdentificacion.Encontrado,
            NumeroNormalizado = "2459999906001",
            RazonSocial = "PROVEEDOR VERIFICADO",
            Correo = "PRIMERO@EXAMPLE.TEST,OTRO@EXAMPLE.TEST",
            Fuente = "GUIA"
        });
        var form = CreateForm(new FakeSupplierService(), identification);
        await form.NuevoAsync();
        form.NumeroRuc = "2459999906001";

        await form.VerificarCommand.ExecuteAsync(null);

        Assert.True(form.IsVerified);
        Assert.Equal("PROVEEDOR VERIFICADO", form.RazonSocial);
        Assert.Equal("primero@example.test", form.Correo);
        Assert.Equal("GUIA", form.FuenteVerificacion);
        Assert.Equal("Verificado con SRI", form.VerificationTitle);
        Assert.Equal(string.Empty, form.VerificationMessage);
        Assert.True(form.ShowVerificationSource);
        Assert.Equal("G", form.VerificationSourceBadge);
        Assert.Equal(PropositoConsultaIdentificacion.Proveedor,
            Assert.Single(identification.Requests).Proposito);
    }

    [Fact]
    public async Task ThreeUnavailableRoundsEnableManualMode()
    {
        var identification = new FakeIdentificationService(_ => new()
        {
            Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles,
            NumeroNormalizado = "2459999906001",
            MensajeUsuario = "Fuentes no disponibles."
        });
        var form = CreateForm(new FakeSupplierService(), identification);
        await form.NuevoAsync();
        form.NumeroRuc = "2459999906001";

        await form.VerificarCommand.ExecuteAsync(null);

        Assert.Equal(3, identification.Requests.Count);
        Assert.True(form.IsOfflineMode);
        Assert.False(form.IsVerified);
        Assert.True(form.CanEditBusinessName);
        Assert.Equal("MANUAL", form.FuenteVerificacion);
    }

    [Fact]
    public async Task ValidRucAutomaticallyRunsThreeAttemptsAndEnablesManualMode()
    {
        var identification = new FakeIdentificationService(_ => new()
        {
            Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles,
            NumeroNormalizado = "2459999906001",
            MensajeUsuario = "Fuentes no disponibles."
        });
        var form = CreateForm(new FakeSupplierService(), identification);
        await form.NuevoAsync();

        form.NumeroRuc = "2459999906001";
        await WaitUntilAsync(
            () => form.IsOfflineMode,
            TimeSpan.FromSeconds(5));

        Assert.Equal(3, identification.Requests.Count);
        Assert.Equal(3, form.VerificationAttempt);
        Assert.True(form.IsOfflineMode);
        Assert.False(form.IsVerified);
        Assert.Equal("MANUAL", form.FuenteVerificacion);
    }

    [Fact]
    public async Task QueryShowsCancelableStateAndDisablesVerifyAction()
    {
        var identification = new BlockingIdentificationService();
        var form = CreateForm(new FakeSupplierService(), identification);
        await form.NuevoAsync();

        form.NumeroRuc = "2459999906001";
        await WaitUntilAsync(() => form.IsVerifying, TimeSpan.FromSeconds(2));

        Assert.False(form.CanVerify);
        form.CancelarVerificacionCommand.Execute(null);
        await WaitUntilAsync(() => !form.IsVerifying, TimeSpan.FromSeconds(2));

        Assert.Equal("Consulta cancelada", form.VerificationTitle);
        Assert.Contains("cancelada", form.VerificationMessage);
        Assert.Equal(1, identification.RequestCount);
    }

    [Fact]
    public async Task ExistingVerifiedSupplierDoesNotCallOfficialSources()
    {
        var service = new FakeSupplierService
        {
            LocalResult = new ProveedorDetalleDto
            {
                TerceroId = 8,
                Ruc = "2459999906001",
                RazonSocial = "PROVEEDOR EXISTENTE",
                EsProveedor = true,
                Verificado = true
            }
        };
        var identification = new FakeIdentificationService(_ => throw new InvalidOperationException(
            "No se debía consultar una fuente oficial."));
        var form = CreateForm(service, identification);
        await form.NuevoAsync();
        form.NumeroRuc = "2459999906001";

        await form.VerificarCommand.ExecuteAsync(null);

        Assert.Equal(8, form.TerceroId);
        Assert.Empty(identification.Requests);
        Assert.Equal("PROVEEDOR EXISTENTE", form.RazonSocial);
    }

    [Fact]
    public async Task OfficialTradeNameIsPassedOnlyAsOfficialData()
    {
        var service = new FakeSupplierService();
        var identification = new FakeIdentificationService(_ => new()
        {
            Estado = EstadoConsultaIdentificacion.Encontrado,
            NumeroNormalizado = "2459999906001",
            RazonSocial = "PROVEEDOR VERIFICADO",
            NombreComercial = "MARCA OFICIAL",
            Fuente = "GUIA"
        });
        var form = CreateForm(service, identification);
        await form.NuevoAsync();
        form.NumeroRuc = "2459999906001";
        await form.VerificarCommand.ExecuteAsync(null);

        await form.GuardarCommand.ExecuteAsync(null);

        Assert.NotNull(service.LastSaveRequest);
        Assert.NotNull(service.LastSaveRequest.ConstanciaVerificacionId);
    }

    [Fact]
    public async Task InvalidRucNeverCallsOfficialSourcesOrOfflineMode()
    {
        var identification = new FakeIdentificationService(
            _ => throw new InvalidOperationException());
        var form = CreateForm(new FakeSupplierService(), identification);
        await form.NuevoAsync();
        form.NumeroRuc = "123";

        await form.VerificarCommand.ExecuteAsync(null);

        Assert.Empty(identification.Requests);
        Assert.False(form.IsOfflineMode);
        Assert.True(form.HasVerificationError);
    }

    private static ProveedorFormViewModel CreateForm(
        IProveedorService service,
        IConsultaIdentificacionService identification) => new(
            service,
            identification,
            new CurrentSession
            {
                UsuarioId = 1,
                EmpresaId = 1,
                RazonSocial = "EMPRESA PRUEBA",
                Permisos = [TercerosPermissions.Gestionar]
            },
            new FakeDialogService());

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!condition() && DateTime.UtcNow < deadline)
            await Task.Delay(25);
        Assert.True(condition(), "La verificación automática no terminó a tiempo.");
    }

    private sealed class FakeIdentificationService(
        Func<ConsultaIdentificacionRequest, ConsultaIdentificacionResult> result)
        : IConsultaIdentificacionService
    {
        public List<ConsultaIdentificacionRequest> Requests { get; } = [];

        public Task<ConsultaIdentificacionResult> ConsultarAsync(
            ConsultaIdentificacionRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            var response = result(request);
            if (response.ConstanciaVerificacionId is null &&
                (response.Encontrado ||
                 (response.Estado ==
                      EstadoConsultaIdentificacion.FuentesNoDisponibles &&
                  Requests.Count == 3)))
            {
                response = response with
                {
                    ConstanciaVerificacionId = Guid.Parse(
                        "55555555-5555-5555-5555-555555555555")
                };
            }
            return Task.FromResult(response);
        }
    }

    private sealed class BlockingIdentificationService
        : IConsultaIdentificacionService
    {
        public int RequestCount { get; private set; }

        public async Task<ConsultaIdentificacionResult> ConsultarAsync(
            ConsultaIdentificacionRequest request,
            CancellationToken cancellationToken = default)
        {
            RequestCount++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("La consulta debió cancelarse.");
        }
    }

    private sealed class FakeSupplierService : IProveedorService
    {
        public ProveedorDetalleDto? LocalResult { get; init; }
        public ProveedorGuardarRequest? LastSaveRequest { get; private set; }
        public Task<ProveedorCatalogoResultadoDto> ObtenerProveedoresAsync(
            ProveedorCatalogoQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new ProveedorCatalogoResultadoDto());
        public Task<ProveedorDetalleDto?> ObtenerProveedorAsync(
            long terceroId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LocalResult);
        public Task<ProveedorDetalleDto?> BuscarPorRucAsync(
            string ruc,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(LocalResult);
        public Task<ProveedorOperationResult> GuardarAsync(
            ProveedorGuardarRequest request,
            CancellationToken cancellationToken = default)
        {
            LastSaveRequest = request;
            return Task.FromResult(ProveedorOperationResult.Ok("OK", 1));
        }
        public Task<ProveedorOperationResult> CambiarEstadoAsync(
            long terceroId,
            int estado,
            uint version,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ProveedorOperationResult.Ok("OK", terceroId));
    }

    private sealed class FakeDialogService : IMessageDialogService
    {
        public Task ShowErrorAsync(string title, string message, string? detail = null) =>
            Task.CompletedTask;
        public Task ShowWarningAsync(string title, string message, string? detail = null) =>
            Task.CompletedTask;
        public Task ShowInfoAsync(string title, string message, string? detail = null) =>
            Task.CompletedTask;
        public Task ShowSuccessAsync(string title, string message, string? detail = null) =>
            Task.CompletedTask;
        public Task<bool> ConfirmAsync(string title, string message,
            string primaryText = "Confirmar", string secondaryText = "Cancelar",
            bool isDestructive = false) => Task.FromResult(true);
        public Task<bool> ConfirmWarningAsync(string title, string message,
            string primaryText = "Continuar", string secondaryText = "Cancelar") =>
            Task.FromResult(true);
        public bool Confirm(string title, string message,
            string primaryText = "Confirmar", string secondaryText = "Cancelar",
            bool isDestructive = false) => true;
    }
}
