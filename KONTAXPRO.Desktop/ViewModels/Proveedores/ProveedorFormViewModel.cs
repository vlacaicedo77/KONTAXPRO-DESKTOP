using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels.Proveedores;

public partial class ProveedorFormViewModel : ObservableObject
{
    private const int MaxOfficialQueryAttempts = 3;
    private readonly IProveedorService _service;
    private readonly IConsultaIdentificacionService _identificationService;
    private readonly CurrentSession _session;
    private readonly IMessageDialogService _dialogs;
    private CancellationTokenSource? _verificationCancellation;
    private long _verificationSequence;
    private bool _initializing;
    private Guid? _constanciaVerificacionId;
    private uint? _version;

    public event Action? CloseRequested;
    public event Action<long>? Saved;
    public event Action? FocusRucRequested;
    public event Action? FocusBusinessNameRequested;
    public event Action? FocusAddressRequested;
    public event Action? ConcurrencyConflictDetected;

    [ObservableProperty] private long? terceroId;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isVerifying;
    [ObservableProperty] private bool isOfflineMode;
    [ObservableProperty] private bool isVerified;
    [ObservableProperty] private bool hasVerificationError;
    [ObservableProperty] private int verificationAttempt;
    [ObservableProperty] private string numeroRuc = string.Empty;
    [ObservableProperty] private string razonSocial = string.Empty;
    [ObservableProperty] private string correo = string.Empty;
    [ObservableProperty] private string telefono = string.Empty;
    [ObservableProperty] private string direccion = string.Empty;
    [ObservableProperty] private int estado = 1;
    [ObservableProperty] private string? fuenteVerificacion;
    [ObservableProperty] private string verificationTitle = "RUC pendiente";
    [ObservableProperty] private string verificationMessage =
        "Ingresa un RUC de 13 dígitos para buscarlo localmente y verificarlo.";

    public string FormTitle => IsEditing ? "Editar proveedor" : "Nuevo proveedor";
    public string FormSubtitle => IsEditing
        ? "Actualiza la identificación y los datos de contacto del proveedor."
        : "Registra o reutiliza un contribuyente mediante su RUC.";
    public string SaveText => IsEditing ? "Guardar cambios" : "Guardar proveedor";
    public bool CanEditRuc => !IsEditing && !IsBusy;
    public bool CanEditBusinessName =>
        !IsBusy && (IsOfflineMode || (IsEditing && !IsVerified));
    public bool CanVerify => !IsBusy && !IsVerifying &&
        IdentificacionEcuadorValidator.Validate("RUC", NumeroRuc).IsValid;
    public bool IsPrepared => _session.IsAuthenticated;
    public bool PuedeGestionarTerceros =>
        _session.HasPermission(TercerosPermissions.Gestionar);
    public bool ShowVerificationSource =>
        IsVerified && FuenteVerificacion is "GUIA" or "SIFAE";
    public string VerificationSourceBadge => FuenteVerificacion switch
    {
        "GUIA" => "G",
        "SIFAE" => "F",
        _ => string.Empty
    };

    public ProveedorFormViewModel(
        IProveedorService service,
        IConsultaIdentificacionService identificationService,
        CurrentSession session,
        IMessageDialogService dialogs)
    {
        _service = service;
        _identificationService = identificationService;
        _session = session;
        _dialogs = dialogs;
    }

    public Task NuevoAsync()
    {
        Reset();
        GuardarCommand.NotifyCanExecuteChanged();
        if (IsPrepared)
            FocusRucRequested?.Invoke();
        return Task.CompletedTask;
    }

    public async Task EditarAsync(long thirdPartyId)
    {
        Reset();
        GuardarCommand.NotifyCanExecuteChanged();
        if (!IsPrepared)
            return;
        IsBusy = true;
        try
        {
            var detail = await _service.ObtenerProveedorAsync(thirdPartyId);
            if (detail is null)
            {
                await _dialogs.ShowErrorAsync(
                    "Proveedor no disponible",
                    "El proveedor no existe o ya no está disponible.");
                return;
            }
            LoadDetail(detail, true);
        }
        finally
        {
            IsBusy = false;
            NotifyDerived();
        }
    }

    [RelayCommand]
    private void LimpiarRuc()
    {
        if (!CanEditRuc) return;
        NumeroRuc = string.Empty;
        FocusRucRequested?.Invoke();
    }

    [RelayCommand]
    private async Task VerificarAsync()
    {
        CancelVerification();
        _verificationCancellation = new CancellationTokenSource();
        await VerifyCoreAsync(true, _verificationCancellation.Token);
    }

    [RelayCommand]
    private void CancelarVerificacion()
    {
        CancelVerification();
        VerificationTitle = "Consulta cancelada";
        VerificationMessage =
            "La consulta oficial fue cancelada. Puedes volver a verificar el RUC.";
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task GuardarAsync()
    {
        if (IsBusy) return;
        if (!IsPrepared)
        {
            await _dialogs.ShowWarningAsync(
                "Sesión no disponible",
                "Vuelve a iniciar sesión antes de guardar el proveedor.");
            return;
        }
        var validation = IdentificacionEcuadorValidator.Validate("RUC", NumeroRuc);
        if (!validation.IsValid)
        {
            await _dialogs.ShowWarningAsync("Revisa el RUC", validation.Error!);
            FocusRucRequested?.Invoke();
            return;
        }
        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            await _dialogs.ShowWarningAsync(
                "Falta la razón social",
                "Ingresa la razón social del proveedor.");
            FocusBusinessNameRequested?.Invoke();
            return;
        }
        if (!IsVerified && !IsOfflineMode && !IsEditing)
        {
            await _dialogs.ShowWarningAsync(
                "Verificación pendiente",
                "Verifica el RUC. El ingreso manual se habilita únicamente cuando las fuentes oficiales no están disponibles.");
            return;
        }
        IsBusy = true;
        try
        {
            var result = await _service.GuardarAsync(new ProveedorGuardarRequest
            {
                TerceroId = TerceroId,
                Version = _version,
                Ruc = validation.Normalized,
                RazonSocial = RazonSocial,
                Direccion = Direccion,
                Correo = Correo,
                Telefono = Telefono,
                ConstanciaVerificacionId = _constanciaVerificacionId,
                Estado = Estado
            });
            if (!result.Success)
            {
                if (result.ConcurrencyConflict)
                    ConcurrencyConflictDetected?.Invoke();
                await _dialogs.ShowErrorAsync("No se pudo guardar", result.Message);
                return;
            }
            Saved?.Invoke(result.TerceroId!.Value);
            Reset();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        if (HasData() && !await _dialogs.ConfirmAsync(
                "Cerrar formulario",
                "Los cambios no guardados se perderán.",
                "Cerrar",
                "Continuar editando"))
            return;
        Close();
    }

    partial void OnNumeroRucChanged(string value)
    {
        NotifyDerived();
        if (_initializing || IsEditing) return;
        CancelVerification();
        TerceroId = null;
        _version = null;
        _constanciaVerificacionId = null;
        IsVerified = false;
        IsOfflineMode = false;
        HasVerificationError = false;
        FuenteVerificacion = null;
        VerificationAttempt = 0;
        VerificationTitle = "RUC pendiente";
        VerificationMessage = "Ingresa un RUC de 13 dígitos para verificarlo.";
        var validation = IdentificacionEcuadorValidator.Validate("RUC", value);
        if (validation.IsValid)
            _ = VerifyAfterDelayAsync();
        else if (value.Length >= 13)
        {
            HasVerificationError = true;
            VerificationTitle = "RUC inválido";
            VerificationMessage = validation.Error ?? "Revisa el número ingresado.";
        }
    }

    partial void OnIsEditingChanged(bool value) => NotifyDerived();
    partial void OnIsBusyChanged(bool value) => NotifyDerived();
    partial void OnIsVerifyingChanged(bool value) => NotifyDerived();
    partial void OnIsOfflineModeChanged(bool value) => NotifyDerived();
    partial void OnIsVerifiedChanged(bool value)
    {
        NotifyDerived();
        OnPropertyChanged(nameof(ShowVerificationSource));
    }
    partial void OnFuenteVerificacionChanged(string? value)
    {
        OnPropertyChanged(nameof(ShowVerificationSource));
        OnPropertyChanged(nameof(VerificationSourceBadge));
    }

    private async Task VerifyAfterDelayAsync()
    {
        _verificationCancellation = new CancellationTokenSource();
        try
        {
            await Task.Delay(350, _verificationCancellation.Token);
            await VerifyCoreAsync(false, _verificationCancellation.Token);
        }
        catch (OperationCanceledException) { }
    }

    private async Task VerifyCoreAsync(
        bool userInitiated,
        CancellationToken cancellationToken = default)
    {
        if (IsVerifying || IsBusy) return;
        var validation = IdentificacionEcuadorValidator.Validate("RUC", NumeroRuc);
        if (!validation.IsValid)
        {
            HasVerificationError = true;
            VerificationTitle = "RUC inválido";
            VerificationMessage = validation.Error!;
            if (userInitiated)
                await _dialogs.ShowWarningAsync("RUC inválido", validation.Error!);
            return;
        }

        var token = cancellationToken;
        var sequence = Interlocked.Increment(ref _verificationSequence);
        var queriedRuc = validation.Normalized;
        var verificationFlowId = Guid.NewGuid();
        _constanciaVerificacionId = null;
        IsVerifying = true;
        HasVerificationError = false;
        try
        {
            var local = await _service.BuscarPorRucAsync(queriedRuc, token);
            if (!IsCurrent(sequence, queriedRuc)) return;
            if (local is not null)
            {
                LoadDetail(local, local.EsProveedor);
                if (local.Verificado)
                {
                    VerificationTitle = "Verificado con SRI";
                    VerificationMessage = local.EsProveedor
                        ? "Este proveedor ya está registrado."
                        : "La identidad ya existe y será reutilizada al guardar el proveedor.";
                    return;
                }
                VerificationTitle = local.EsProveedor
                    ? "Proveedor existente"
                    : "Contribuyente existente";
                VerificationMessage = local.EsProveedor
                    ? "El proveedor ya existe y sus datos pueden actualizarse."
                    : "La identidad ya existe y será reutilizada al guardar el proveedor.";
                IsOfflineMode = false;
            }

            Guid? offlineProofId = null;
            for (var attempt = 1; attempt <= MaxOfficialQueryAttempts; attempt++)
            {
                VerificationAttempt = attempt;
                VerificationTitle = "Consultando fuentes oficiales";
                VerificationMessage =
                    $"Intento {attempt} de {MaxOfficialQueryAttempts}.";
                var result = await _identificationService.ConsultarAsync(
                    new ConsultaIdentificacionRequest(
                        "RUC",
                        queriedRuc,
                        PropositoConsultaIdentificacion.Proveedor,
                        verificationFlowId),
                    token);
                if (!IsCurrent(sequence, queriedRuc)) return;
                if (result.Estado ==
                        EstadoConsultaIdentificacion.FuentesNoDisponibles)
                    offlineProofId = result.ConstanciaVerificacionId;
                if (result.Encontrado)
                {
                    IsVerified = true;
                    IsOfflineMode = false;
                    RazonSocial = result.RazonSocial?.Trim() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(result.Correo))
                        Correo = ContactoClienteNormalizer.FirstEmailOrNull(
                            result.Correo)?.ToLowerInvariant() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(result.Direccion))
                        Direccion = result.Direccion.Trim();
                    FuenteVerificacion = result.Fuente;
                    _constanciaVerificacionId =
                        result.ConstanciaVerificacionId;
                    VerificationTitle = "Verificado con SRI";
                    VerificationMessage = string.Empty;
                    FocusAddressRequested?.Invoke();
                    return;
                }
                if (result.Estado is EstadoConsultaIdentificacion.NoEncontrado or
                    EstadoConsultaIdentificacion.IdentificacionInvalida)
                {
                    HasVerificationError = true;
                    IsOfflineMode = false;
                    VerificationTitle = "RUC no válido";
                    VerificationMessage = result.MensajeUsuario;
                    await _dialogs.ShowWarningAsync(
                        "No se pudo validar el RUC",
                        result.MensajeUsuario);
                    return;
                }
                if (attempt < MaxOfficialQueryAttempts)
                    await Task.Delay(500, token);
            }

            if (!offlineProofId.HasValue)
            {
                HasVerificationError = true;
                IsOfflineMode = false;
                VerificationTitle = "No se pudo autorizar el registro";
                VerificationMessage =
                    "Las fuentes no confirmaron el RUC y tampoco se pudo autorizar el modo offline.";
                return;
            }

            _constanciaVerificacionId = offlineProofId;
            IsOfflineMode = true;
            IsVerified = false;
            FuenteVerificacion = "MANUAL";
            VerificationTitle = "Registro manual habilitado";
            VerificationMessage =
                "Los servicios oficiales no se encuentran disponibles después de 3 intentos. El proveedor se registrará sin verificación oficial; ingresa manualmente la razón social.";
            FocusBusinessNameRequested?.Invoke();
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (sequence == Volatile.Read(ref _verificationSequence))
                IsVerifying = false;
        }
    }

    private bool IsCurrent(long sequence, string ruc) =>
        sequence == Volatile.Read(ref _verificationSequence) &&
        IdentificacionEcuadorValidator.Normalize(NumeroRuc, "RUC") == ruc &&
        IsPrepared;

    private void LoadDetail(ProveedorDetalleDto detail, bool editing)
    {
        _initializing = true;
        try
        {
            _constanciaVerificacionId = null;
            TerceroId = detail.TerceroId;
            _version = detail.Version;
            NumeroRuc = detail.Ruc;
            RazonSocial = detail.RazonSocial;
            Direccion = detail.Direccion ?? string.Empty;
            Correo = detail.Correo ?? string.Empty;
            Telefono = detail.Telefono ?? string.Empty;
            IsVerified = detail.Verificado;
            FuenteVerificacion = detail.FuenteVerificacion;
            Estado = detail.Estado;
            IsEditing = editing;
            IsOfflineMode = !detail.Verificado;
            VerificationTitle = detail.Verificado
                ? "Verificado con SRI"
                : "RUC no verificado";
            VerificationMessage = detail.Verificado
                ? string.Empty
                : "Puedes verificar nuevamente o mantener los datos manuales.";
        }
        finally
        {
            _initializing = false;
            NotifyDerived();
        }
    }

    private void Close()
    {
        CancelVerification();
        Reset();
        CloseRequested?.Invoke();
    }

    public void CloseForCompanyChange() => Close();

    private void CancelVerification()
    {
        _verificationCancellation?.Cancel();
        _verificationCancellation?.Dispose();
        _verificationCancellation = null;
        Interlocked.Increment(ref _verificationSequence);
        IsVerifying = false;
    }

    private void Reset()
    {
        CancelVerification();
        _initializing = true;
        TerceroId = null;
        _version = null;
        _constanciaVerificacionId = null;
        IsEditing = IsBusy = IsVerifying = IsOfflineMode = IsVerified =
            HasVerificationError = false;
        VerificationAttempt = 0;
        NumeroRuc = RazonSocial = Correo = Telefono = Direccion = string.Empty;
        Estado = 1;
        FuenteVerificacion = null;
        VerificationTitle = "RUC pendiente";
        VerificationMessage =
            "Ingresa un RUC de 13 dígitos para buscarlo localmente y verificarlo.";
        _initializing = false;
        NotifyDerived();
    }

    private bool HasData() => IsEditing ||
        !string.IsNullOrWhiteSpace(NumeroRuc) ||
        !string.IsNullOrWhiteSpace(RazonSocial) ||
        !string.IsNullOrWhiteSpace(Direccion) ||
        !string.IsNullOrWhiteSpace(Correo) ||
        !string.IsNullOrWhiteSpace(Telefono);

    private void NotifyDerived()
    {
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(FormSubtitle));
        OnPropertyChanged(nameof(SaveText));
        OnPropertyChanged(nameof(CanEditRuc));
        OnPropertyChanged(nameof(CanEditBusinessName));
        OnPropertyChanged(nameof(CanVerify));
        OnPropertyChanged(nameof(IsPrepared));
    }
}
