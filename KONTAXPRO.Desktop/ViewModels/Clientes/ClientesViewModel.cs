using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Clientes;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;

namespace KONTAXPRO.Desktop.ViewModels.Clientes;

public sealed record ClienteFiltroItem<T>(string Nombre, T Valor);

public partial class ClientesViewModel : ObservableObject,
    IAsyncNavigationTarget,
    IDisposable
{
    private readonly IClienteService _clienteService;
    private readonly CurrentSession _currentSession;
    private readonly IMessageDialogService _messageDialogService;
    private readonly INotificationService _notificationService;
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _searchCancellation;
    private long _loadSequence;
    private bool _isSynchronizingQuickFilters;
    private bool _suppressReload;
    private bool _isDisposed;
    private Task _companyChangeTask = Task.CompletedTask;
    private readonly object _initializationLock = new();
    private Task? _initializationTask;

    public ClienteFormViewModel ClienteForm { get; }
    public ObservableCollection<ClienteCatalogoItemDto> Clientes { get; } = [];
    public IReadOnlyList<int> TamanosPagina { get; } = [25, 50, 100];
    public IReadOnlyList<ClienteFiltroItem<ClienteEstadoFiltro>> Estados { get; } =
    [
        new("ACTIVOS", ClienteEstadoFiltro.Activos),
        new("INACTIVOS", ClienteEstadoFiltro.Inactivos),
        new("TODOS", ClienteEstadoFiltro.Todos)
    ];
    public IReadOnlyList<ClienteFiltroItem<ClienteVerificacionFiltro>>
        Verificaciones { get; } =
    [
        new("TODOS", ClienteVerificacionFiltro.Todos),
        new("VERIFICADOS", ClienteVerificacionFiltro.Verificados),
        new("NO VERIFICADOS", ClienteVerificacionFiltro.NoVerificados)
    ];
    public IReadOnlyList<ClienteFiltroItem<ClienteCreditoFiltro>> Creditos { get; } =
    [
        new("TODOS", ClienteCreditoFiltro.Todos),
        new("CON CRÉDITO", ClienteCreditoFiltro.ConCredito),
        new("SIN CRÉDITO", ClienteCreditoFiltro.SinCredito)
    ];

    [ObservableProperty] private string textoBusqueda = string.Empty;
    [ObservableProperty] private ClienteFiltroItem<ClienteEstadoFiltro>
        estadoSeleccionado;
    [ObservableProperty] private ClienteFiltroItem<ClienteVerificacionFiltro>
        verificacionSeleccionada;
    [ObservableProperty] private ClienteFiltroItem<ClienteCreditoFiltro>
        creditoSeleccionado;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isClientFormOpen;
    [ObservableProperty] private int paginaActual = 1;
    [ObservableProperty] private int tamanoPagina = 25;
    [ObservableProperty] private int totalItems;
    [ObservableProperty] private ClienteCatalogoOrden ordenClientes =
        ClienteCatalogoOrden.RazonSocial;
    [ObservableProperty] private bool ordenClientesDescendente;
    [ObservableProperty] private int totalClientes;
    [ObservableProperty] private int totalPendientesVerificar;
    [ObservableProperty] private int totalSinCredito;
    [ObservableProperty] private int totalSinContactoDigital;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsKpiClientes))]
    [NotifyPropertyChangedFor(nameof(EsKpiPendientesVerificar))]
    [NotifyPropertyChangedFor(nameof(EsKpiSinCredito))]
    [NotifyPropertyChangedFor(nameof(EsKpiSinContactoDigital))]
    private ClienteCatalogoKpi kpiActivo = ClienteCatalogoKpi.Todos;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TituloEstadoVacio))]
    private string? mensajeEstado;

    public bool EsKpiClientes => KpiActivo == ClienteCatalogoKpi.Todos;
    public bool EsKpiPendientesVerificar =>
        KpiActivo == ClienteCatalogoKpi.PendientesVerificar;
    public bool EsKpiSinCredito =>
        KpiActivo == ClienteCatalogoKpi.SinCredito;
    public bool EsKpiSinContactoDigital =>
        KpiActivo == ClienteCatalogoKpi.SinContactoDigital;
    public bool PuedeGestionarTerceros =>
        _currentSession.HasPermission(TercerosPermissions.Gestionar);
    public bool HayResultados => Clientes.Count > 0;
    public bool MostrarEstadoVacio => !IsLoading && !HayResultados;
    public string TituloEstadoVacio => string.IsNullOrWhiteSpace(MensajeEstado)
        ? "No se encontraron clientes"
        : MensajeEstado;
    public int TotalPaginas => TotalItems == 0
        ? 0
        : (int)Math.Ceiling((double)TotalItems / TamanoPagina);
    public bool PuedeIrAnterior => PaginaActual > 1;
    public bool PuedeIrSiguiente => PaginaActual < TotalPaginas;
    public Task PendingCompanyChange => _companyChangeTask;
    public string IndicadorIdentificacion => IndicadorOrdenClientes(
        ClienteCatalogoOrden.Identificacion);
    public string IndicadorRazonSocial => IndicadorOrdenClientes(
        ClienteCatalogoOrden.RazonSocial);
    public string IndicadorClasificacion => IndicadorOrdenClientes(
        ClienteCatalogoOrden.Clasificacion);
    public string IndicadorCredito => IndicadorOrdenClientes(
        ClienteCatalogoOrden.Credito);
    public string IndicadorEstado => IndicadorOrdenClientes(
        ClienteCatalogoOrden.Estado);
    public string TextoPaginacion
    {
        get
        {
            if (TotalItems == 0)
                return "Mostrando 0 de 0 clientes";
            var start = (PaginaActual - 1) * TamanoPagina + 1;
            var end = Math.Min(PaginaActual * TamanoPagina, TotalItems);
            return $"Mostrando {start:N0}–{end:N0} de {TotalItems:N0} clientes";
        }
    }

    public ClientesViewModel(
        IClienteService clienteService,
        CurrentSession currentSession,
        ClienteFormViewModel clienteForm,
        IMessageDialogService messageDialogService,
        INotificationService notificationService)
    {
        _clienteService = clienteService;
        _currentSession = currentSession;
        ClienteForm = clienteForm;
        _messageDialogService = messageDialogService;
        _notificationService = notificationService;
        estadoSeleccionado = Estados[0];
        verificacionSeleccionada = Verificaciones[0];
        creditoSeleccionado = Creditos[0];
        ClienteForm.CloseRequested += OnFormCloseRequested;
        ClienteForm.Saved += OnClientSaved;
        ClienteForm.ExistingClientRequested += OnExistingClientRequested;
        ClienteForm.ConcurrencyConflictDetected += OnConcurrencyConflictDetected;
        _currentSession.EmpresaActivaChanged += OnEmpresaActivaChanged;
    }

    public Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_initializationLock)
        {
            if (_isDisposed)
                return Task.CompletedTask;
            return _initializationTask ??= LoadAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private Task RecargarAsync() => LoadAsync();

    [RelayCommand]
    private void LimpiarBusqueda() => TextoBusqueda = string.Empty;

    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
        _suppressReload = true;
        try
        {
            TextoBusqueda = string.Empty;
            EstadoSeleccionado = Estados[0];
            VerificacionSeleccionada = Verificaciones[0];
            CreditoSeleccionado = Creditos[0];
            KpiActivo = ClienteCatalogoKpi.Todos;
            PaginaActual = 1;
        }
        finally
        {
            _suppressReload = false;
        }

        await LoadAsync();
    }

    [RelayCommand]
    private void SeleccionarKpi(ClienteCatalogoKpi kpi)
    {
        if (KpiActivo == kpi)
            return;

        _isSynchronizingQuickFilters = true;
        try
        {
            VerificacionSeleccionada = Verificaciones[0];
            CreditoSeleccionado = Creditos[0];
        }
        finally
        {
            _isSynchronizingQuickFilters = false;
        }

        KpiActivo = kpi;
        ReloadFromFilter();
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task NuevoClienteAsync()
    {
        await ClienteForm.NuevoAsync();
        IsClientFormOpen = ClienteForm.IsPreparedForCurrentCompany;
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task EditarClienteAsync(ClienteCatalogoItemDto? client)
    {
        if (client is null)
            return;
        await ClienteForm.EditarAsync(client.TerceroId);
        IsClientFormOpen = ClienteForm.IsPreparedForCurrentCompany;
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task CambiarEstadoAsync(ClienteCatalogoItemDto? client)
    {
        if (client is null)
            return;
        var activate = client.Estado == 0;
        var confirmed = await _messageDialogService.ConfirmAsync(
            activate ? "Activar cliente" : "Inactivar cliente",
            activate
                ? $"¿Deseas activar globalmente a {client.RazonSocial} como cliente?"
                : $"¿Deseas inactivar globalmente a {client.RazonSocial} como cliente? Su posible rol de proveedor y sus datos históricos no se modificarán.",
            activate ? "Activar" : "Inactivar",
            "Cancelar",
            !activate);
        if (!confirmed)
            return;

        var result = await _clienteService.CambiarEstadoAsync(
            client.TerceroId,
            CurrentCompanyId(),
            activate ? 1 : 0,
            client.Version);
        if (!result.Success)
        {
            if (result.ConcurrencyConflict)
                await LoadAsync();
            await _messageDialogService.ShowErrorAsync(
                "No se pudo cambiar el estado",
                result.Message);
            return;
        }
        await LoadAsync();
        await _notificationService.ShowSuccessAsync(result.Message);
    }

    [RelayCommand]
    private void PaginaAnterior()
    {
        if (!PuedeIrAnterior)
            return;
        PaginaActual--;
        _ = LoadAsync();
    }

    [RelayCommand]
    private void PaginaSiguiente()
    {
        if (!PuedeIrSiguiente)
            return;
        PaginaActual++;
        _ = LoadAsync();
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        if (_suppressReload)
            return;
        PaginaActual = 1;
        _ = SearchWithDelayAsync();
    }

    partial void OnEstadoSeleccionadoChanged(
        ClienteFiltroItem<ClienteEstadoFiltro> value)
    {
        if (!_suppressReload)
            ReloadFromFilter();
    }
    partial void OnVerificacionSeleccionadaChanged(
        ClienteFiltroItem<ClienteVerificacionFiltro> value)
    {
        if (!_suppressReload)
            ReloadFromAdvancedFilter();
    }
    partial void OnCreditoSeleccionadoChanged(
        ClienteFiltroItem<ClienteCreditoFiltro> value)
    {
        if (!_suppressReload)
            ReloadFromAdvancedFilter();
    }
    partial void OnTamanoPaginaChanged(int value) => ReloadFromFilter();
    partial void OnIsLoadingChanged(bool value) => NotifyListState();

    private void ReloadFromFilter()
    {
        PaginaActual = 1;
        _ = LoadAsync();
    }

    private void ReloadFromAdvancedFilter()
    {
        if (_isSynchronizingQuickFilters)
            return;

        KpiActivo = ClienteCatalogoKpi.Todos;
        ReloadFromFilter();
    }

    private async Task SearchWithDelayAsync()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _searchCancellation.Token);
            await LoadAsync(_searchCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
            return;

        var companyId = CurrentCompanyId();
        if (companyId <= 0)
        {
            MensajeEstado = "No existe una empresa activa en la sesión.";
            return;
        }

        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sequence = Interlocked.Increment(ref _loadSequence);
        IsLoading = true;
        MensajeEstado = null;
        try
        {
            var result = await _clienteService.ObtenerClientesAsync(
                new ClienteCatalogoQuery
                {
                    EmpresaId = companyId,
                    Busqueda = TextoBusqueda,
                    Kpi = KpiActivo,
                    Estado = EstadoSeleccionado.Valor,
                    Verificacion = VerificacionSeleccionada.Valor,
                    Credito = CreditoSeleccionado.Valor,
                    Orden = OrdenClientes,
                    OrdenDescendente = OrdenClientesDescendente,
                    Pagina = PaginaActual,
                    TamanoPagina = TamanoPagina
                },
                _loadCancellation.Token);
            if (sequence != Volatile.Read(ref _loadSequence) ||
                CurrentCompanyId() != companyId)
                return;

            Clientes.Clear();
            foreach (var client in result.Items)
                Clientes.Add(client);
            TotalItems = result.Total;
            TotalClientes = result.Kpis.Clientes;
            TotalPendientesVerificar = result.Kpis.PendientesVerificar;
            TotalSinCredito = result.Kpis.SinCredito;
            TotalSinContactoDigital = result.Kpis.SinContactoDigital;
            PaginaActual = result.Pagina;
            NotifyListState();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception.GetType().Name);
            MensajeEstado = "No fue posible cargar los clientes.";
        }
        finally
        {
            if (sequence == Volatile.Read(ref _loadSequence))
                IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OrdenarClientesAsync(ClienteCatalogoOrden orden)
    {
        if (OrdenClientes == orden)
            OrdenClientesDescendente = !OrdenClientesDescendente;
        else
        {
            OrdenClientes = orden;
            OrdenClientesDescendente = false;
        }

        PaginaActual = 1;
        NotifyClientSortIndicators();
        await LoadAsync();
    }

    private async void OnClientSaved(long id)
    {
        IsClientFormOpen = false;
        await LoadAsync();
        await _notificationService.ShowSuccessAsync(
            "Cliente guardado correctamente.");
    }

    private async void OnExistingClientRequested(long id)
    {
        await ClienteForm.EditarAsync(id);
        IsClientFormOpen = ClienteForm.IsPreparedForCurrentCompany;
    }

    private void OnFormCloseRequested() => IsClientFormOpen = false;

    private void OnConcurrencyConflictDetected() => _ = LoadAsync();

    private void OnEmpresaActivaChanged(
        object? sender,
        EmpresaActivaChangedEventArgs args)
    {
        _companyChangeTask = HandleCompanyChangeAsync();
    }

    private async Task HandleCompanyChangeAsync()
    {
        if (_isDisposed)
            return;

        _searchCancellation?.Cancel();
        _loadCancellation?.Cancel();
        Interlocked.Increment(ref _loadSequence);
        IsLoading = false;
        PaginaActual = 1;
        ClearCompanyData();
        NotifyManagementPermissionChanged();

        if (IsClientFormOpen)
        {
            ClienteForm.CloseForCompanyChange();
            IsClientFormOpen = false;
            await _messageDialogService.ShowWarningAsync(
                "Empresa activa actualizada",
                "El formulario de cliente se cerró para evitar guardar condiciones comerciales en una empresa diferente.");
        }

        await LoadAsync();
    }

    private void ClearCompanyData()
    {
        Clientes.Clear();
        TotalItems = 0;
        TotalClientes = 0;
        TotalPendientesVerificar = 0;
        TotalSinCredito = 0;
        TotalSinContactoDigital = 0;
        MensajeEstado = null;
        NotifyListState();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        _currentSession.EmpresaActivaChanged -= OnEmpresaActivaChanged;
        ClienteForm.CloseRequested -= OnFormCloseRequested;
        ClienteForm.Saved -= OnClientSaved;
        ClienteForm.ExistingClientRequested -= OnExistingClientRequested;
        ClienteForm.ConcurrencyConflictDetected -= OnConcurrencyConflictDetected;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        ClienteForm.CloseForCompanyChange();
    }

    private void NotifyListState()
    {
        OnPropertyChanged(nameof(HayResultados));
        OnPropertyChanged(nameof(MostrarEstadoVacio));
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(PuedeIrAnterior));
        OnPropertyChanged(nameof(PuedeIrSiguiente));
        OnPropertyChanged(nameof(TextoPaginacion));
    }

    private string IndicadorOrdenClientes(ClienteCatalogoOrden orden) =>
        OrdenClientes != orden
            ? string.Empty
            : OrdenClientesDescendente ? "▼" : "▲";

    private void NotifyClientSortIndicators()
    {
        OnPropertyChanged(nameof(IndicadorIdentificacion));
        OnPropertyChanged(nameof(IndicadorRazonSocial));
        OnPropertyChanged(nameof(IndicadorClasificacion));
        OnPropertyChanged(nameof(IndicadorCredito));
        OnPropertyChanged(nameof(IndicadorEstado));
    }

    private void NotifyManagementPermissionChanged()
    {
        OnPropertyChanged(nameof(PuedeGestionarTerceros));
        NuevoClienteCommand.NotifyCanExecuteChanged();
        EditarClienteCommand.NotifyCanExecuteChanged();
        CambiarEstadoCommand.NotifyCanExecuteChanged();
    }

    private long CurrentCompanyId() => _currentSession.EmpresaId ?? 0;
}
