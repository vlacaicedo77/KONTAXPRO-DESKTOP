using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.Services;

namespace KONTAXPRO.Desktop.ViewModels.Inventory;

public partial class InventoryViewModel : ObservableObject,
    IAsyncNavigationTarget,
    IDisposable
{
    private readonly IInventoryQueryService _queries;
    private readonly IInventoryTransferService _transfers;
    private readonly IInventoryService _movements;
    private readonly CurrentSession _session;
    private readonly IMessageDialogService _dialogs;
    private bool _productAdjustmentWasOpened;
    public ProductFormViewModel ProductOperations { get; }
    private CancellationTokenSource? _loadCancellation;
    private CancellationTokenSource? _searchCancellation;
    private CancellationTokenSource? _kardexCancellation;
    private long _loadVersion;
    private long _kardexLoadVersion;
    private bool _suppressReload;
    private readonly object _initializationLock = new();
    private Task? _initializationTask;
    private bool _disposed;

    public ObservableCollection<InventarioItemDto> Items { get; } = [];
    public ObservableCollection<InventarioOpcionDto> Warehouses { get; } = [];
    public ObservableCollection<InventarioOpcionDto> WarehouseFilters { get; }
        = [];
    public ObservableCollection<KardexItemDto> KardexItems { get; } = [];
    public ObservableCollection<InventoryPageItem> VisiblePages { get; } = [];
    public ObservableCollection<InventoryPageItem> KardexVisiblePages { get; } = [];
    public ObservableCollection<InventarioOpcionDto> CorrectionSourceWarehouses
        { get; } = [];
    public ObservableCollection<InventarioOpcionDto> CorrectionTargetWarehouses
        { get; } = [];
    public ObservableCollection<InventoryCorrectionLotRow> CorrectionLots
        { get; } = [];
    public ObservableCollection<InventoryCorrectionSeriesRow> CorrectionSeries
        { get; } = [];
    public ObservableCollection<MotivoOperacionInventarioDto> AdjustmentReasons { get; } = [];
    public ObservableCollection<InventoryAdjustmentLotRow> AdjustmentLots { get; } = [];
    public ObservableCollection<InventoryAdjustmentSeriesRow> AdjustmentSeries { get; } = [];
    public IReadOnlyList<int> PageSizes { get; } = [25, 50, 100];

    [ObservableProperty] private InventarioItemDto? selectedItem;
    [ObservableProperty] private InventarioProductoDetalleDto? selectedDetail;
    [ObservableProperty] private InventarioOpcionDto? selectedWarehouse;
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string selectedIndicator = "TODOS";
    [ObservableProperty] private InventarioCatalogoOrden inventoryOrder =
        InventarioCatalogoOrden.Producto;
    [ObservableProperty] private bool inventoryOrderDescending;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isDetailOpen;
    [ObservableProperty] private int selectedDetailTabIndex;
    [ObservableProperty] private bool isKardexOpen;
    [ObservableProperty] private bool isInitialWarehouseCorrectionOpen;
    [ObservableProperty] private bool isAdjustmentOpen;
    [ObservableProperty] private int page = 1;
    [ObservableProperty] private int pageSize = 25;
    [ObservableProperty] private int totalItems;
    [ObservableProperty] private int productsCount;
    [ObservableProperty] private int noStockCount;
    [ObservableProperty] private int lowStockCount;
    [ObservableProperty] private int expiringCount;
    [ObservableProperty] private decimal inventoryValue;

    [ObservableProperty] private int kardexPage = 1;
    [ObservableProperty] private int kardexPageSize = 25;
    [ObservableProperty] private int kardexTotalItems;
    [ObservableProperty] private bool isKardexLoading;
    [ObservableProperty] private KardexItemDto? selectedKardexItem;
    [ObservableProperty] private DateTime? kardexFrom;
    [ObservableProperty] private DateTime? kardexTo;
    [ObservableProperty] private string kardexFromText = string.Empty;
    [ObservableProperty] private string kardexToText = string.Empty;
    [ObservableProperty] private string kardexNature = "TODAS";
    [ObservableProperty] private bool kardexDateDescending = true;

    [ObservableProperty] private InventarioOpcionDto? transferSource;
    [ObservableProperty] private InventarioOpcionDto? transferTarget;
    [ObservableProperty] private DateTime transferDate = DateTime.Today;
    [ObservableProperty] private string transferReason = string.Empty;
    [ObservableProperty] private bool isTransferSaving;
    [ObservableProperty] private decimal correctionQuantity;

    [ObservableProperty] private bool adjustmentIsInitial;
    [ObservableProperty] private string adjustmentType = "ENTRADA";
    [ObservableProperty] private InventarioOpcionDto? adjustmentWarehouse;
    [ObservableProperty] private InventarioPresentacionDto? adjustmentPresentation;
    [ObservableProperty] private MotivoOperacionInventarioDto? adjustmentReason;
    [ObservableProperty] private decimal adjustmentQuantity;
    [ObservableProperty] private decimal adjustmentCostTotal;
    [ObservableProperty] private DateTime adjustmentDate = DateTime.Today;
    [ObservableProperty] private string adjustmentJustification = string.Empty;
    [ObservableProperty] private string? adjustmentObservation;
    [ObservableProperty] private bool isAdjustmentSaving;
    [ObservableProperty] private bool isProductOperationOpen;

    public bool HasItems => Items.Count > 0;
    public bool IsAllKpi => SelectedIndicator == "TODOS";
    public bool IsNoStockKpi => SelectedIndicator == "SIN_STOCK";
    public bool IsLowStockKpi => SelectedIndicator == "STOCK_BAJO";
    public bool IsExpiringKpi => SelectedIndicator == "POR_CADUCAR";
    public bool CanCorrectInitialWarehouse =>
        _session.HasPermission("INVENTARIO_AGREGAR_ENTRADA_INICIAL");
    public bool CanReconcile => _session.HasPermission("INVENTARIO_RECONCILIAR");
    public bool CanViewCosts => _session.HasPermission("INVENTARIO_VER_COSTO");
    public bool CanAdjust => _session.HasPermission("INVENTARIO_REGISTRAR_AJUSTE");
    public bool CanRegisterInitial =>
        _session.HasPermission("INVENTARIO_AGREGAR_ENTRADA_INICIAL");
    public bool CanRegisterInitialForSelection =>
        CanRegisterInitial && SelectedItem is not null;
    public decimal CorrectionSourceStock => SelectedDetail?.Bodegas
        .FirstOrDefault(x => x.BodegaId == TransferSource?.Id)?.StockActual ?? 0;
    public decimal CorrectionSourceReserved => SelectedDetail?.Bodegas
        .FirstOrDefault(x => x.BodegaId == TransferSource?.Id)?.StockReservado ?? 0;
    public decimal CorrectionAvailableStock =>
        CorrectionSourceStock - CorrectionSourceReserved;
    public bool CorrectionUsesSeries =>
        SelectedDetail?.TipoControl.Contains("SERIE") == true;
    public bool CorrectionUsesLotsOnly =>
        SelectedDetail?.TipoControl.Contains("LOTE") == true &&
        !CorrectionUsesSeries;
    public bool CorrectionUsesNormalControl =>
        !CorrectionUsesSeries && !CorrectionUsesLotsOnly;
    public decimal CorrectionQuantityToMove => CorrectionUsesSeries
        ? CorrectionSeries.Count(x => x.IsSelected)
        : CorrectionUsesLotsOnly
            ? CorrectionLots.Sum(x => x.Quantity)
            : CorrectionQuantity;
    public bool HasCorrectionLots => CorrectionLots.Count > 0;
    public bool HasCorrectionSeries => CorrectionSeries.Count > 0;
    public bool CorrectionExceedsAvailable =>
        CorrectionQuantityToMove > CorrectionAvailableStock;
    public decimal CorrectionRemainingStock =>
        CorrectionSourceStock - CorrectionQuantityToMove;
    public int CorrectionSourceLots => SelectedDetail?.Lotes.Count(x =>
        x.BodegaId == TransferSource?.Id && x.StockActual != 0) ?? 0;
    public int CorrectionSourceSeries => SelectedDetail?.Series.Count(x =>
        x.BodegaId == TransferSource?.Id && x.Estado == "DISPONIBLE") ?? 0;
    public bool AdjustmentIsOutput => AdjustmentType == "SALIDA";
    public string AdjustmentTitle => AdjustmentIsInitial
        ? "Registrar saldo inicial" : "Registrar ajuste de inventario";
    public bool AdjustmentUsesLots =>
        SelectedDetail?.TipoControl.Contains("LOTE") == true;
    public bool AdjustmentUsesSeries =>
        SelectedDetail?.TipoControl.Contains("SERIE") == true;
    public decimal AdjustmentRequiredBase => AdjustmentQuantity *
        (AdjustmentPresentation?.Factor ?? 0);
    public decimal AdjustmentAssignedLots =>
        AdjustmentLots.Sum(x => x.Quantity);
    public int AdjustmentSelectedSeries => AdjustmentSeries.Count(x =>
        AdjustmentIsOutput ? x.IsSelected : !string.IsNullOrWhiteSpace(x.Number));
    public decimal AdjustmentAssignedControl => AdjustmentUsesLots
        ? AdjustmentAssignedLots
        : AdjustmentUsesSeries ? AdjustmentSelectedSeries : AdjustmentRequiredBase;
    public decimal AdjustmentPendingControl =>
        AdjustmentRequiredBase - AdjustmentAssignedControl;
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(
        TotalItems / (double)PageSize));
    public int KardexTotalPages => Math.Max(1, (int)Math.Ceiling(
        KardexTotalItems / (double)KardexPageSize));
    public bool HasKardexItems => KardexItems.Count > 0;
    public bool HasKardexFilters => KardexFrom.HasValue || KardexTo.HasValue ||
        KardexNature != "TODAS";
    public bool IsKardexAll => KardexNature == "TODAS";
    public bool IsKardexEntry => KardexNature == "ENTRADA";
    public bool IsKardexExit => KardexNature == "SALIDA";
    public string KardexWarehouseText => SelectedWarehouse?.Display ??
        "TODAS LAS BODEGAS AUTORIZADAS";
    private long? SelectedWarehouseId => SelectedWarehouse is { Id: > 0 }
        ? SelectedWarehouse.Id
        : null;
    private bool CanKardexPrevious => !IsKardexLoading && KardexPage > 1;
    private bool CanKardexNext => !IsKardexLoading &&
        KardexPage < KardexTotalPages;
    public string KardexEmptyTitle => HasKardexFilters
        ? "No existen movimientos con estos filtros"
        : "Este producto aún no tiene movimientos";
    public string KardexEmptyDescription => HasKardexFilters
        ? "Ajusta el período o el tipo de movimiento para ampliar la consulta."
        : "Los ingresos, salidas y ajustes aparecerán aquí cuando se registren.";
    public string PaginationText => TotalItems == 0
        ? "Mostrando 0 de 0 productos"
        : $"Mostrando {(Page - 1) * PageSize + 1:N0}–{Math.Min(Page * PageSize, TotalItems):N0} de {TotalItems:N0} productos";
    public string KardexPaginationText => KardexTotalItems == 0
        ? "Mostrando 0 de 0 movimientos"
        : $"Mostrando {(KardexPage - 1) * KardexPageSize + 1:N0}–{Math.Min(KardexPage * KardexPageSize, KardexTotalItems):N0} de {KardexTotalItems:N0} movimientos";

    public InventoryViewModel(IInventoryQueryService queries,
        IInventoryTransferService transfers, IInventoryService movements,
        CurrentSession session,
        IMessageDialogService dialogs,
        ProductFormViewModel productOperations)
    {
        _queries = queries;
        _transfers = transfers;
        _movements = movements;
        _session = session;
        _dialogs = dialogs;
        ProductOperations = productOperations;
        ProductOperations.CloseRequested += CloseProductOperation;
        ProductOperations.ProductSaved += ProductOperationSaved;
        ProductOperations.PropertyChanged += ProductOperationsPropertyChanged;
        _session.EmpresaActivaChanged += OnCompanyChanged;
    }

    public Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        lock (_initializationLock)
        {
            if (_disposed) return Task.CompletedTask;
            return _initializationTask ??= InitializeCoreAsync(
                cancellationToken);
        }
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            await LoadCatalogsAsync(cancellationToken);
            await LoadAsync(cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_disposed)
                await _dialogs.ShowWarningAsync(
                    "No se pudo cargar Inventario", ex.Message);
        }
    }

    private async Task LoadCatalogsAsync(
        CancellationToken cancellationToken = default)
    {
        if (_disposed || !_session.EmpresaId.HasValue) return;
        var data = await _queries.ObtenerCatalogosAsync(_session.EmpresaId.Value,
            _session.UsuarioId, cancellationToken);
        _suppressReload = true;
        try
        {
            Warehouses.Clear();
            foreach (var item in data.Bodegas) Warehouses.Add(item);
            WarehouseFilters.Clear();
            WarehouseFilters.Add(new InventarioOpcionDto
            {
                Id = 0,
                Nombre = "TODAS LAS BODEGAS"
            });
            foreach (var item in data.Bodegas) WarehouseFilters.Add(item);
            SelectedWarehouse = WarehouseFilters[0];
        }
        finally
        {
            _suppressReload = false;
        }
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed || !_session.EmpresaId.HasValue) return;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);
        var token = _loadCancellation.Token;
        var version = ++_loadVersion;
        IsLoading = true;
        try
        {
            var result = await _queries.ListarAsync(new InventarioCatalogoRequest
            {
                EmpresaId = _session.EmpresaId.Value,
                UsuarioId = _session.UsuarioId,
                EstablecimientoId = _session.EstablecimientoId,
                BodegaId = SelectedWarehouseId,
                Busqueda = SearchText,
                Indicador = SelectedIndicator,
                Orden = InventoryOrder,
                OrdenDescendente = InventoryOrderDescending,
                Pagina = Page,
                TamanoPagina = PageSize
            }, token);
            if (version != _loadVersion) return;
            Items.Clear();
            foreach (var item in result.Items) Items.Add(item);
            TotalItems = result.Total;
            ProductsCount = result.Productos;
            NoStockCount = result.SinStock;
            LowStockCount = result.StockBajo;
            ExpiringCount = result.PorCaducar;
            InventoryValue = result.ValorInventario;
            NotifyListState();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (version == _loadVersion)
                await _dialogs.ShowWarningAsync("No se pudo cargar Inventario",
                    ex.Message);
        }
        finally
        {
            if (version == _loadVersion) IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SortInventoryAsync(InventarioCatalogoOrden order)
    {
        if (InventoryOrder == order)
            InventoryOrderDescending = !InventoryOrderDescending;
        else
        {
            InventoryOrder = order;
            InventoryOrderDescending = false;
        }

        Page = 1;
        NotifyInventorySortIndicators();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SelectKpiAsync(string? key)
    {
        _suppressReload = true;
        SelectedIndicator = key is "SIN_STOCK" or "STOCK_BAJO" or
            "POR_CADUCAR" ? key : "TODOS";
        Page = 1;
        _suppressReload = false;
        NotifyKpis();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText)) return;
        _suppressReload = true;
        SearchText = null;
        Page = 1;
        _suppressReload = false;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        _suppressReload = true;
        SearchText = null;
        SelectedWarehouse = WarehouseFilters.FirstOrDefault();
        SelectedIndicator = "TODOS";
        Page = 1;
        _suppressReload = false;
        NotifyKpis();
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ViewDetailAsync(InventarioItemDto? item)
    {
        item ??= SelectedItem;
        if (item is null || !_session.EmpresaId.HasValue) return;
        SelectedDetailTabIndex = 0;
        SelectedDetail = await _queries.ObtenerDetalleAsync(
            _session.EmpresaId.Value, _session.UsuarioId, item.ProductoId);
        IsDetailOpen = SelectedDetail is not null;
    }

    [RelayCommand]
    private async Task OpenKardexAsync(InventarioItemDto? item)
    {
        item ??= SelectedItem;
        if (item is null || !_session.EmpresaId.HasValue) return;
        SelectedItem = item;
        KardexFrom = null;
        KardexTo = null;
        KardexFromText = string.Empty;
        KardexToText = string.Empty;
        KardexNature = "TODAS";
        KardexPage = 1;
        SelectedKardexItem = null;
        NotifyKardexState();
        IsKardexOpen = true;
        await LoadKardexAsync();
    }

    [RelayCommand]
    private async Task LoadKardexAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_session.EmpresaId.HasValue || SelectedItem is null) return;
        _kardexCancellation?.Cancel();
        _kardexCancellation?.Dispose();
        _kardexCancellation = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken);
        var token = _kardexCancellation.Token;
        var version = ++_kardexLoadVersion;
        IsKardexLoading = true;
        try
        {
            var result = await _queries.ObtenerKardexPaginadoAsync(
                new KardexPaginadoRequest
                {
                    EmpresaId = _session.EmpresaId.Value,
                    UsuarioId = _session.UsuarioId,
                    ProductoId = SelectedItem.ProductoId,
                    EstablecimientoId = _session.EstablecimientoId,
                    BodegaId = SelectedWarehouseId,
                    Desde = KardexFrom.HasValue
                        ? DateOnly.FromDateTime(KardexFrom.Value) : null,
                    Hasta = KardexTo.HasValue
                        ? DateOnly.FromDateTime(KardexTo.Value) : null,
                    Naturaleza = KardexNature,
                    FechaDescendente = KardexDateDescending,
                    Pagina = KardexPage,
                    TamanoPagina = KardexPageSize
                }, token);
            if (version != _kardexLoadVersion) return;
            KardexItems.Clear();
            foreach (var item in result.Items) KardexItems.Add(item);
            SelectedKardexItem = null;
            KardexTotalItems = result.Total;
            NotifyKardexState();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (version == _kardexLoadVersion)
                await _dialogs.ShowWarningAsync("No se pudo cargar el Kardex",
                    ex.Message);
        }
        finally
        {
            if (version == _kardexLoadVersion) IsKardexLoading = false;
        }
    }

    public string KardexDateSortIndicator =>
        KardexDateDescending ? "▼" : "▲";

    [RelayCommand]
    private async Task SortKardexByDateAsync()
    {
        KardexDateDescending = !KardexDateDescending;
        KardexPage = 1;
        OnPropertyChanged(nameof(KardexDateSortIndicator));
        await LoadKardexAsync();
    }

    [RelayCommand]
    private async Task ApplyKardexFiltersAsync()
    {
        if (!TryParseKardexDate(KardexFromText, out var from))
        {
            await _dialogs.ShowWarningAsync("Fecha Desde incorrecta",
                "Ingresa la fecha Desde con el formato dd/mm/aaaa.");
            return;
        }
        if (!TryParseKardexDate(KardexToText, out var to))
        {
            await _dialogs.ShowWarningAsync("Fecha Hasta incorrecta",
                "Ingresa la fecha Hasta con el formato dd/mm/aaaa.");
            return;
        }
        KardexFrom = from;
        KardexTo = to;
        if (KardexFrom.HasValue && KardexTo.HasValue &&
            KardexFrom.Value.Date > KardexTo.Value.Date)
        {
            await _dialogs.ShowWarningAsync("Período incorrecto",
                "La fecha Desde no puede ser posterior a la fecha Hasta.");
            return;
        }
        KardexPage = 1;
        await LoadKardexAsync();
    }

    [RelayCommand]
    private async Task ClearKardexFiltersAsync()
    {
        KardexFrom = null;
        KardexTo = null;
        KardexFromText = string.Empty;
        KardexToText = string.Empty;
        KardexNature = "TODAS";
        KardexPage = 1;
        NotifyKardexState();
        await LoadKardexAsync();
    }

    [RelayCommand]
    private void ClearKardexFrom()
    {
        KardexFrom = null;
        KardexFromText = string.Empty;
    }

    [RelayCommand]
    private void ClearKardexTo()
    {
        KardexTo = null;
        KardexToText = string.Empty;
    }

    [RelayCommand]
    private void CollapseKardexDetail()
    {
        SelectedKardexItem = null;
    }

    [RelayCommand]
    private async Task SelectKardexNatureAsync(string? nature)
    {
        var selected = nature is "ENTRADA" or "SALIDA"
            ? nature : "TODAS";
        if (KardexNature == selected) return;
        KardexNature = selected;
        KardexPage = 1;
        await LoadKardexAsync();
    }

    private static bool TryParseKardexDate(string? text, out DateTime? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(text)) return true;
        if (!DateTime.TryParseExact(text.Trim(), "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.None,
                out var parsed))
            return false;
        value = parsed;
        return true;
    }

    private bool CanOpenInitialWarehouseCorrection(InventarioItemDto? item) =>
        CanCorrectInitialWarehouse &&
        (item ?? SelectedItem)?.PuedeCompletarInventarioInicial == true;

    [RelayCommand(CanExecute = nameof(CanOpenInitialWarehouseCorrection))]
    private async Task OpenInitialWarehouseCorrectionAsync(
        InventarioItemDto? item)
    {
        item ??= SelectedItem;
        if (item is null || !_session.EmpresaId.HasValue) return;
        if (!CanCorrectInitialWarehouse)
        {
            await _dialogs.ShowWarningAsync("Acción no autorizada",
                "No tienes permiso para corregir el inventario inicial.");
            return;
        }
        if (!item.PuedeCompletarInventarioInicial)
        {
            await _dialogs.ShowWarningAsync("Corrección no disponible",
                "El producto ya tiene movimientos posteriores al inventario inicial. Utiliza un ajuste trazable.");
            return;
        }
        SelectedItem = item;
        SelectedDetail = await _queries.ObtenerDetalleAsync(
            _session.EmpresaId.Value, _session.UsuarioId, item.ProductoId);
        if (SelectedDetail is null) return;
        CorrectionSourceWarehouses.Clear();
        foreach (var warehouse in Warehouses.Where(x =>
            x.EstablecimientoId == _session.EstablecimientoId &&
            SelectedDetail.Bodegas.Any(b => b.BodegaId == x.Id &&
                b.StockActual > 0)))
            CorrectionSourceWarehouses.Add(warehouse);
        if (CorrectionSourceWarehouses.Count == 0)
        {
            await _dialogs.ShowWarningAsync("Sin stock para corregir",
                "El producto no tiene stock positivo en las bodegas del establecimiento activo.");
            return;
        }
        TransferSource = CorrectionSourceWarehouses.FirstOrDefault(x =>
            x.Id == _session.BodegaId) ?? CorrectionSourceWarehouses[0];
        RefreshCorrectionSource();
        if (TransferTarget is null)
        {
            await _dialogs.ShowWarningAsync("Sin bodega de destino",
                "No existe otra bodega activa en el establecimiento para realizar la corrección.");
            return;
        }
        TransferDate = DateTime.Today;
        TransferReason = string.Empty;
        IsInitialWarehouseCorrectionOpen = true;
        NotifyCorrectionState();
    }

    [RelayCommand]
    private async Task SaveInitialWarehouseCorrectionAsync()
    {
        if (!_session.EmpresaId.HasValue || SelectedDetail is null ||
            TransferSource is null || TransferTarget is null) return;
        if (string.IsNullOrWhiteSpace(TransferReason) ||
            TransferReason.Trim().Length < 5)
        {
            await _dialogs.ShowWarningAsync("Motivo obligatorio",
                "Explica la corrección con al menos 5 caracteres.");
            return;
        }
        var quantityToMove = CorrectionQuantityToMove;
        if (quantityToMove <= 0)
        {
            await _dialogs.ShowWarningAsync("Cantidad obligatoria",
                CorrectionUsesSeries
                    ? "Selecciona al menos una serie para corregir."
                    : CorrectionUsesLotsOnly
                        ? "Distribuye una cantidad mayor que cero entre los lotes."
                        : "Ingresa una cantidad mayor que cero.");
            return;
        }
        if (quantityToMove > CorrectionAvailableStock)
        {
            await _dialogs.ShowWarningAsync("Cantidad no disponible",
                $"Solo existen {CorrectionAvailableStock:0.######} unidades disponibles para corregir.");
            return;
        }
        IsTransferSaving = true;
        try
        {
            var result = await _transfers.CorregirBodegaInventarioInicialAsync(
                new CorreccionBodegaInventarioInicialRequest
                {
                    EmpresaId = _session.EmpresaId.Value,
                    UsuarioId = _session.UsuarioId,
                    ProductoId = SelectedDetail.ProductoId,
                    BodegaOrigenId = TransferSource.Id,
                    BodegaDestinoId = TransferTarget.Id,
                    CantidadBase = quantityToMove,
                    Fecha = TransferDate,
                    Motivo = TransferReason,
                    Lotes = CorrectionUsesLotsOnly
                        ? CorrectionLots.Where(x => x.Quantity > 0)
                            .Select(x => x.ToRequest()).ToList()
                        : [],
                    Series = CorrectionUsesSeries
                        ? CorrectionSeries.Where(x => x.IsSelected)
                            .Select(x => x.ToRequest()).ToList()
                        : []
                });
            if (!result.Success)
            {
                await _dialogs.ShowWarningAsync(
                    "No se pudo corregir la bodega", result.Message);
                return;
            }
            await _dialogs.ShowSuccessAsync("Bodega inicial corregida",
                result.Message);
            IsInitialWarehouseCorrectionOpen = false;
            await LoadAsync();
        }
        finally { IsTransferSaving = false; }
    }

    [RelayCommand]
    private async Task OpenAdjustmentAsync(InventarioItemDto? item)
    {
        item ??= SelectedItem;
        if (!CanAdjust) return;
        if (item is null)
        {
            await _dialogs.ShowWarningAsync(
                "Selecciona un producto",
                "Debes seleccionar un producto de la lista antes de registrar un nuevo ajuste de inventario.");
            return;
        }
        await OpenApprovedProductOperationAsync(item, false);
    }

    private bool CanOpenInitial(InventarioItemDto? item) =>
        CanRegisterInitial && (item ?? SelectedItem) is not null;

    [RelayCommand(CanExecute = nameof(CanOpenInitial))]
    private async Task OpenInitialAsync(InventarioItemDto? item)
    {
        item ??= SelectedItem;
        if (item is null || !CanOpenInitial(item)) return;
        if (!item.PuedeCompletarInventarioInicial)
        {
            await _dialogs.ShowWarningAsync(
                "Saldo inicial no disponible",
                "El producto ya tiene movimientos operativos confirmados. " +
                "Para corregir o completar sus existencias utiliza Nuevo ajuste.");
            return;
        }
        await OpenApprovedProductOperationAsync(item, true);
    }

    private async Task OpenApprovedProductOperationAsync(
        InventarioItemDto item, bool initial)
    {
        await ProductOperations.EditarAsync(item.ProductoId);
        IsProductOperationOpen = true;
        if (initial)
        {
            ProductOperations.AgregarEntradaInicialExistenteCommand.Execute(null);
            ProductOperations.SolicitarFocoInventarioOperativo();
            return;
        }

        _productAdjustmentWasOpened = true;
        await ProductOperations.AbrirAjusteCommand.ExecuteAsync(null);
    }

    private void CloseProductOperation()
    {
        _productAdjustmentWasOpened = false;
        IsProductOperationOpen = false;
    }

    private void ProductOperationSaved(long productId)
    {
        CloseProductOperation();
        _ = LoadAsync();
    }

    private void ProductOperationsPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProductFormViewModel.IsAjusteOpen) ||
            !_productAdjustmentWasOpened || ProductOperations.IsAjusteOpen)
            return;
        CloseProductOperation();
        _ = LoadAsync();
    }

    private async Task PrepareAdjustmentAsync(InventarioItemDto? item,
        bool initial)
    {
        item ??= SelectedItem;
        if (item is null || !_session.EmpresaId.HasValue) return;
        if (initial ? !CanRegisterInitial : !CanAdjust)
        {
            await _dialogs.ShowWarningAsync("Acción no autorizada",
                initial
                    ? "No tienes permiso para registrar inventario inicial."
                    : "No tienes permiso para registrar ajustes de inventario.");
            return;
        }
        SelectedItem = item;
        SelectedDetail = await _queries.ObtenerDetalleAsync(
            _session.EmpresaId.Value, _session.UsuarioId, item.ProductoId);
        if (SelectedDetail is null) return;
        AdjustmentIsInitial = initial;
        AdjustmentType = "ENTRADA";
        AdjustmentWarehouse = Warehouses.FirstOrDefault(x =>
            x.Id == (_session.BodegaId ?? SelectedWarehouse?.Id)) ??
            Warehouses.FirstOrDefault();
        AdjustmentPresentation = SelectedDetail.Presentaciones
            .FirstOrDefault(x => x.EsBase) ??
            SelectedDetail.Presentaciones.FirstOrDefault();
        AdjustmentQuantity = 0;
        AdjustmentCostTotal = 0;
        AdjustmentDate = DateTime.Today;
        AdjustmentJustification = initial ? "SALDO INICIAL" : string.Empty;
        AdjustmentObservation = null;
        await LoadAdjustmentReasonsAsync();
        RefreshAdjustmentControl();
        IsAdjustmentOpen = true;
        NotifyAdjustmentState();
    }

    [RelayCommand]
    private void AddAdjustmentLot()
    {
        var row = new InventoryAdjustmentLotRow();
        row.PropertyChanged += (_, _) => NotifyAdjustmentState();
        AdjustmentLots.Add(row);
        NotifyAdjustmentState();
    }

    [RelayCommand]
    private void RemoveAdjustmentLot(InventoryAdjustmentLotRow? row)
    {
        if (row is not null) AdjustmentLots.Remove(row);
        NotifyAdjustmentState();
    }

    [RelayCommand]
    private void AddAdjustmentSeries()
    {
        var row = new InventoryAdjustmentSeriesRow();
        row.PropertyChanged += (_, _) => NotifyAdjustmentState();
        AdjustmentSeries.Add(row);
        NotifyAdjustmentState();
    }

    [RelayCommand]
    private void RemoveAdjustmentSeries(InventoryAdjustmentSeriesRow? row)
    {
        if (row is not null) AdjustmentSeries.Remove(row);
        NotifyAdjustmentState();
    }

    [RelayCommand]
    private async Task SaveAdjustmentAsync()
    {
        if (!_session.EmpresaId.HasValue || SelectedDetail is null ||
            AdjustmentWarehouse is null || AdjustmentPresentation is null)
            return;
        var validation = ValidateAdjustment();
        if (validation is not null)
        {
            await _dialogs.ShowWarningAsync("Revisa la operación", validation);
            return;
        }

        var chosenSeries = AdjustmentSeries.Where(x => AdjustmentIsOutput
                ? x.IsSelected : !string.IsNullOrWhiteSpace(x.Number))
            .ToList();
        List<IngresoInventarioLoteRequest> lots;
        if (AdjustmentUsesLots && AdjustmentUsesSeries)
        {
            lots = chosenSeries.GroupBy(x => x.Lot!.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var source = AdjustmentLots.First(x => x.Number.Equals(
                        g.Key, StringComparison.OrdinalIgnoreCase));
                    return source.ToRequest(g.Count());
                }).ToList();
        }
        else
        {
            lots = AdjustmentLots.Where(x => x.Quantity > 0)
                .Select(x => x.ToRequest()).ToList();
        }
        var detail = new IngresoInventarioDetalleRequest
        {
            ProductoId = SelectedDetail.ProductoId,
            ProductoPresentacionId = AdjustmentPresentation.Id,
            Cantidad = AdjustmentQuantity,
            CostoTotal = AdjustmentIsOutput
                ? SelectedDetail.CostoPromedio * AdjustmentRequiredBase
                : AdjustmentCostTotal,
            Lotes = lots,
            Series = chosenSeries.Select(x =>
                new IngresoInventarioSerieRequest
                {
                    NumeroSerie = x.Number.Trim(),
                    NumeroLote = string.IsNullOrWhiteSpace(x.Lot)
                        ? null : x.Lot.Trim()
                }).ToList(),
            Observacion = AdjustmentObservation
        };

        IsAdjustmentSaving = true;
        try
        {
            InventoryOperationResult result;
            if (AdjustmentIsInitial)
            {
                result = await _movements.RegistrarIngresoInicialAsync(
                    new IngresoInventarioRequest
                    {
                        EmpresaId = _session.EmpresaId.Value,
                        BodegaId = AdjustmentWarehouse.Id,
                        UsuarioId = _session.UsuarioId,
                        FechaMovimiento = AdjustmentDate,
                        Referencia = AdjustmentJustification,
                        Observacion = AdjustmentObservation,
                        Detalles = [detail]
                    });
            }
            else
            {
                result = await _movements.RegistrarAjusteAsync(
                    new AjusteInventarioRequest
                    {
                        EmpresaId = _session.EmpresaId.Value,
                        EstablecimientoId =
                            AdjustmentWarehouse.EstablecimientoId ?? 0,
                        BodegaId = AdjustmentWarehouse.Id,
                        UsuarioId = _session.UsuarioId,
                        TipoAjuste = AdjustmentType,
                        Fecha = AdjustmentDate,
                        MotivoOperacionInventarioId = AdjustmentReason!.Id,
                        Motivo = AdjustmentJustification,
                        Observacion = AdjustmentObservation,
                        Detalles = [detail]
                    });
            }
            if (!result.Success)
            {
                await _dialogs.ShowWarningAsync("No se pudo confirmar",
                    result.Message);
                return;
            }
            await _dialogs.ShowSuccessAsync("Inventario actualizado",
                result.Message);
            IsAdjustmentOpen = false;
            await LoadAsync();
        }
        finally { IsAdjustmentSaving = false; }
    }

    private string? ValidateAdjustment()
    {
        if (AdjustmentWarehouse is null || AdjustmentPresentation is null ||
            AdjustmentQuantity <= 0)
            return "Selecciona bodega y presentación e ingresa una cantidad mayor que cero.";
        if (!AdjustmentIsInitial && AdjustmentReason is null)
            return "Selecciona el motivo del ajuste.";
        if (string.IsNullOrWhiteSpace(AdjustmentJustification) ||
            AdjustmentJustification.Trim().Length < 5)
            return "Explica la justificación con al menos 5 caracteres.";
        if (!AdjustmentIsOutput && AdjustmentCostTotal < 0)
            return "El costo total no puede ser negativo.";
        if (AdjustmentIsOutput)
        {
            var available = SelectedDetail?.Bodegas.FirstOrDefault(x =>
                x.BodegaId == AdjustmentWarehouse.Id)?.Disponible ?? 0;
            if (AdjustmentRequiredBase > available)
                return $"La bodega dispone de {available:0.######} unidades base y se solicitaron {AdjustmentRequiredBase:0.######}.";
        }
        if (AdjustmentUsesSeries &&
            AdjustmentRequiredBase != decimal.Truncate(AdjustmentRequiredBase))
            return "Los productos serializados requieren una cantidad base entera.";
        if (AdjustmentUsesSeries &&
            AdjustmentSelectedSeries != AdjustmentRequiredBase)
            return $"Registra o selecciona exactamente {AdjustmentRequiredBase:0} series.";
        if (AdjustmentUsesLots && !AdjustmentUsesSeries &&
            AdjustmentAssignedLots != AdjustmentRequiredBase)
            return $"Distribuye exactamente {AdjustmentRequiredBase:0.######} unidades base entre los lotes.";
        if (AdjustmentUsesLots && AdjustmentLots.Any(x =>
                string.IsNullOrWhiteSpace(x.Number)))
            return "Todos los lotes deben tener un número.";
        if (AdjustmentUsesSeries && AdjustmentSeries
                .Where(x => AdjustmentIsOutput ? x.IsSelected :
                    !string.IsNullOrWhiteSpace(x.Number))
                .GroupBy(x => x.Number.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(x => x.Count() > 1))
            return "Los números de serie no pueden repetirse.";
        if (AdjustmentUsesLots && AdjustmentUsesSeries &&
            AdjustmentSeries.Where(x => AdjustmentIsOutput ? x.IsSelected :
                    !string.IsNullOrWhiteSpace(x.Number))
                .Any(x => string.IsNullOrWhiteSpace(x.Lot) ||
                    !AdjustmentLots.Any(l => l.Number.Equals(x.Lot.Trim(),
                        StringComparison.OrdinalIgnoreCase))))
            return "Cada serie debe estar asociada a uno de los lotes de la operación.";
        return null;
    }

    private async Task LoadAdjustmentReasonsAsync()
    {
        AdjustmentReasons.Clear();
        AdjustmentReason = null;
        if (AdjustmentIsInitial || !_session.EmpresaId.HasValue) return;
        var type = AdjustmentType == "SALIDA"
            ? "AJUSTE_SALIDA" : "AJUSTE_ENTRADA";
        foreach (var reason in await _movements.ObtenerMotivosOperacionAsync(
                     _session.EmpresaId.Value, _session.UsuarioId, type))
            AdjustmentReasons.Add(reason);
        AdjustmentReason = AdjustmentReasons.FirstOrDefault();
    }

    [RelayCommand]
    private async Task ReconcileAsync()
    {
        if (!_session.EmpresaId.HasValue) return;
        try
        {
            var result = await _queries.ReconciliarAsync(
                _session.EmpresaId.Value, _session.UsuarioId);
            if (result.EsConsistente)
                await _dialogs.ShowSuccessAsync("Inventario consistente",
                    "Existencias, lotes y series no presentan diferencias.");
            else
                await _dialogs.ShowWarningAsync("Revisión necesaria",
                    $"Se encontraron {result.Hallazgos.Count} diferencias. " +
                    string.Join(Environment.NewLine,
                        result.Hallazgos.Take(8).Select(x =>
                            $"• {x.Producto}: {x.Descripcion}")));
        }
        catch (Exception ex)
        {
            await _dialogs.ShowWarningAsync("No se pudo reconciliar",
                ex.Message);
        }
    }

    [RelayCommand]
    private void ClosePanels()
    {
        CloseProductOperation();
        _kardexCancellation?.Cancel();
        SelectedDetailTabIndex = 0;
        SelectedKardexItem = null;
        IsDetailOpen = false;
        IsKardexOpen = false;
        IsInitialWarehouseCorrectionOpen = false;
        IsAdjustmentOpen = false;
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (Page <= 1) return;
        Page--;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (Page >= TotalPages) return;
        Page++;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task GoToPageAsync(InventoryPageItem? item)
    {
        if (item is null || item.IsSeparator || item.Number == Page) return;
        Page = item.Number;
        await LoadAsync();
    }

    [RelayCommand(CanExecute = nameof(CanKardexPrevious))]
    private async Task KardexPreviousPageAsync()
    {
        if (KardexPage <= 1) return;
        KardexPage--;
        await LoadKardexAsync();
    }

    [RelayCommand(CanExecute = nameof(CanKardexNext))]
    private async Task KardexNextPageAsync()
    {
        if (KardexPage >= KardexTotalPages) return;
        KardexPage++;
        await LoadKardexAsync();
    }

    [RelayCommand]
    private async Task GoToKardexPageAsync(InventoryPageItem? item)
    {
        if (item is null || item.IsSeparator || item.Number == KardexPage) return;
        KardexPage = item.Number;
        await LoadKardexAsync();
    }

    partial void OnSearchTextChanged(string? value)
    {
        if (!_suppressReload) DebounceSearch();
    }
    partial void OnSelectedWarehouseChanged(InventarioOpcionDto? value)
    {
        OnPropertyChanged(nameof(KardexWarehouseText));
        if (!_suppressReload) { Page = 1; _ = LoadAsync(); }
    }

    partial void OnSelectedItemChanged(InventarioItemDto? value)
    {
        OnPropertyChanged(nameof(CanRegisterInitialForSelection));
        OpenInitialCommand.NotifyCanExecuteChanged();
        OpenInitialWarehouseCorrectionCommand.NotifyCanExecuteChanged();
    }
    partial void OnPageSizeChanged(int value)
    {
        Page = 1;
        _ = LoadAsync();
    }
    partial void OnKardexPageSizeChanged(int value)
    {
        if (!IsKardexOpen) return;
        KardexPage = 1;
        _ = LoadKardexAsync();
    }
    partial void OnKardexNatureChanged(string value) => NotifyKardexState();
    partial void OnIsKardexLoadingChanged(bool value)
    {
        KardexPreviousPageCommand.NotifyCanExecuteChanged();
        KardexNextPageCommand.NotifyCanExecuteChanged();
    }
    partial void OnTransferSourceChanged(InventarioOpcionDto? value) =>
        RefreshCorrectionSource();
    partial void OnCorrectionQuantityChanged(decimal value) =>
        NotifyCorrectionState();
    partial void OnAdjustmentWarehouseChanged(InventarioOpcionDto? value) =>
        RefreshAdjustmentControl();
    partial void OnAdjustmentPresentationChanged(
        InventarioPresentacionDto? value) => NotifyAdjustmentState();
    partial void OnAdjustmentQuantityChanged(decimal value) =>
        NotifyAdjustmentState();
    partial void OnAdjustmentTypeChanged(string value)
    {
        if (AdjustmentIsInitial) return;
        _ = LoadAdjustmentReasonsAsync();
        RefreshAdjustmentControl();
        NotifyAdjustmentState();
    }
    partial void OnAdjustmentIsInitialChanged(bool value) =>
        OnPropertyChanged(nameof(AdjustmentTitle));

    private async void DebounceSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = new CancellationTokenSource();
        try
        {
            await Task.Delay(300, _searchCancellation.Token);
            Page = 1;
            await LoadAsync();
        }
        catch (OperationCanceledException) { }
    }

    private void RefreshCorrectionTargets()
    {
        CorrectionTargetWarehouses.Clear();
        if (TransferSource is null)
        {
            TransferTarget = null;
            NotifyCorrectionState();
            return;
        }
        foreach (var warehouse in Warehouses.Where(x =>
            x.EstablecimientoId == TransferSource.EstablecimientoId &&
            x.Id != TransferSource.Id))
            CorrectionTargetWarehouses.Add(warehouse);
        if (TransferTarget is null ||
            CorrectionTargetWarehouses.All(x => x.Id != TransferTarget.Id))
            TransferTarget = CorrectionTargetWarehouses.FirstOrDefault();
        NotifyCorrectionState();
    }

    private void RefreshCorrectionSource()
    {
        CorrectionLots.Clear();
        CorrectionSeries.Clear();
        CorrectionQuantity = 0;
        RefreshCorrectionTargets();
        if (SelectedDetail is null || TransferSource is null) return;
        if (CorrectionUsesSeries)
        {
            foreach (var series in SelectedDetail.Series.Where(x =>
                x.BodegaId == TransferSource.Id && x.Estado == "DISPONIBLE"))
            {
                var row = new InventoryCorrectionSeriesRow(series);
                row.PropertyChanged += (_, _) => NotifyCorrectionState();
                CorrectionSeries.Add(row);
            }
        }
        else if (CorrectionUsesLotsOnly)
        {
            foreach (var lot in SelectedDetail.Lotes.Where(x =>
                x.BodegaId == TransferSource.Id && x.Disponible > 0))
            {
                var row = new InventoryCorrectionLotRow(lot);
                row.PropertyChanged += (_, _) => NotifyCorrectionState();
                CorrectionLots.Add(row);
            }
        }
        NotifyCorrectionState();
    }

    private void NotifyCorrectionState()
    {
        OnPropertyChanged(nameof(CorrectionSourceStock));
        OnPropertyChanged(nameof(CorrectionSourceReserved));
        OnPropertyChanged(nameof(CorrectionAvailableStock));
        OnPropertyChanged(nameof(CorrectionSourceLots));
        OnPropertyChanged(nameof(CorrectionSourceSeries));
        OnPropertyChanged(nameof(CorrectionUsesSeries));
        OnPropertyChanged(nameof(CorrectionUsesLotsOnly));
        OnPropertyChanged(nameof(CorrectionUsesNormalControl));
        OnPropertyChanged(nameof(CorrectionQuantityToMove));
        OnPropertyChanged(nameof(HasCorrectionLots));
        OnPropertyChanged(nameof(HasCorrectionSeries));
        OnPropertyChanged(nameof(CorrectionExceedsAvailable));
        OnPropertyChanged(nameof(CorrectionRemainingStock));
    }

    private void RefreshAdjustmentControl()
    {
        AdjustmentLots.Clear();
        AdjustmentSeries.Clear();
        if (SelectedDetail is null || AdjustmentWarehouse is null) return;
        if (AdjustmentIsOutput)
        {
            foreach (var lot in SelectedDetail.Lotes.Where(x =>
                x.BodegaId == AdjustmentWarehouse.Id &&
                x.StockActual - x.StockReservado > 0))
            {
                var row = new InventoryAdjustmentLotRow(lot);
                row.PropertyChanged += (_, _) => NotifyAdjustmentState();
                AdjustmentLots.Add(row);
            }
            foreach (var series in SelectedDetail.Series.Where(x =>
                x.BodegaId == AdjustmentWarehouse.Id &&
                x.Estado == "DISPONIBLE"))
            {
                var row = new InventoryAdjustmentSeriesRow(series);
                row.PropertyChanged += (_, _) => NotifyAdjustmentState();
                AdjustmentSeries.Add(row);
            }
        }
        else
        {
            if (AdjustmentUsesLots) AddAdjustmentLot();
            if (AdjustmentUsesSeries) AddAdjustmentSeries();
        }
        NotifyAdjustmentState();
    }

    private void NotifyAdjustmentState()
    {
        OnPropertyChanged(nameof(AdjustmentIsOutput));
        OnPropertyChanged(nameof(AdjustmentTitle));
        OnPropertyChanged(nameof(AdjustmentUsesLots));
        OnPropertyChanged(nameof(AdjustmentUsesSeries));
        OnPropertyChanged(nameof(AdjustmentRequiredBase));
        OnPropertyChanged(nameof(AdjustmentAssignedLots));
        OnPropertyChanged(nameof(AdjustmentSelectedSeries));
        OnPropertyChanged(nameof(AdjustmentAssignedControl));
        OnPropertyChanged(nameof(AdjustmentPendingControl));
    }

    private void NotifyListState()
    {
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(TotalPages));
        OnPropertyChanged(nameof(PaginationText));
        BuildPages(VisiblePages, Page, TotalPages);
        NotifyKpis();
    }

    public string ProductSortIndicator => InventorySortIndicator(
        InventarioCatalogoOrden.Producto);
    public string StockSortIndicator => InventorySortIndicator(
        InventarioCatalogoOrden.Stock);
    public string ReservedSortIndicator => InventorySortIndicator(
        InventarioCatalogoOrden.Reservado);
    public string AvailableSortIndicator => InventorySortIndicator(
        InventarioCatalogoOrden.Disponible);
    public string AverageCostSortIndicator => InventorySortIndicator(
        InventarioCatalogoOrden.CostoPromedio);
    public string ValueSortIndicator => InventorySortIndicator(
        InventarioCatalogoOrden.Valor);

    private string InventorySortIndicator(InventarioCatalogoOrden order) =>
        InventoryOrder != order
            ? string.Empty
            : InventoryOrderDescending ? "▼" : "▲";

    private void NotifyInventorySortIndicators()
    {
        OnPropertyChanged(nameof(ProductSortIndicator));
        OnPropertyChanged(nameof(StockSortIndicator));
        OnPropertyChanged(nameof(ReservedSortIndicator));
        OnPropertyChanged(nameof(AvailableSortIndicator));
        OnPropertyChanged(nameof(AverageCostSortIndicator));
        OnPropertyChanged(nameof(ValueSortIndicator));
    }

    private void NotifyKpis()
    {
        OnPropertyChanged(nameof(IsAllKpi));
        OnPropertyChanged(nameof(IsNoStockKpi));
        OnPropertyChanged(nameof(IsLowStockKpi));
        OnPropertyChanged(nameof(IsExpiringKpi));
    }

    private void NotifyKardexState()
    {
        OnPropertyChanged(nameof(HasKardexItems));
        OnPropertyChanged(nameof(HasKardexFilters));
        OnPropertyChanged(nameof(IsKardexAll));
        OnPropertyChanged(nameof(IsKardexEntry));
        OnPropertyChanged(nameof(IsKardexExit));
        OnPropertyChanged(nameof(KardexEmptyTitle));
        OnPropertyChanged(nameof(KardexEmptyDescription));
        OnPropertyChanged(nameof(KardexTotalPages));
        OnPropertyChanged(nameof(KardexPaginationText));
        BuildPages(KardexVisiblePages, KardexPage, KardexTotalPages);
        KardexPreviousPageCommand.NotifyCanExecuteChanged();
        KardexNextPageCommand.NotifyCanExecuteChanged();
    }

    private static void BuildPages(ObservableCollection<InventoryPageItem> target,
        int current, int total)
    {
        target.Clear();
        var pages = new SortedSet<int>
        { 1, total, Math.Max(1, current - 2), Math.Max(1, current - 1),
          current, Math.Min(total, current + 1), Math.Min(total, current + 2) };
        var previous = 0;
        foreach (var number in pages.Where(x => x > 0 && x <= total))
        {
            if (previous > 0 && number - previous > 1)
                target.Add(InventoryPageItem.Separator());
            target.Add(new InventoryPageItem(number, number == current));
            previous = number;
        }
    }

    private async void OnCompanyChanged(object? sender,
        EmpresaActivaChangedEventArgs e)
    {
        ClosePanels();
        try
        {
            _loadCancellation?.Cancel();
            await LoadCatalogsAsync();
            await LoadAsync();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            if (!_disposed)
                await _dialogs.ShowWarningAsync(
                    "No se pudo cambiar de empresa", ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ProductOperations.CloseRequested -= CloseProductOperation;
        ProductOperations.ProductSaved -= ProductOperationSaved;
        ProductOperations.PropertyChanged -= ProductOperationsPropertyChanged;
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _kardexCancellation?.Cancel();
        _kardexCancellation?.Dispose();
        _session.EmpresaActivaChanged -= OnCompanyChanged;
    }
}

public sealed class InventoryPageItem
{
    public int Number { get; }
    public string Text { get; }
    public bool IsCurrent { get; }
    public bool IsSeparator { get; }
    public InventoryPageItem(int number, bool current)
    { Number = number; Text = number.ToString(); IsCurrent = current; }
    private InventoryPageItem() { Text = "…"; IsSeparator = true; }
    public static InventoryPageItem Separator() => new();
}

public partial class InventoryCorrectionLotRow : ObservableObject
{
    public string Number { get; }
    public decimal Available { get; }
    public DateOnly? Manufacturing { get; }
    public DateOnly? Expiration { get; }
    [ObservableProperty] private decimal quantity;

    public InventoryCorrectionLotRow(InventarioLoteDetalleDto source)
    {
        Number = source.Numero;
        Available = source.Disponible;
        Manufacturing = source.Elaboracion;
        Expiration = source.Caducidad;
    }

    public IngresoInventarioLoteRequest ToRequest() => new()
    {
        NumeroLote = Number,
        CantidadBase = Quantity,
        FechaElaboracion = Manufacturing,
        FechaCaducidad = Expiration
    };
}

public partial class InventoryCorrectionSeriesRow : ObservableObject
{
    public string Number { get; }
    public string? Lot { get; }
    [ObservableProperty] private bool isSelected;

    public InventoryCorrectionSeriesRow(InventarioSerieDetalleDto source)
    {
        Number = source.Numero;
        Lot = source.Lote;
    }

    public IngresoInventarioSerieRequest ToRequest() => new()
    {
        NumeroSerie = Number,
        NumeroLote = Lot
    };
}

public partial class InventoryAdjustmentLotRow : ObservableObject
{
    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty] private decimal quantity;
    [ObservableProperty] private DateOnly? manufacturing;
    [ObservableProperty] private DateOnly? expiration;
    public decimal Available { get; }
    public bool IsExisting { get; }

    public InventoryAdjustmentLotRow() { }

    public InventoryAdjustmentLotRow(InventarioLoteDetalleDto source)
    {
        Number = source.Numero;
        Available = source.StockActual - source.StockReservado;
        Manufacturing = source.Elaboracion;
        Expiration = source.Caducidad;
        IsExisting = true;
    }

    public IngresoInventarioLoteRequest ToRequest(
        decimal? quantityOverride = null) => new()
    {
        NumeroLote = Number.Trim(),
        CantidadBase = quantityOverride ?? Quantity,
        FechaElaboracion = Manufacturing,
        FechaCaducidad = Expiration
    };
}

public partial class InventoryAdjustmentSeriesRow : ObservableObject
{
    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty] private string? lot;
    [ObservableProperty] private bool isSelected;
    public bool IsExisting { get; }

    public InventoryAdjustmentSeriesRow() { }

    public InventoryAdjustmentSeriesRow(InventarioSerieDetalleDto source)
    {
        Number = source.Numero;
        Lot = source.Lote;
        IsExisting = true;
    }
}
