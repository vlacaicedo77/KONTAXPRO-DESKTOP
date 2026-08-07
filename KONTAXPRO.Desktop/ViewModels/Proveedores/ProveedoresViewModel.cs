using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels.Proveedores;

public sealed record ProveedorFiltroItem<T>(string Nombre, T Valor);

public partial class ProveedoresViewModel : ObservableObject, IDisposable
{
    private readonly IProveedorService _service;
    private readonly IMessageDialogService _dialogs;
    private readonly INotificationService _notifications;
    private readonly CurrentSession _session;
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _searchCancellation;
    private long _loadSequence;
    private bool _synchronizingFilters;
    private bool _disposed;

    public ProveedorFormViewModel ProveedorForm { get; }
    public ObservableCollection<ProveedorCatalogoItemDto> Proveedores { get; } = [];
    public IReadOnlyList<int> TamanosPagina { get; } = [25, 50, 100];
    public IReadOnlyList<ProveedorFiltroItem<ProveedorEstadoFiltro>> Estados { get; } =
    [
        new("ACTIVOS", ProveedorEstadoFiltro.Activos),
        new("INACTIVOS", ProveedorEstadoFiltro.Inactivos),
        new("TODOS", ProveedorEstadoFiltro.Todos)
    ];
    public IReadOnlyList<ProveedorFiltroItem<ProveedorVerificacionFiltro>>
        Verificaciones { get; } =
    [
        new("TODOS", ProveedorVerificacionFiltro.Todos),
        new("VERIFICADOS", ProveedorVerificacionFiltro.Verificados),
        new("NO VERIFICADOS", ProveedorVerificacionFiltro.NoVerificados)
    ];

    [ObservableProperty] private string textoBusqueda = string.Empty;
    [ObservableProperty] private ProveedorFiltroItem<ProveedorEstadoFiltro>
        estadoSeleccionado;
    [ObservableProperty] private ProveedorFiltroItem<ProveedorVerificacionFiltro>
        verificacionSeleccionada;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSupplierFormOpen;
    [ObservableProperty] private int paginaActual = 1;
    [ObservableProperty] private int tamanoPagina = 25;
    [ObservableProperty] private int totalItems;
    [ObservableProperty] private int totalActivos;
    [ObservableProperty] private int totalPendientesVerificar;
    [ObservableProperty] private int totalSinCorreo;
    [ObservableProperty] private int totalSinContactoDigital;
    [ObservableProperty] private string? mensajeEstado;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsKpiActivos))]
    [NotifyPropertyChangedFor(nameof(EsKpiPendientes))]
    [NotifyPropertyChangedFor(nameof(EsKpiSinCorreo))]
    [NotifyPropertyChangedFor(nameof(EsKpiSinContacto))]
    private ProveedorCatalogoKpi kpiActivo;

    public bool EsKpiActivos => KpiActivo == ProveedorCatalogoKpi.Activos;
    public bool EsKpiPendientes =>
        KpiActivo == ProveedorCatalogoKpi.PendientesVerificar;
    public bool EsKpiSinCorreo => KpiActivo == ProveedorCatalogoKpi.SinCorreo;
    public bool EsKpiSinContacto =>
        KpiActivo == ProveedorCatalogoKpi.SinContactoDigital;
    public bool PuedeGestionarTerceros =>
        _session.HasPermission(TercerosPermissions.Gestionar);
    public bool HayResultados => Proveedores.Count > 0;
    public bool MostrarEstadoVacio => !IsLoading && !HayResultados;
    public int TotalPaginas => TotalItems == 0
        ? 0
        : (int)Math.Ceiling((double)TotalItems / TamanoPagina);
    public bool PuedeIrAnterior => PaginaActual > 1;
    public bool PuedeIrSiguiente => PaginaActual < TotalPaginas;
    public string TextoPaginacion => TotalItems == 0
        ? "Mostrando 0 de 0 proveedores"
        : $"Mostrando {(PaginaActual - 1) * TamanoPagina + 1:N0}–{Math.Min(PaginaActual * TamanoPagina, TotalItems):N0} de {TotalItems:N0} proveedores";

    public ProveedoresViewModel(
        IProveedorService service,
        ProveedorFormViewModel proveedorForm,
        IMessageDialogService dialogs,
        INotificationService notifications,
        CurrentSession session)
    {
        _service = service;
        ProveedorForm = proveedorForm;
        _dialogs = dialogs;
        _notifications = notifications;
        _session = session;
        estadoSeleccionado = Estados[2];
        verificacionSeleccionada = Verificaciones[0];
        kpiActivo = ProveedorCatalogoKpi.Activos;
        ProveedorForm.CloseRequested += CloseForm;
        ProveedorForm.Saved += SupplierSaved;
        ProveedorForm.ConcurrencyConflictDetected += ConcurrencyConflictDetected;
        _session.EmpresaActivaChanged += OnEmpresaActivaChanged;
    }

    public Task InitializeAsync() => LoadAsync();

    [RelayCommand] private Task RecargarAsync() => LoadAsync();
    [RelayCommand] private void LimpiarBusqueda() => TextoBusqueda = string.Empty;

    [RelayCommand]
    private void SeleccionarKpi(ProveedorCatalogoKpi kpi)
    {
        if (KpiActivo == kpi)
        {
            KpiActivo = ProveedorCatalogoKpi.Todos;
            Reload();
            return;
        }
        _synchronizingFilters = true;
        try
        {
            EstadoSeleccionado = Estados[2];
            VerificacionSeleccionada = Verificaciones[0];
        }
        finally
        {
            _synchronizingFilters = false;
        }
        KpiActivo = kpi;
        Reload();
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task NuevoProveedorAsync()
    {
        await ProveedorForm.NuevoAsync();
        IsSupplierFormOpen = ProveedorForm.IsPrepared;
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task EditarProveedorAsync(ProveedorCatalogoItemDto? supplier)
    {
        if (supplier is null)
            return;
        await ProveedorForm.EditarAsync(supplier.TerceroId);
        IsSupplierFormOpen = ProveedorForm.IsPrepared;
    }

    [RelayCommand(CanExecute = nameof(PuedeGestionarTerceros))]
    private async Task CambiarEstadoAsync(ProveedorCatalogoItemDto? supplier)
    {
        if (supplier is null)
            return;
        var activate = supplier.Estado == 0;
        if (!await _dialogs.ConfirmAsync(
                activate ? "Activar proveedor" : "Inactivar proveedor",
                activate
                    ? $"¿Deseas activar a {supplier.RazonSocial}?"
                    : $"¿Deseas inactivar a {supplier.RazonSocial}? Su perfil de cliente no se modificará.",
                activate ? "Activar" : "Inactivar",
                "Cancelar",
                !activate))
            return;
        var result = await _service.CambiarEstadoAsync(
            supplier.TerceroId,
            activate ? 1 : 0,
            supplier.Version);
        if (!result.Success)
        {
            if (result.ConcurrencyConflict)
                await LoadAsync();
            await _dialogs.ShowErrorAsync("No se pudo cambiar el estado", result.Message);
            return;
        }
        await LoadAsync();
        await _notifications.ShowSuccessAsync(result.Message);
    }

    [RelayCommand]
    private void PaginaAnterior()
    {
        if (!PuedeIrAnterior) return;
        PaginaActual--;
        _ = LoadAsync();
    }

    [RelayCommand]
    private void PaginaSiguiente()
    {
        if (!PuedeIrSiguiente) return;
        PaginaActual++;
        _ = LoadAsync();
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        PaginaActual = 1;
        _ = SearchDelayedAsync();
    }
    partial void OnEstadoSeleccionadoChanged(
        ProveedorFiltroItem<ProveedorEstadoFiltro> value) => AdvancedReload();
    partial void OnVerificacionSeleccionadaChanged(
        ProveedorFiltroItem<ProveedorVerificacionFiltro> value) => AdvancedReload();
    partial void OnTamanoPaginaChanged(int value) => Reload();
    partial void OnIsLoadingChanged(bool value) => NotifyState();

    private void Reload()
    {
        PaginaActual = 1;
        _ = LoadAsync();
    }

    private void AdvancedReload()
    {
        if (_synchronizingFilters) return;
        KpiActivo = ProveedorCatalogoKpi.Todos;
        Reload();
    }

    private async Task SearchDelayedAsync()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _searchCancellation.Token);
            await LoadAsync(_searchCancellation.Token);
        }
        catch (OperationCanceledException) { }
    }

    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var sequence = Interlocked.Increment(ref _loadSequence);
        IsLoading = true;
        MensajeEstado = null;
        try
        {
            var result = await _service.ObtenerProveedoresAsync(
                new ProveedorCatalogoQuery
                {
                    Busqueda = TextoBusqueda,
                    Kpi = KpiActivo,
                    Estado = EstadoSeleccionado.Valor,
                    Verificacion = VerificacionSeleccionada.Valor,
                    Pagina = PaginaActual,
                    TamanoPagina = TamanoPagina
                },
                _loadCancellation.Token);
            if (sequence != Volatile.Read(ref _loadSequence))
                return;
            Proveedores.Clear();
            foreach (var supplier in result.Items)
                Proveedores.Add(supplier);
            TotalItems = result.Total;
            TotalActivos = result.Kpis.Activos;
            TotalPendientesVerificar = result.Kpis.PendientesVerificar;
            TotalSinCorreo = result.Kpis.SinCorreo;
            TotalSinContactoDigital = result.Kpis.SinContactoDigital;
            PaginaActual = result.Pagina;
            NotifyState();
        }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine(exception.GetType().Name);
            MensajeEstado = "No fue posible cargar los proveedores.";
        }
        finally
        {
            if (sequence == Volatile.Read(ref _loadSequence))
                IsLoading = false;
        }
    }

    private async void SupplierSaved(long id)
    {
        IsSupplierFormOpen = false;
        await LoadAsync();
        await _notifications.ShowSuccessAsync("Proveedor guardado correctamente.");
    }

    private void CloseForm() => IsSupplierFormOpen = false;

    private void ConcurrencyConflictDetected() => _ = LoadAsync();

    private async void OnEmpresaActivaChanged(
        object? sender,
        EmpresaActivaChangedEventArgs args)
    {
        OnPropertyChanged(nameof(PuedeGestionarTerceros));
        NuevoProveedorCommand.NotifyCanExecuteChanged();
        EditarProveedorCommand.NotifyCanExecuteChanged();
        CambiarEstadoCommand.NotifyCanExecuteChanged();
        if (!IsSupplierFormOpen)
            return;

        ProveedorForm.CloseForCompanyChange();
        IsSupplierFormOpen = false;
        await _dialogs.ShowWarningAsync(
            "Empresa activa actualizada",
            "El formulario de proveedor se cerró porque la autorización de mantenimiento depende de la empresa activa.");
    }

    private void NotifyState()
    {
        OnPropertyChanged(nameof(HayResultados));
        OnPropertyChanged(nameof(MostrarEstadoVacio));
        OnPropertyChanged(nameof(TotalPaginas));
        OnPropertyChanged(nameof(PuedeIrAnterior));
        OnPropertyChanged(nameof(PuedeIrSiguiente));
        OnPropertyChanged(nameof(TextoPaginacion));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _session.EmpresaActivaChanged -= OnEmpresaActivaChanged;
        ProveedorForm.CloseRequested -= CloseForm;
        ProveedorForm.Saved -= SupplierSaved;
        ProveedorForm.ConcurrencyConflictDetected -= ConcurrencyConflictDetected;
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
    }
}
