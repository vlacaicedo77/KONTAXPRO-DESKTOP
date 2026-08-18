using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Tesoreria;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;
using Microsoft.Win32;

namespace KONTAXPRO.Desktop.ViewModels.Tesoreria;

public partial class OperacionesSinSustentoViewModel : ObservableObject,
    IAsyncNavigationTarget,
    IDisposable
{
    private readonly IOperacionSinSustentoService _service;
    private readonly CurrentSession _session;
    private readonly IMessageDialogService _dialogs;
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _searchCancellation;
    private long _loadVersion;
    private bool _suppressAutoLoad;
    private readonly object _initializationLock = new();
    private Task? _initializationTask;
    private bool _disposed;

    public ObservableCollection<OperacionSinSustentoItemDto> Operations { get; } = [];
    public ObservableCollection<PaginaOperacionItemViewModel> VisiblePages { get; } = [];
    public IReadOnlyList<int> PageSizes { get; } = [25, 50, 100];
    public OperacionSinSustentoViewModel Form { get; }

    [ObservableProperty] private OperacionSinSustentoItemDto? selectedOperation;
    [ObservableProperty] private OperacionSinSustentoDetalleDto? selectedDetail;
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedState = "TODAS";
    [ObservableProperty] private string selectedType = "TODOS";
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isFormOpen;
    [ObservableProperty] private bool isDetailOpen;
    [ObservableProperty] private bool isCancelOpen;
    [ObservableProperty] private string cancelReason = string.Empty;
    [ObservableProperty] private int confirmedCount;
    [ObservableProperty] private int expenseCount;
    [ObservableProperty] private int inventoryCount;
    [ObservableProperty] private int cancelledCount;
    [ObservableProperty] private decimal confirmedTotal;
    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalItems;
    [ObservableProperty] private OperacionSinSustentoCatalogoOrden operationOrder =
        OperacionSinSustentoCatalogoOrden.Fecha;
    [ObservableProperty] private bool operationOrderDescending = true;

    public bool HasItems => Operations.Count > 0;
    public int OperationCount => ConfirmedCount + CancelledCount;
    public bool IsAllKpi => SelectedState == "TODAS" && SelectedType == "TODOS";
    public bool IsExpenseKpi => SelectedState == "CONFIRMADO" && SelectedType == "GASTO";
    public bool IsInventoryKpi => SelectedState == "CONFIRMADO" && SelectedType == "INVENTARIO";
    public bool IsCancelledKpi => SelectedState == "ANULADO";
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(
        TotalItems / (double)PageSize));
    public bool CanGoPrevious => Page > 1;
    public bool CanGoNext => Page < TotalPages;
    public string PaginationText
    {
        get
        {
            if (TotalItems == 0) return "Mostrando 0 de 0 operaciones";
            var start = (Page - 1) * PageSize + 1;
            var end = Math.Min(Page * PageSize, TotalItems);
            return $"Mostrando {start:N0}–{end:N0} de {TotalItems:N0} operaciones";
        }
    }
    public string DateSortIndicator => OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden.Fecha);
    public string OperationSortIndicatorText => OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden.Operacion);
    public string BeneficiarySortIndicator => OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden.Beneficiario);
    public string FundSortIndicator => OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden.Fondo);
    public string TotalSortIndicator => OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden.Total);
    public string StateSortIndicator => OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden.Estado);
    public bool CanRegister => _session.HasPermission("TESORERIA_REGISTRAR_SIN_SUSTENTO");
    public bool CanCorrect => _session.HasPermission("TESORERIA_CORREGIR_SIN_SUSTENTO");
    public bool CanCancel => _session.HasPermission("TESORERIA_ANULAR_SIN_SUSTENTO");

    public OperacionesSinSustentoViewModel(IOperacionSinSustentoService service,
        CurrentSession session, IMessageDialogService dialogs,
        OperacionSinSustentoViewModel form)
    {
        _service = service; _session = session; _dialogs = dialogs; Form = form;
        Form.CloseRequested += CloseForm;
        Form.Saved += OnSaved;
        _session.EmpresaActivaChanged += OnCompanyChanged;
    }

    public Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_initializationLock)
        {
            if (_disposed) return Task.CompletedTask;
            return _initializationTask ??= LoadAsync(cancellationToken);
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !_session.EmpresaId.HasValue ||
            !_session.EstablecimientoId.HasValue) return;
        _loadCancellation?.Cancel(); _loadCancellation?.Dispose();
        _loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        var version = ++_loadVersion;
        IsLoading = true;
        try
        {
            var result = await _service.ListarAsync(new OperacionSinSustentoCatalogoRequest
            { EmpresaId = _session.EmpresaId.Value,
              EstablecimientoId = _session.EstablecimientoId.Value,
              UsuarioId = _session.UsuarioId,
              Busqueda = SearchText, Estado = SelectedState, Tipo = SelectedType,
              Orden = OperationOrder,
              OrdenDescendente = OperationOrderDescending,
              Pagina = Page, TamanoPagina = PageSize },
                _loadCancellation.Token);
            if (version != _loadVersion) return;
            Operations.Clear(); foreach (var item in result.Items) Operations.Add(item);
            TotalItems = result.Total;
            ConfirmedCount = result.Confirmadas; ExpenseCount = result.Gastos;
            InventoryCount = result.Inventario; CancelledCount = result.Anuladas;
            ConfirmedTotal = result.TotalConfirmado;
            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(OperationCount));
            NotifyPagination();
            NotifyKpis();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (version == _loadVersion)
                await _dialogs.ShowWarningAsync("No se pudo cargar",
                    $"No fue posible consultar las operaciones. {ex.Message}");
        }
        finally { if (version == _loadVersion) IsLoading = false; }
    }

    [RelayCommand]
    private async Task SortOperationsAsync(
        OperacionSinSustentoCatalogoOrden order)
    {
        if (OperationOrder == order)
            OperationOrderDescending = !OperationOrderDescending;
        else
        {
            OperationOrder = order;
            OperationOrderDescending = false;
        }

        Page = 1;
        NotifyOperationSortIndicators();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NewAsync()
    {
        IsDetailOpen = false; IsCancelOpen = false; IsFormOpen = true;
        await Form.InitializeNewAsync();
    }

    [RelayCommand]
    private async Task ViewAsync(OperacionSinSustentoItemDto? item)
    {
        item ??= SelectedOperation; if (item is null || !_session.EmpresaId.HasValue) return;
        SelectedDetail = await _service.ObtenerAsync(_session.EmpresaId.Value,
            _session.UsuarioId, item.Id);
        IsDetailOpen = SelectedDetail is not null; IsFormOpen = false; IsCancelOpen = false;
    }

    [RelayCommand]
    private async Task CorrectAsync(OperacionSinSustentoItemDto? item)
    {
        item ??= SelectedOperation;
        if (item is null || !_session.EmpresaId.HasValue) return;
        if (item.Estado != "CONFIRMADO")
        { await _dialogs.ShowWarningAsync("Corrección no disponible", "Solo se corrigen operaciones confirmadas."); return; }
        var detail = await _service.ObtenerAsync(_session.EmpresaId.Value,
            _session.UsuarioId, item.Id);
        if (detail is null) return;
        if (!await _dialogs.ConfirmWarningAsync("Corregir mediante sustitución",
            "La operación original será anulada con reversos y se creará una nueva. No se sobrescribirá el histórico.",
            "Preparar corrección")) return;
        IsDetailOpen = false; IsCancelOpen = false; IsFormOpen = true;
        await Form.InitializeCorrectionAsync(detail);
    }

    [RelayCommand]
    private async Task ExportEvidenceAsync()
    {
        if (SelectedDetail is not { TieneEvidencia: true } detail ||
            !_session.EmpresaId.HasValue) return;
        try
        {
            var evidence = await _service.ObtenerEvidenciaAsync(
                _session.EmpresaId.Value, _session.UsuarioId, detail.Id);
            var dialog = new SaveFileDialog
            {
                FileName = evidence.NombreArchivo,
                Filter = "Archivo de evidencia|*" + Path.GetExtension(evidence.NombreArchivo) +
                         "|Todos los archivos|*.*"
            };
            if (dialog.ShowDialog() != true) return;
            await File.WriteAllBytesAsync(dialog.FileName, evidence.Contenido);
            await _dialogs.ShowSuccessAsync("Evidencia verificada",
                "La huella SHA-256 coincide y el archivo fue exportado correctamente.");
        }
        catch (Exception ex)
        {
            await _dialogs.ShowWarningAsync("No se pudo recuperar la evidencia", ex.Message);
        }
    }

    [RelayCommand]
    private void PrepareCancel(OperacionSinSustentoItemDto? item)
    {
        item ??= SelectedOperation; if (item?.Estado != "CONFIRMADO") return;
        SelectedOperation = item; CancelReason = string.Empty;
        IsCancelOpen = true; IsDetailOpen = false; IsFormOpen = false;
    }

    [RelayCommand]
    private async Task ConfirmCancelAsync()
    {
        if (SelectedOperation is null || !_session.EmpresaId.HasValue) return;
        if (!await _dialogs.ConfirmAsync("Anular operación",
            $"Se crearán reversos para {SelectedOperation.Numero}. Esta acción conserva el histórico.",
            "Anular operación", isDestructive: true)) return;
        IsLoading = true;
        try
        {
            var result = await _service.AnularAsync(new AnularOperacionSinSustentoRequest(
                _session.EmpresaId.Value, _session.UsuarioId, SelectedOperation.Id, CancelReason));
            if (!result.Success) { await _dialogs.ShowWarningAsync("No se pudo anular", result.Message); return; }
            await _dialogs.ShowSuccessAsync("Operación anulada", result.Message);
            IsCancelOpen = false; await LoadAsync();
        }
        finally { IsLoading = false; }
    }

    [RelayCommand] private void ClosePanels() { IsFormOpen = false; IsDetailOpen = false; IsCancelOpen = false; }

    [RelayCommand]
    private async Task SelectKpiAsync(string? key)
    {
        _suppressAutoLoad = true;
        Page = 1;
        (SelectedState, SelectedType) = key switch
        { "GASTO" => ("CONFIRMADO", "GASTO"), "INVENTARIO" => ("CONFIRMADO", "INVENTARIO"),
          "ANULADO" => ("ANULADO", "TODOS"), _ => ("TODAS", "TODOS") };
        _suppressAutoLoad = false;
        NotifyKpis(); await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _suppressAutoLoad = true;
        Page = 1; SearchText = null; SelectedState = "TODAS"; SelectedType = "TODOS";
        _suppressAutoLoad = false;
        await LoadAsync();
    }

    [RelayCommand]
    private void ClearSearch() => SearchText = null;

    partial void OnSearchTextChanged(string? value) { if (!_suppressAutoLoad) DebounceSearch(); }
    partial void OnSelectedStateChanged(string value) { if (!_suppressAutoLoad) { Page = 1; _ = LoadAsync(); } }
    partial void OnSelectedTypeChanged(string value) { if (!_suppressAutoLoad) { Page = 1; _ = LoadAsync(); } }
    partial void OnPageChanged(int value) => NotifyPagination();
    partial void OnPageSizeChanged(int value) { Page = 1; _ = LoadAsync(); }

    [RelayCommand(CanExecute = nameof(CanGoPrevious))]
    private async Task PreviousPageAsync() { Page--; await LoadAsync(); }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextPageAsync() { Page++; await LoadAsync(); }

    [RelayCommand]
    private async Task GoToPageAsync(PaginaOperacionItemViewModel? page)
    {
        if (page is null || page.IsSeparator || page.Number == Page) return;
        Page = page.Number;
        await LoadAsync();
    }

    private async void DebounceSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        var token = _searchCancellation.Token;
        try
        {
            await Task.Delay(350, token);
            Page = 1;
            await LoadAsync(token);
        }
        catch (OperationCanceledException) { }
    }

    private void NotifyPagination()
    {
        BuildVisiblePages();
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(PaginationText));
        PreviousPageCommand.NotifyCanExecuteChanged();
        NextPageCommand.NotifyCanExecuteChanged();
    }

    private string OperationSortIndicator(
        OperacionSinSustentoCatalogoOrden order) =>
        OperationOrder != order
            ? string.Empty
            : OperationOrderDescending ? "▼" : "▲";

    private void NotifyOperationSortIndicators()
    {
        OnPropertyChanged(nameof(DateSortIndicator));
        OnPropertyChanged(nameof(OperationSortIndicatorText));
        OnPropertyChanged(nameof(BeneficiarySortIndicator));
        OnPropertyChanged(nameof(FundSortIndicator));
        OnPropertyChanged(nameof(TotalSortIndicator));
        OnPropertyChanged(nameof(StateSortIndicator));
    }

    private void BuildVisiblePages()
    {
        VisiblePages.Clear();
        if (TotalPages <= 0) return;
        var pages = new SortedSet<int>
        {
            1,
            TotalPages,
            Math.Max(1, Page - 2),
            Math.Max(1, Page - 1),
            Page,
            Math.Min(TotalPages, Page + 1),
            Math.Min(TotalPages, Page + 2)
        };
        var previous = 0;
        foreach (var number in pages)
        {
            if (previous > 0 && number - previous > 1)
                VisiblePages.Add(PaginaOperacionItemViewModel.Separator());
            VisiblePages.Add(new PaginaOperacionItemViewModel(number,
                number == Page));
            previous = number;
        }
    }
    private void CloseForm() => IsFormOpen = false;
    private void OnSaved() => _ = LoadAsync();
    private async void OnCompanyChanged(object? sender, EmpresaActivaChangedEventArgs e)
    { ClosePanels(); OnPropertyChanged(nameof(CanRegister)); OnPropertyChanged(nameof(CanCorrect)); OnPropertyChanged(nameof(CanCancel)); await LoadAsync(); }
    private void NotifyKpis()
    { OnPropertyChanged(nameof(IsAllKpi)); OnPropertyChanged(nameof(IsExpenseKpi));
      OnPropertyChanged(nameof(IsInventoryKpi)); OnPropertyChanged(nameof(IsCancelledKpi)); }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _loadCancellation?.Cancel(); _loadCancellation?.Dispose();
        _searchCancellation?.Cancel(); _searchCancellation?.Dispose();
        Form.CloseRequested -= CloseForm; Form.Saved -= OnSaved;
        _session.EmpresaActivaChanged -= OnCompanyChanged;
    }
}

public sealed class PaginaOperacionItemViewModel
{
    public int Number { get; }
    public string Text { get; }
    public bool IsCurrent { get; }
    public bool IsSeparator { get; }

    public PaginaOperacionItemViewModel(int number, bool isCurrent)
    {
        Number = number;
        Text = number.ToString();
        IsCurrent = isCurrent;
    }

    private PaginaOperacionItemViewModel()
    {
        Text = "…";
        IsSeparator = true;
    }

    public static PaginaOperacionItemViewModel Separator() => new();
}
