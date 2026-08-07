using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Clientes;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels.Clientes;

public enum TipoIdentificacionClienteModo
{
    Nacional,
    Pasaporte,
    Extranjero
}

public enum ClienteFormFocusTarget
{
    Names,
    Address
}

public partial class ClienteFormViewModel : ObservableObject
{
    private const int MaxOfficialQueryAttempts = 3;
    private readonly IClienteService _clienteService;
    private readonly IConsultaIdentificacionService _consultaService;
    private readonly CurrentSession _currentSession;
    private readonly IMessageDialogService _messageDialogService;
    private CancellationTokenSource? _queryCancellation;
    private long? _expectedCompanyId;
    private Guid? _constanciaVerificacionId;
    private uint? _version;
    private TipoIdentificacionClienteModo _identificacionModo =
        TipoIdentificacionClienteModo.Nacional;

    public ObservableCollection<TipoIdentificacionClienteDto>
        TiposIdentificacion { get; } = [];
    public ObservableCollection<ListaPrecioClienteDto> ListasPrecio { get; } = [];

    public event Action? CloseRequested;
    public event Action<long>? Saved;
    public event Action<long>? ExistingClientRequested;
    public event Action<ClienteFormFocusTarget>? FocusRequested;
    public event Action? ConcurrencyConflictDetected;

    [ObservableProperty] private long? empresaTerceroId;
    [ObservableProperty] private long? terceroId;
    [ObservableProperty] private bool isEditing;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isQuerying;
    [ObservableProperty] private bool modoOfflineHabilitado;
    [ObservableProperty] private bool identificacionRechazada;
    [ObservableProperty] private int intentoConsultaActual;
    [ObservableProperty] private TipoIdentificacionClienteDto? tipoIdentificacion;
    [ObservableProperty] private string numeroIdentificacion = string.Empty;
    [ObservableProperty] private string razonSocial = string.Empty;
    [ObservableProperty] private string nombreComercial = string.Empty;
    [ObservableProperty] private string correo = string.Empty;
    [ObservableProperty] private string telefono = string.Empty;
    [ObservableProperty] private string direccion = string.Empty;
    [ObservableProperty] private string observacion = string.Empty;
    [ObservableProperty] private ListaPrecioClienteDto? listaPrecio;
    [ObservableProperty] private bool creditoHabilitado = true;
    [ObservableProperty] private bool activo = true;
    [ObservableProperty] private EstadoVerificacionCliente verificacion =
        EstadoVerificacionCliente.NoVerificado;
    [ObservableProperty] private string? fuenteVerificacion;
    [ObservableProperty] private string? mensajeFormulario;
    [ObservableProperty] private string? mensajeIdentificacion;
    [ObservableProperty] private long? clienteExistenteId;
    [ObservableProperty] private bool terceroExistenteSinCliente;

    public bool IdentificacionNacional
    {
        get => _identificacionModo == TipoIdentificacionClienteModo.Nacional;
        set
        {
            if (value)
                SetIdentificationMode(TipoIdentificacionClienteModo.Nacional);
        }
    }
    public bool PuedeGestionarTerceros =>
        _currentSession.HasPermission(TercerosPermissions.Gestionar);

    public bool IdentificacionPasaporte
    {
        get => _identificacionModo == TipoIdentificacionClienteModo.Pasaporte;
        set
        {
            if (value)
                SetIdentificationMode(TipoIdentificacionClienteModo.Pasaporte);
        }
    }

    public bool IdentificacionExtranjero
    {
        get => _identificacionModo == TipoIdentificacionClienteModo.Extranjero;
        set
        {
            if (value)
                SetIdentificationMode(TipoIdentificacionClienteModo.Extranjero);
        }
    }

    public string FormTitle => IsEditing ? "Editar cliente" : "Nuevo cliente";
    public string FormSubtitle => IsEditing
        ? "Actualiza los datos de contacto y las condiciones comerciales de este cliente."
        : "Completa la identificación, los datos de contacto y las condiciones comerciales del nuevo cliente.";
    public string PrimaryFieldLabel => TipoIdentificacion?.Codigo switch
    {
        "CEDULA" => "Apellidos y nombres",
        "RUC" => "Razón social",
        _ => "Nombres o razón social"
    };
    public bool NombrePrincipalEditable =>
        !IdentificacionNacional || ModoOfflineHabilitado;
    public bool MostrarFuenteVerificacion =>
        Verificacion == EstadoVerificacionCliente.Verificado &&
        FuenteVerificacion is "GUIA" or "SIFAE";
    public string FuenteVerificacionInicial => FuenteVerificacion switch
    {
        "GUIA" => "G",
        "SIFAE" => "F",
        _ => string.Empty
    };
    public string TipoNacionalDetectadoTexto => TipoIdentificacion?.Codigo switch
    {
        "CEDULA" => "Cédula detectada · 10 dígitos",
        "RUC" => "RUC detectado · 13 dígitos",
        _ => "Ingresa 10 dígitos para cédula o 13 para RUC"
    };
    public string EmpresaListaPrecioTexto =>
        $"Lista de precios en: {_currentSession.RazonSocial ?? "empresa activa"}";
    public string VerificationActionText =>
        IsEditing ? "Verificar ahora" : "Verificar";
    public bool PuedeConsultar =>
        !IsBusy &&
        !IsQuerying &&
        IdentificacionNacional &&
        (!IsEditing ||
         Verificacion != EstadoVerificacionCliente.Verificado) &&
        TipoIdentificacion?.PermiteConsulta == true &&
        IdentificacionEcuadorValidator.Validate(
            TipoIdentificacion.Codigo,
            NumeroIdentificacion).IsValid;
    public bool IdentificacionEditable => !IsEditing && !IsBusy;
    public bool HayClienteExistente => ClienteExistenteId.HasValue;
    public bool IsPreparedForCurrentCompany =>
        _expectedCompanyId is > 0 &&
        _expectedCompanyId.Value == CurrentCompanyId();
    public string VerificacionTexto => Verificacion switch
    {
        _ when IdentificacionRechazada && TipoIdentificacion?.Codigo == "RUC" =>
            "RUC no válido",
        _ when IdentificacionRechazada => "Cédula no válida",
        EstadoVerificacionCliente.Verificado when
            TipoIdentificacion?.Codigo == "RUC" => "Verificado con SRI",
        EstadoVerificacionCliente.Verificado when
            TipoIdentificacion?.Codigo == "CEDULA" =>
            "Verificado con Registro Civil",
        EstadoVerificacionCliente.Verificado => "Verificado",
        EstadoVerificacionCliente.NoAplica => "Verificación no aplicable",
        _ when IsQuerying =>
            $"Consultando servicios oficiales · intento {IntentoConsultaActual} de {MaxOfficialQueryAttempts}",
        _ when ModoOfflineHabilitado => "Registro sin verificación oficial",
        _ when IdentificacionNacional => "Pendiente de verificación oficial",
        _ => "Ingreso manual · no verificado"
    };

    public ClienteFormViewModel(
        IClienteService clienteService,
        IConsultaIdentificacionService consultaService,
        CurrentSession currentSession,
        IMessageDialogService messageDialogService)
    {
        _clienteService = clienteService;
        _consultaService = consultaService;
        _currentSession = currentSession;
        _messageDialogService = messageDialogService;
    }

    public async Task NuevoAsync()
    {
        Reset();
        GuardarCommand.NotifyCanExecuteChanged();
        _expectedCompanyId = CurrentCompanyId();
        await LoadCatalogsAsync(_expectedCompanyId.Value);
        IsEditing = false;
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(FormSubtitle));
        OnPropertyChanged(nameof(EmpresaListaPrecioTexto));
    }

    public async Task EditarAsync(long id)
    {
        Reset();
        GuardarCommand.NotifyCanExecuteChanged();
        _expectedCompanyId = CurrentCompanyId();
        IsBusy = true;
        try
        {
            await LoadCatalogsAsync(_expectedCompanyId.Value);
            var detail = await _clienteService.ObtenerClienteAsync(
                id,
                _expectedCompanyId.Value);
            if (detail is null)
            {
                MensajeFormulario = "El cliente no fue encontrado.";
                return;
            }

            IsEditing = true;
            EmpresaTerceroId = detail.EmpresaTerceroId;
            TerceroId = detail.TerceroId;
            _version = detail.Version;
            SetIdentificationMode(
                ModeFromType(detail.TipoIdentificacionCodigo),
                clearIdentification: false);
            TipoIdentificacion = TiposIdentificacion.FirstOrDefault(
                x => x.Id == detail.TipoIdentificacionId);
            NumeroIdentificacion = detail.NumeroIdentificacion;
            RazonSocial = detail.RazonSocial;
            NombreComercial = detail.NombreComercial ?? string.Empty;
            Correo = string.IsNullOrWhiteSpace(detail.Correo)
                ? ContactoClienteNormalizer.DefaultEmail
                : detail.Correo;
            Telefono = detail.Telefono ?? string.Empty;
            Direccion = detail.Direccion ?? string.Empty;
            Observacion = detail.Observacion ?? string.Empty;
            ListaPrecio = ListasPrecio.FirstOrDefault(
                x => x.Id == detail.ListaPrecioId) ??
                ListasPrecio.FirstOrDefault(x => x.EsListaBase);
            CreditoHabilitado = detail.CreditoHabilitado;
            Activo = detail.Estado == 1;
            Verificacion = detail.Verificacion;
            FuenteVerificacion = detail.FuenteVerificacion;
            ModoOfflineHabilitado =
                IdentificacionNacional &&
                detail.Verificacion != EstadoVerificacionCliente.Verificado;
            NotifyDerivedProperties();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeConsultar))]
    private async Task ConsultarIdentificacionAsync()
    {
        CancelIdentificationQuery();
        _queryCancellation = new CancellationTokenSource();
        await ConsultarIdentificacionCoreAsync(_queryCancellation.Token);
    }

    [RelayCommand]
    private void CancelarConsulta()
    {
        CancelIdentificationQuery();
        MensajeIdentificacion = "Consulta cancelada.";
    }

    [RelayCommand]
    private void AbrirClienteExistente()
    {
        if (ClienteExistenteId.HasValue)
            ExistingClientRequested?.Invoke(ClienteExistenteId.Value);
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task GuardarAsync()
    {
        MensajeFormulario = null;
        if (!_expectedCompanyId.HasValue ||
            _expectedCompanyId.Value != CurrentCompanyId())
        {
            MensajeFormulario =
                "La empresa activa cambió. Cierra este formulario y vuelve a abrirlo antes de guardar.";
            return;
        }
        if (TipoIdentificacion is null)
        {
            MensajeFormulario = "Selecciona el tipo de identificación.";
            return;
        }
        if (HayClienteExistente && !IsEditing)
        {
            MensajeFormulario =
                "Este cliente ya existe. Utiliza “Abrir cliente” para editarlo.";
            return;
        }

        var validation = IdentificacionEcuadorValidator.Validate(
            TipoIdentificacion.Codigo,
            NumeroIdentificacion);
        if (!validation.IsValid)
        {
            IdentificacionRechazada = IdentificacionNacional;
            MensajeIdentificacion = validation.Error;
            MensajeFormulario = validation.Error;
            return;
        }
        if (IdentificacionNacional &&
            Verificacion != EstadoVerificacionCliente.Verificado &&
            !ModoOfflineHabilitado)
        {
            MensajeFormulario =
                "La identificación nacional debe verificarse antes de guardar. El modo offline se habilita automáticamente si fallan los tres intentos oficiales.";
            return;
        }
        if (string.IsNullOrWhiteSpace(RazonSocial))
        {
            MensajeFormulario = $"Ingresa {PrimaryFieldLabel.ToLowerInvariant()}.";
            return;
        }
        if (string.IsNullOrWhiteSpace(Direccion))
        {
            MensajeFormulario = "Ingresa la dirección del cliente para la facturación.";
            return;
        }
        if (ListaPrecio is null)
        {
            MensajeFormulario =
                "Selecciona una lista de precios de la empresa activa.";
            return;
        }
        var email = ContactoClienteNormalizer.NormalizeEmail(Correo);
        if (!email.IsValid)
        {
            MensajeFormulario = email.Error;
            return;
        }
        var phone = ContactoClienteNormalizer.NormalizePhone(Telefono);
        if (!phone.IsValid)
        {
            MensajeFormulario = phone.Error;
            return;
        }

        IsBusy = true;
        try
        {
            var result = await _clienteService.GuardarAsync(
                new ClienteGuardarRequest
                {
                    EmpresaId = _expectedCompanyId.Value,
                    EmpresaTerceroId = EmpresaTerceroId,
                    TerceroId = TerceroId,
                    Version = _version,
                    TipoIdentificacionId = TipoIdentificacion.Id,
                    NumeroIdentificacion = validation.Normalized,
                    RazonSocial = RazonSocial,
                    NombreComercial = NombreComercial,
                    Correo = email.Value,
                    Telefono = Telefono,
                    Direccion = Direccion,
                    Observacion = Observacion,
                    ListaPrecioId = ListaPrecio.Id,
                    CreditoHabilitado = CreditoHabilitado,
                    Estado = Activo ? 1 : 0,
                    ConstanciaVerificacionId = _constanciaVerificacionId
                });
            if (!result.Success || !result.TerceroId.HasValue)
            {
                MensajeFormulario = result.Message;
                if (result.ConcurrencyConflict)
                    ConcurrencyConflictDetected?.Invoke();
                return;
            }

            Saved?.Invoke(result.TerceroId.Value);
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception.GetType().Name);
            MensajeFormulario =
                "No fue posible guardar el cliente. Intenta nuevamente.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        _queryCancellation?.Cancel();
        if (!HasEnteredData())
        {
            CloseRequested?.Invoke();
            return;
        }

        var close = await _messageDialogService.ConfirmWarningAsync(
            "Cerrar formulario",
            "Los cambios que no hayas guardado se perderán.",
            "Cerrar",
            "Continuar editando");
        if (close)
            CloseRequested?.Invoke();
    }

    partial void OnTipoIdentificacionChanged(TipoIdentificacionClienteDto? value)
    {
        OnPropertyChanged(nameof(PrimaryFieldLabel));
        OnPropertyChanged(nameof(TipoNacionalDetectadoTexto));
        NotifyDerivedProperties();
    }

    partial void OnNumeroIdentificacionChanged(string value)
    {
        CancelIdentificationQuery();
        _constanciaVerificacionId = null;
        ClienteExistenteId = null;
        TerceroExistenteSinCliente = false;
        if (IsEditing || !IdentificacionNacional)
            return;

        EmpresaTerceroId = null;
        TerceroId = null;
        _version = null;
        ModoOfflineHabilitado = false;
        IdentificacionRechazada = false;
        Verificacion = EstadoVerificacionCliente.NoVerificado;
        FuenteVerificacion = null;
        RazonSocial = string.Empty;
        MensajeIdentificacion = null;
        var detectedType = IdentificacionEcuadorValidator.DetectNationalType(value);
        TipoIdentificacion = TiposIdentificacion.FirstOrDefault(
            x => x.Codigo == detectedType) ??
            TiposIdentificacion.FirstOrDefault(x => x.Codigo == "CEDULA");
        NotifyDerivedProperties();

        if (detectedType is null)
            return;

        var validation = IdentificacionEcuadorValidator.Validate(detectedType, value);
        if (!validation.IsValid)
        {
            IdentificacionRechazada = true;
            MensajeIdentificacion = validation.Error;
            return;
        }

        ScheduleAutomaticQuery(value, detectedType);
    }

    partial void OnIsEditingChanged(bool value) => NotifyDerivedProperties();
    partial void OnIsBusyChanged(bool value) => NotifyDerivedProperties();
    partial void OnIsQueryingChanged(bool value) => NotifyDerivedProperties();
    partial void OnVerificacionChanged(EstadoVerificacionCliente value)
    {
        NotifyVerificationProperties();
        OnPropertyChanged(nameof(PuedeConsultar));
        ConsultarIdentificacionCommand.NotifyCanExecuteChanged();
    }
    partial void OnFuenteVerificacionChanged(string? value) =>
        NotifyVerificationProperties();
    partial void OnModoOfflineHabilitadoChanged(bool value) =>
        NotifyDerivedProperties();
    partial void OnIdentificacionRechazadaChanged(bool value) =>
        NotifyDerivedProperties();
    partial void OnIntentoConsultaActualChanged(int value) =>
        OnPropertyChanged(nameof(VerificacionTexto));
    partial void OnClienteExistenteIdChanged(long? value) =>
        OnPropertyChanged(nameof(HayClienteExistente));
    partial void OnCorreoChanged(string value)
    {
        var normalized = (value ?? string.Empty).ToLowerInvariant();
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
            Correo = normalized;
    }

    private void ScheduleAutomaticQuery(string number, string typeCode)
    {
        _queryCancellation = new CancellationTokenSource();
        var token = _queryCancellation.Token;
        _ = RunAutomaticQueryAsync(number, typeCode, token);
    }

    private async Task RunAutomaticQueryAsync(
        string number,
        string typeCode,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(350, cancellationToken);
            if (!IdentificacionNacional ||
                IdentificacionEcuadorValidator.DetectNationalType(
                    NumeroIdentificacion) != typeCode ||
                !string.Equals(
                    IdentificacionEcuadorValidator.Normalize(number, typeCode),
                    IdentificacionEcuadorValidator.Normalize(
                        NumeroIdentificacion,
                        typeCode),
                    StringComparison.Ordinal))
                return;

            await ConsultarIdentificacionCoreAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ConsultarIdentificacionCoreAsync(
        CancellationToken cancellationToken)
    {
        if (TipoIdentificacion is null || !IdentificacionNacional)
            return;

        MensajeIdentificacion = null;
        ClienteExistenteId = null;
        TerceroExistenteSinCliente = false;
        ModoOfflineHabilitado = false;
        IdentificacionRechazada = false;
        var validation = IdentificacionEcuadorValidator.Validate(
            TipoIdentificacion.Codigo,
            NumeroIdentificacion);
        if (!validation.IsValid)
        {
            IdentificacionRechazada = true;
            MensajeIdentificacion = validation.Error;
            return;
        }

        NumeroIdentificacion = validation.Normalized;
        var queriedTypeId = TipoIdentificacion.Id;
        var queriedNumber = validation.Normalized;
        IsQuerying = true;
        try
        {
            if (!IsEditing)
            {
                var local = await _clienteService.BuscarPorIdentificacionAsync(
                    queriedTypeId,
                    queriedNumber,
                    ExpectedCompanyId(),
                    cancellationToken);
                if (!IsCurrentIdentification(queriedTypeId, queriedNumber))
                    return;
                if (local is not null)
                {
                    LoadExistingThirdParty(local);
                    var equivalentIdentityMessage =
                        local.NumeroIdentificacion == queriedNumber
                            ? string.Empty
                            : $" Corresponde al mismo contribuyente registrado con {local.TipoIdentificacionCodigo}: {local.NumeroIdentificacion}.";
                    if (local.EsCliente)
                    {
                        ClienteExistenteId = local.TerceroId;
                        MensajeIdentificacion =
                            $"Este cliente ya está registrado en el sistema.{equivalentIdentityMessage}";
                    }
                    else
                    {
                        TerceroExistenteSinCliente = true;
                        MensajeIdentificacion =
                            $"Esta persona ya existe en el sistema y se reutilizará como cliente.{equivalentIdentityMessage}";
                    }
                    return;
                }
            }

            ConsultaIdentificacionResult? lastResult = null;
            var verificationFlowId = Guid.NewGuid();
            _constanciaVerificacionId = null;
            for (var attempt = 1;
                 attempt <= MaxOfficialQueryAttempts;
                 attempt++)
            {
                IntentoConsultaActual = attempt;
                lastResult = await _consultaService.ConsultarAsync(
                    new ConsultaIdentificacionRequest(
                        TipoIdentificacion.Codigo,
                        validation.Normalized,
                        PropositoConsultaIdentificacion.Cliente,
                        verificationFlowId),
                    cancellationToken);
                if (!IsCurrentIdentification(queriedTypeId, queriedNumber))
                    return;
                if (lastResult.Encontrado)
                {
                    RazonSocial = lastResult.RazonSocial ?? string.Empty;
                    NombreComercial = lastResult.NombreComercial ?? NombreComercial;
                    Correo = lastResult.Correo ?? Correo;
                    Direccion = lastResult.Direccion ?? Direccion;
                    Verificacion = EstadoVerificacionCliente.Verificado;
                    FuenteVerificacion = lastResult.Fuente;
                    _constanciaVerificacionId =
                        lastResult.ConstanciaVerificacionId;
                    MensajeIdentificacion = null;
                    FocusRequested?.Invoke(ClienteFormFocusTarget.Address);
                    return;
                }

                if (lastResult.Estado is
                    EstadoConsultaIdentificacion.IdentificacionInvalida or
                    EstadoConsultaIdentificacion.NoEncontrado)
                {
                    IdentificacionRechazada = true;
                    MensajeIdentificacion = lastResult.MensajeUsuario;
                    await _messageDialogService.ShowWarningAsync(
                        "Identificación no válida",
                        lastResult.MensajeUsuario);
                    return;
                }

                if (attempt < MaxOfficialQueryAttempts)
                    await Task.Delay(500, cancellationToken);
            }

            if (lastResult?.ConstanciaVerificacionId is null)
            {
                IdentificacionRechazada = true;
                ModoOfflineHabilitado = false;
                MensajeIdentificacion =
                    "Las fuentes no confirmaron la identificación y tampoco se pudo autorizar el modo offline.";
                return;
            }

            _constanciaVerificacionId =
                lastResult.ConstanciaVerificacionId;
            Verificacion = EstadoVerificacionCliente.NoVerificado;
            FuenteVerificacion = "MANUAL";
            ModoOfflineHabilitado = true;
            MensajeIdentificacion =
                "Los servicios oficiales no se encuentran disponibles después de 3 intentos. El cliente se registrará sin verificación oficial; ingresa manualmente los nombres o la razón social.";
            FocusRequested?.Invoke(ClienteFormFocusTarget.Names);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_queryCancellation?.Token == cancellationToken)
                IsQuerying = false;
        }
    }

    private void CancelIdentificationQuery()
    {
        _queryCancellation?.Cancel();
        _queryCancellation?.Dispose();
        _queryCancellation = null;
        IsQuerying = false;
        IntentoConsultaActual = 0;
    }

    private void SetIdentificationMode(
        TipoIdentificacionClienteModo mode,
        bool clearIdentification = true)
    {
        var changed = _identificacionModo != mode;
        _identificacionModo = mode;
        CancelIdentificationQuery();
        _constanciaVerificacionId = null;

        var code = mode switch
        {
            TipoIdentificacionClienteModo.Pasaporte => "PASAPORTE",
            TipoIdentificacionClienteModo.Extranjero => "EXTERIOR",
            _ => IdentificacionEcuadorValidator.DetectNationalType(
                     NumeroIdentificacion) ?? "CEDULA"
        };
        TipoIdentificacion = TiposIdentificacion.FirstOrDefault(
            x => x.Codigo == code);

        if (changed && clearIdentification && !IsEditing)
        {
            NumeroIdentificacion = string.Empty;
            RazonSocial = string.Empty;
            NombreComercial = string.Empty;
            ClienteExistenteId = null;
            TerceroExistenteSinCliente = false;
        }

        ModoOfflineHabilitado = false;
        IdentificacionRechazada = false;
        Verificacion = mode == TipoIdentificacionClienteModo.Nacional
            ? EstadoVerificacionCliente.NoVerificado
            : EstadoVerificacionCliente.NoAplica;
        FuenteVerificacion = mode == TipoIdentificacionClienteModo.Nacional
            ? null
            : "MANUAL";
        MensajeIdentificacion = mode == TipoIdentificacionClienteModo.Nacional
            ? "La consulta se iniciará automáticamente al completar una cédula o RUC válido."
            : "Este tipo de identificación se registra manualmente.";
        NotifyIdentificationModeProperties();
        NotifyDerivedProperties();
    }

    private static TipoIdentificacionClienteModo ModeFromType(string code) =>
        code switch
        {
            "PASAPORTE" => TipoIdentificacionClienteModo.Pasaporte,
            "EXTERIOR" => TipoIdentificacionClienteModo.Extranjero,
            _ => TipoIdentificacionClienteModo.Nacional
        };

    private void NotifyIdentificationModeProperties()
    {
        OnPropertyChanged(nameof(IdentificacionNacional));
        OnPropertyChanged(nameof(IdentificacionPasaporte));
        OnPropertyChanged(nameof(IdentificacionExtranjero));
        OnPropertyChanged(nameof(NombrePrincipalEditable));
        OnPropertyChanged(nameof(TipoNacionalDetectadoTexto));
    }

    private void NotifyVerificationProperties()
    {
        OnPropertyChanged(nameof(VerificacionTexto));
        OnPropertyChanged(nameof(MostrarFuenteVerificacion));
        OnPropertyChanged(nameof(FuenteVerificacionInicial));
    }

    private async Task LoadCatalogsAsync(long companyId)
    {
        TiposIdentificacion.Clear();
        foreach (var type in await _clienteService.ObtenerTiposIdentificacionAsync())
            TiposIdentificacion.Add(type);

        ListasPrecio.Clear();
        foreach (var list in await _clienteService.ObtenerListasPrecioAsync(
                     companyId))
            ListasPrecio.Add(list);
        ListaPrecio = ListasPrecio.FirstOrDefault(x => x.EsListaBase);
        if (ListaPrecio is null)
            MensajeFormulario =
                "La empresa activa no tiene una lista de precios base configurada.";
        SetIdentificationMode(
            TipoIdentificacionClienteModo.Nacional,
            clearIdentification: false);
    }

    private void LoadExistingThirdParty(TerceroIdentificacionDto local)
    {
        _constanciaVerificacionId = null;
        TerceroId = local.TerceroId;
        _version = local.Version;
        EmpresaTerceroId = local.EmpresaTerceroId;
        RazonSocial = local.RazonSocial;
        NombreComercial = local.NombreComercial ?? string.Empty;
        Correo = string.IsNullOrWhiteSpace(local.Correo)
            ? ContactoClienteNormalizer.DefaultEmail
            : local.Correo;
        Telefono = local.Telefono ?? string.Empty;
        Direccion = local.Direccion ?? string.Empty;
        Observacion = local.Observacion ?? string.Empty;
        Verificacion = local.Verificacion;
        FuenteVerificacion = local.FuenteVerificacion;
        ModoOfflineHabilitado =
            IdentificacionNacional &&
            local.Verificacion != EstadoVerificacionCliente.Verificado;
        CreditoHabilitado = local.TieneConfiguracionEmpresaActual
            ? local.CreditoHabilitado
            : true;
        Activo = local.Estado == 1;
        ListaPrecio = ListasPrecio.FirstOrDefault(
            x => x.Id == local.ListaPrecioId) ?? ListaPrecio;
    }

    private void Reset()
    {
        CancelIdentificationQuery();
        _constanciaVerificacionId = null;
        EmpresaTerceroId = null;
        TerceroId = null;
        _version = null;
        IsEditing = false;
        IsBusy = false;
        IsQuerying = false;
        IntentoConsultaActual = 0;
        ModoOfflineHabilitado = false;
        IdentificacionRechazada = false;
        NumeroIdentificacion = string.Empty;
        RazonSocial = string.Empty;
        NombreComercial = string.Empty;
        Correo = ContactoClienteNormalizer.DefaultEmail;
        Telefono = string.Empty;
        Direccion = string.Empty;
        Observacion = string.Empty;
        CreditoHabilitado = true;
        Activo = true;
        Verificacion = EstadoVerificacionCliente.NoVerificado;
        FuenteVerificacion = null;
        MensajeFormulario = null;
        MensajeIdentificacion = null;
        ClienteExistenteId = null;
        TerceroExistenteSinCliente = false;
        _identificacionModo = TipoIdentificacionClienteModo.Nacional;
        NotifyIdentificationModeProperties();
    }

    public void CloseForCompanyChange()
    {
        Reset();
        TiposIdentificacion.Clear();
        ListasPrecio.Clear();
        ListaPrecio = null;
        _expectedCompanyId = null;
    }

    private bool HasEnteredData() =>
        IsEditing ||
        !string.IsNullOrWhiteSpace(NumeroIdentificacion) ||
        !string.IsNullOrWhiteSpace(RazonSocial) ||
        !string.IsNullOrWhiteSpace(NombreComercial) ||
        (!string.IsNullOrWhiteSpace(Correo) &&
         !string.Equals(
             Correo,
             ContactoClienteNormalizer.DefaultEmail,
             StringComparison.OrdinalIgnoreCase)) ||
        !string.IsNullOrWhiteSpace(Telefono) ||
        !string.IsNullOrWhiteSpace(Direccion) ||
        !string.IsNullOrWhiteSpace(Observacion);

    private void NotifyDerivedProperties()
    {
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(FormSubtitle));
        OnPropertyChanged(nameof(VerificationActionText));
        OnPropertyChanged(nameof(PuedeConsultar));
        OnPropertyChanged(nameof(IdentificacionEditable));
        OnPropertyChanged(nameof(NombrePrincipalEditable));
        OnPropertyChanged(nameof(TipoNacionalDetectadoTexto));
        OnPropertyChanged(nameof(VerificacionTexto));
        ConsultarIdentificacionCommand.NotifyCanExecuteChanged();
    }

    private long CurrentCompanyId() => _currentSession.EmpresaId ?? 0;

    private long ExpectedCompanyId() =>
        _expectedCompanyId is > 0
            ? _expectedCompanyId.Value
            : CurrentCompanyId();

    private bool IsCurrentIdentification(long typeId, string number) =>
        TipoIdentificacion?.Id == typeId &&
        string.Equals(
            IdentificacionEcuadorValidator.Normalize(
                NumeroIdentificacion,
                TipoIdentificacion.Codigo),
            number,
            StringComparison.Ordinal);
}
