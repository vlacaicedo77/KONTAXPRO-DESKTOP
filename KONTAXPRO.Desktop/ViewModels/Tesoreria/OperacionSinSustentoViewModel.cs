using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Models.Tesoreria;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.ViewModels.Products;

namespace KONTAXPRO.Desktop.ViewModels.Tesoreria;

public partial class OperacionSinSustentoViewModel : ObservableObject
{
    private readonly IOperacionSinSustentoService _service;
    private readonly ICompraProductoResolverService _resolver;
    private readonly IInventoryService _inventoryService;
    private readonly IProductService _productService;
    private readonly CurrentSession _session;
    private readonly IMessageDialogService _dialogs;
    private long? _correctionOfId;
    private OperacionSinSustentoInventarioLineaViewModel? _creatingProductFor;

    public event Action? CloseRequested;
    public event Action? Saved;
    public event Func<Task>? SelectEvidenceRequested;

    public ObservableCollection<FondoSalidaDto> CashSessions { get; } = [];
    public ObservableCollection<FondoSalidaDto> BankAccounts { get; } = [];
    public ObservableCollection<FondoSalidaDto> Warehouses { get; } = [];
    public ObservableCollection<CuentaGastoDto> ExpenseAccounts { get; } = [];
    public ObservableCollection<OperacionSinSustentoGastoLineaViewModel>
        ExpenseLines { get; } = [];
    public ObservableCollection<OperacionSinSustentoInventarioLineaViewModel>
        InventoryLines { get; } = [];
    public ProductFormViewModel ProductForm { get; }
    public ObservableCollection<OperacionSinSustentoLoteReferenciaViewModel>
        ExistingLots { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsExpense))]
    [NotifyPropertyChangedFor(nameof(IsInventory))]
    [NotifyPropertyChangedFor(nameof(Total))]
    private string operationType = "INVENTARIO";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCash))]
    [NotifyPropertyChangedFor(nameof(IsBank))]
    [NotifyPropertyChangedFor(nameof(SourceLabel))]
    [NotifyPropertyChangedFor(nameof(HasAvailableSources))]
    [NotifyPropertyChangedFor(nameof(SourceEmptyMessage))]
    private string sourceType = "CAJA";
    [ObservableProperty] private FondoSalidaDto? selectedCashSession;
    [ObservableProperty] private FondoSalidaDto? selectedBankAccount;
    [ObservableProperty] private FondoSalidaDto? selectedWarehouse;
    [ObservableProperty] private DateTime operationDate = DateTime.Today;
    [ObservableProperty] private string beneficiary = string.Empty;
    [ObservableProperty] private string reason = string.Empty;
    [ObservableProperty] private string? reference;
    [ObservableProperty] private string? evidencePath;
    [ObservableProperty] private bool isBusy;
    [ObservableProperty] private bool isProductFormOpen;
    [ObservableProperty] private bool isControlOpen;
    [ObservableProperty] private OperacionSinSustentoControlEditorViewModel? controlEditor;

    public bool IsExpense => OperationType == "GASTO";
    public bool IsInventory => OperationType == "INVENTARIO";
    public bool IsCash => SourceType == "CAJA";
    public bool IsBank => SourceType == "BANCO";
    public string SourceLabel => IsCash ? "Caja abierta" : "Cuenta bancaria";
    public bool HasAvailableSources => IsCash
        ? CashSessions.Count > 0
        : BankAccounts.Count > 0;
    public string SourceEmptyMessage => IsCash
        ? "No existe una caja abierta en el establecimiento actual."
        : "No existen cuentas bancarias activas para esta empresa.";
    public decimal Total => IsExpense
        ? ExpenseLines.Sum(x => x.Amount)
        : InventoryLines.Sum(x => x.TotalCost);
    public bool IsCorrection => _correctionOfId.HasValue;
    public string FormTitle => IsCorrection
        ? "Corregir operación" : "Nueva operación sin comprobante";

    public OperacionSinSustentoViewModel(
        IOperacionSinSustentoService service,
        ICompraProductoResolverService resolver,
        IInventoryService inventoryService,
        IProductService productService,
        ProductFormViewModel productForm,
        CurrentSession session,
        IMessageDialogService dialogs)
    {
        _service = service;
        _resolver = resolver;
        _inventoryService = inventoryService;
        _productService = productService;
        ProductForm = productForm;
        _session = session;
        _dialogs = dialogs;
        ProductForm.CloseRequested += OnProductFormClosed;
        ProductForm.ProductSaved += OnProductSaved;
        AddExpenseLine();
        AddInventoryLine();
    }

    public async Task InitializeAsync()
    {
        if (!_session.EmpresaId.HasValue || !_session.EstablecimientoId.HasValue)
        {
            await _dialogs.ShowErrorAsync("Sesión incompleta",
                "Selecciona una empresa y un establecimiento antes de registrar el egreso.");
            CloseRequested?.Invoke();
            return;
        }
        IsBusy = true;
        try
        {
            var catalog = await _service.ObtenerCatalogosAsync(
                _session.EmpresaId.Value, _session.EstablecimientoId.Value,
                _session.UsuarioId);
            Replace(CashSessions, catalog.CajasAbiertas);
            Replace(BankAccounts, catalog.CuentasBancarias);
            Replace(Warehouses, catalog.BodegasNoFacturables);
            Replace(ExpenseAccounts, catalog.CuentasGasto);
            SelectedCashSession = CashSessions.FirstOrDefault(x =>
                x.Id == _session.CajaSesionId) ?? CashSessions.FirstOrDefault();
            SelectedBankAccount = BankAccounts.FirstOrDefault();
            SelectedWarehouse = Warehouses.FirstOrDefault();
            foreach (var line in ExpenseLines)
                line.Account ??= ExpenseAccounts.FirstOrDefault();
            OnPropertyChanged(nameof(HasAvailableSources));
            OnPropertyChanged(nameof(SourceEmptyMessage));
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("No se pudo preparar el formulario",
                "No fue posible cargar cajas, cuentas, bodegas o cuentas de gasto.",
                ex.Message);
            CloseRequested?.Invoke();
        }
        finally { IsBusy = false; }
    }

    public async Task InitializeNewAsync()
    {
        ResetForm();
        await InitializeAsync();
    }

    public async Task InitializeCorrectionAsync(OperacionSinSustentoDetalleDto source)
    {
        _correctionOfId = source.Id;
        await InitializeAsync();
        OperationType = source.Tipo; SourceType = source.MedioSalida;
        OperationDate = source.Fecha.ToDateTime(TimeOnly.MinValue);
        Beneficiary = source.Beneficiario; Reason = source.Motivo;
        Reference = source.Referencia;
        SelectedCashSession = CashSessions.FirstOrDefault(x => x.Id == source.CajaSesionId);
        SelectedBankAccount = BankAccounts.FirstOrDefault(x => x.Id == source.CuentaBancariaId);
        SelectedWarehouse = Warehouses.FirstOrDefault(x => x.Id == source.BodegaId);
        ExpenseLines.Clear(); InventoryLines.Clear();
        foreach (var item in source.Lineas)
            if (source.Tipo == "GASTO")
            {
                var line = new OperacionSinSustentoGastoLineaViewModel
                {
                    Account = ExpenseAccounts.FirstOrDefault(x => x.Id == item.CuentaContableId),
                    Description = item.Descripcion, Amount = item.Total
                };
                line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Total));
                ExpenseLines.Add(line);
            }
            else
            {
                var line = new OperacionSinSustentoInventarioLineaViewModel(
                    SearchProductsAsync, LoadProductCostAsync,
                    DiscardContextualDraftAsync)
                { Description = item.Descripcion, Quantity = item.Cantidad, TotalCost = item.Total };
                var matches = await SearchProductsAsync(item.Producto ?? item.Descripcion);
                var match = matches.FirstOrDefault(x => x.ProductoId == item.ProductoId &&
                    x.ProductoPresentacionId == item.ProductoPresentacionId);
                if (match is not null) line.ApplyProduct(match);
                line.ReplaceControl(item.Lotes.Select(x =>
                    new OperacionSinSustentoLoteViewModel
                    {
                        Number = x.NumeroLote,
                        BaseQuantity = x.CantidadBase,
                        ManufacturingDate = x.FechaElaboracion?.ToDateTime(TimeOnly.MinValue),
                        ExpirationDate = x.FechaCaducidad?.ToDateTime(TimeOnly.MinValue)
                    }), item.Series.Select(x =>
                    new OperacionSinSustentoSerieViewModel
                    {
                        Number = x.NumeroSerie,
                        LotNumber = x.NumeroLote
                    }));
                line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Total));
                InventoryLines.Add(line);
            }
        if (ExpenseLines.Count == 0) AddExpenseLine();
        if (InventoryLines.Count == 0) AddInventoryLine();
        OnPropertyChanged(nameof(IsCorrection));
        OnPropertyChanged(nameof(FormTitle));
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void SelectExpense() => OperationType = "GASTO";

    [RelayCommand]
    private void SelectInventory() => OperationType = "INVENTARIO";

    [RelayCommand]
    private void SelectCash() => SourceType = "CAJA";

    [RelayCommand]
    private void SelectBank() => SourceType = "BANCO";

    [RelayCommand]
    private void AddExpenseLine()
    {
        var line = new OperacionSinSustentoGastoLineaViewModel();
        line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Total));
        ExpenseLines.Add(line);
    }

    [RelayCommand]
    private void RemoveExpenseLine(OperacionSinSustentoGastoLineaViewModel? line)
    {
        if (line is not null && ExpenseLines.Count > 1) ExpenseLines.Remove(line);
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private void AddInventoryLine()
    {
        var line = new OperacionSinSustentoInventarioLineaViewModel(
            SearchProductsAsync, LoadProductCostAsync,
            DiscardContextualDraftAsync);
        line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(Total));
        InventoryLines.Add(line);
    }

    [RelayCommand]
    private async Task RemoveInventoryLineAsync(OperacionSinSustentoInventarioLineaViewModel? line)
    {
        if (line is not null && InventoryLines.Count > 1)
        {
            if (line.ContextualDraftProductId.HasValue)
                await DiscardContextualDraftAsync(line.ContextualDraftProductId.Value);
            InventoryLines.Remove(line);
        }
        OnPropertyChanged(nameof(Total));
    }

    [RelayCommand]
    private async Task CreateProductAsync(
        OperacionSinSustentoInventarioLineaViewModel? line)
    {
        if (line is null) return;
        if (string.IsNullOrWhiteSpace(line.SearchText))
        {
            await _dialogs.ShowWarningAsync("Descripción requerida",
                "Escribe la descripción del producto antes de crearlo.");
            return;
        }
        if (line.Quantity <= 0)
        {
            await _dialogs.ShowWarningAsync("Cantidad requerida",
                "Ingresa primero la cantidad adquirida. Se utilizará para calcular el costo de la presentación.");
            line.QuantityFocusRequest++;
            return;
        }
        if (line.TotalCost <= 0)
        {
            await _dialogs.ShowWarningAsync("Costo total requerido",
                "Ingresa primero el costo total de la línea. Se utilizará como referencia para configurar los precios.");
            line.CostFocusRequest++;
            return;
        }

        _creatingProductFor = line;
        var presentationCost = line.TotalCost / line.Quantity;
        await ProductForm.NuevoDesdeOperacionSinComprobanteAsync(
            presentationCost);
        var prefill = DescripcionProductoCompraParser.Analizar(
            line.SearchText,
            ProductForm.Marcas.Select(x =>
                new MarcaProductoCompra(x.Id, x.Nombre)));
        ProductForm.Nombre = prefill.Nombre;
        ProductForm.MarcaId = prefill.MarcaId;
        ProductForm.PresentacionNombre = prefill.PresentacionNombre;
        ProductForm.ConfigurarPresentacionCompraDesdeXml(
            prefill.PresentacionCompraNombre,
            prefill.FactorPresentacionCompra,
            presentationCost,
            line.Quantity);
        IsProductFormOpen = true;
    }

    [RelayCommand]
    private async Task ConfigureControlAsync(
        OperacionSinSustentoInventarioLineaViewModel? line)
    {
        if (line?.Product is null || !line.RequiresControl) return;
        if (line.Quantity <= 0)
        {
            await _dialogs.ShowWarningAsync("Cantidad requerida",
                "Ingresa una cantidad mayor que cero antes de configurar lotes y/o series.");
            return;
        }
        if (!_session.EmpresaId.HasValue) return;

        ExistingLots.Clear();
        var state = await _inventoryService.ObtenerEstadoControlAsync(
            _session.EmpresaId.Value, line.Product.ProductoId);
        if (state is not null)
            foreach (var warehouse in state.Bodegas)
                foreach (var lot in warehouse.Lotes)
                    ExistingLots.Add(new OperacionSinSustentoLoteReferenciaViewModel(
                        lot, warehouse.BodegaDisplay));

        ControlEditor = new OperacionSinSustentoControlEditorViewModel(line);
        IsControlOpen = true;
    }

    [RelayCommand]
    private void CancelControl()
    {
        IsControlOpen = false;
        ControlEditor = null;
        ExistingLots.Clear();
    }

    [RelayCommand]
    private async Task ApplyControlAsync()
    {
        if (ControlEditor is null) return;
        var validation = ControlEditor.Validate();
        if (validation is not null)
        {
            await _dialogs.ShowWarningAsync("Control incompleto", validation);
            return;
        }
        ControlEditor.Apply();
        CancelControl();
    }

    [RelayCommand]
    private void UseExistingLot(OperacionSinSustentoLoteReferenciaViewModel? lot)
    {
        if (lot is null || ControlEditor is null) return;
        ControlEditor.UseExistingLot(lot);
    }

    [RelayCommand]
    private async Task SelectEvidenceAsync()
    {
        if (SelectEvidenceRequested is not null)
            await SelectEvidenceRequested.Invoke();
    }

    [RelayCommand]
    private void ClearEvidence() => EvidencePath = null;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!_session.EmpresaId.HasValue || !_session.EstablecimientoId.HasValue)
            return;
        if (IsInventory)
        {
            var invalid = InventoryLines.FirstOrDefault(x =>
                x.Product is null || x.Quantity <= 0 || x.TotalCost <= 0 ||
                (x.RequiresControl && !x.IsControlComplete));
            if (invalid is not null)
            {
                await _dialogs.ShowWarningAsync("Detalle de inventario incompleto",
                    invalid.Product is null
                        ? "Selecciona un producto existente o créalo antes de confirmar."
                        : invalid.RequiresControl && !invalid.IsControlComplete
                            ? "Configura completamente los lotes y/o series del producto antes de confirmar."
                            : "La cantidad y el costo total deben ser mayores que cero.");
                return;
            }
        }
        byte[]? evidence = null;
        if (!string.IsNullOrWhiteSpace(EvidencePath))
        {
            try
            {
                var file = new FileInfo(EvidencePath);
                if (!file.Exists || file.Length <= 0 || file.Length > 10 * 1024 * 1024)
                    throw new InvalidOperationException(
                        "El archivo está vacío, no existe o supera el límite de 10 MB.");
                evidence = await File.ReadAllBytesAsync(EvidencePath);
            }
            catch (Exception ex)
            {
                await _dialogs.ShowErrorAsync("Soporte no disponible",
                    "No se pudo leer el archivo seleccionado.", ex.Message);
                return;
            }
        }
        var request = new OperacionSinSustentoRequest
        {
            OperacionSustituidaId = _correctionOfId,
            EmpresaId = _session.EmpresaId.Value,
            EstablecimientoId = _session.EstablecimientoId.Value,
            UsuarioId = _session.UsuarioId,
            TipoOperacion = OperationType,
            MedioSalida = SourceType,
            CajaSesionId = IsCash ? SelectedCashSession?.Id : null,
            CuentaBancariaId = IsBank ? SelectedBankAccount?.Id : null,
            BodegaId = IsInventory ? SelectedWarehouse?.Id : null,
            Fecha = DateOnly.FromDateTime(OperationDate),
            Beneficiario = Beneficiary,
            Motivo = Reason,
            Referencia = Reference,
            EvidenciaNombre = EvidencePath is null ? null : Path.GetFileName(EvidencePath),
            EvidenciaContenido = evidence,
            Detalles = IsExpense
                ? ExpenseLines.Select(x => new OperacionSinSustentoDetalleRequest
                {
                    CuentaContableId = x.Account?.Id,
                    Descripcion = x.Description,
                    CostoTotal = x.Amount
                }).ToList()
                : InventoryLines.Select(x => x.ToRequest()).ToList()
        };
        IsBusy = true;
        try
        {
            var result = IsCorrection
                ? await _service.CorregirAsync(request)
                : await _service.RegistrarAsync(request);
            if (!result.Success)
            {
                await _dialogs.ShowWarningAsync("No se registró la operación",
                    result.Message);
                return;
            }
            await _dialogs.ShowSuccessAsync("Operación registrada", result.Message,
                "No genera crédito tributario ni gasto deducible.");
            ResetForm();
            Saved?.Invoke();
            CloseRequested?.Invoke();
        }
        catch (Exception ex)
        {
            await _dialogs.ShowErrorAsync("No se pudo guardar",
                "Ocurrió un error inesperado al confirmar la operación.", ex.Message);
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        foreach (var draftId in InventoryLines
            .Select(x => x.ContextualDraftProductId).OfType<long>().Distinct())
            await DiscardContextualDraftAsync(draftId);
        CloseRequested?.Invoke();
    }

    private async Task DiscardContextualDraftAsync(long productId)
    {
        if (!_session.EmpresaId.HasValue) return;
        await _productService.EliminarBorradorContextualAsync(productId,
            _session.EmpresaId.Value);
    }

    private async Task<IReadOnlyList<CandidatoProductoCompraDto>>
        SearchProductsAsync(string text)
    {
        var tokens = text.Trim().Split(' ',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Where(x => x.Length >= 2)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToArray();
        if (tokens.Length == 0) return [];
        var searches = tokens.Select(token =>
            _resolver.BuscarPresentacionesAsync(token, limite: 30));
        var resultSets = await Task.WhenAll(searches);
        return resultSets.SelectMany(x => x)
                .GroupBy(x => x.ProductoPresentacionId)
                .Select(x => x.First())
                .Where(x => tokens.All(token => CandidateContains(x, token)))
                .OrderByDescending(x => x.ProductoNombre.StartsWith(text.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .ThenBy(x => x.ProductoNombre)
                .ThenByDescending(x => x.EsPresentacionBase)
                .ThenBy(x => x.FactorConversion)
                .ThenBy(x => x.PresentacionNombre)
                .Take(20)
                .ToList();
    }

    private static bool CandidateContains(CandidatoProductoCompraDto candidate,
        string token) =>
        candidate.PresentacionCodigo.Contains(token,
            StringComparison.OrdinalIgnoreCase) ||
        candidate.ProductoNombre.Contains(token,
            StringComparison.OrdinalIgnoreCase) ||
        candidate.PresentacionNombre.Contains(token,
            StringComparison.OrdinalIgnoreCase) ||
        candidate.MarcaNombre?.Contains(token,
            StringComparison.OrdinalIgnoreCase) == true ||
        candidate.CodigoBarras?.Contains(token,
            StringComparison.OrdinalIgnoreCase) == true;

    private async Task<OperacionSinSustentoCostoProductoViewModel?>
        LoadProductCostAsync(long productId)
    {
        if (!_session.EmpresaId.HasValue) return null;
        var product = await _productService.ObtenerProductoAsync(
            productId, _session.EmpresaId.Value);
        return product is null ? null : new OperacionSinSustentoCostoProductoViewModel(
            product.Costo.CostoPromedio,
            product.Existencias.Sum(x => x.StockActual));
    }

    private void OnProductFormClosed()
    {
        IsProductFormOpen = false;
        _creatingProductFor = null;
    }

    private async void OnProductSaved(long productId)
    {
        try
        {
            IsProductFormOpen = false;
            var target = _creatingProductFor;
            _creatingProductFor = null;
            if (target is null || !_session.EmpresaId.HasValue) return;
            var pendingPrices = ProductForm
                .ObtenerPreciosPendientesOperacionSinComprobante();
            var preferredCode = ProductForm.PresentacionCompraXmlCodigo;
            var matches = await _resolver.BuscarPresentacionesAsync(
                productoId: productId, limite: 30);
            var candidate = matches.FirstOrDefault(x => string.Equals(
                    x.PresentacionCodigo, preferredCode,
                    StringComparison.OrdinalIgnoreCase)) ?? matches.FirstOrDefault();
            if (candidate is null)
                throw new InvalidOperationException(
                    "El producto no tiene una presentación activa habilitada para compras.");
            var mappedPrices = pendingPrices.Select(price =>
            {
                var presentation = matches.FirstOrDefault(x => string.Equals(
                    x.PresentacionCodigo, price.PresentacionCodigo,
                    StringComparison.OrdinalIgnoreCase));
                return presentation is null ? null :
                    new OperacionSinSustentoPrecioRequest
                {
                    ProductoPresentacionId = presentation.ProductoPresentacionId,
                    ListaPrecioId = price.ListaPrecioId,
                    MetodoCalculo = price.MetodoCalculo,
                    Porcentaje = price.Porcentaje,
                    Precio = price.Precio,
                    Estado = price.Estado
                };
            }).ToList();
            if (mappedPrices.Any(x => x is null))
                throw new InvalidOperationException(
                    "Una presentación no pudo vincularse con sus precios.");

            var deactivation = await _productService.CambiarEstadoAsync(
                productId, _session.EmpresaId.Value, 0);
            if (!deactivation.Success)
                throw new InvalidOperationException(deactivation.Message);
            target.IsContextualProductDraft = true;
            target.ContextualDraftProductId = productId;
            target.ReplacePendingPrices(mappedPrices.OfType<OperacionSinSustentoPrecioRequest>());
            target.ApplyProduct(candidate, target.TotalCost / target.Quantity);
        }
        catch (Exception ex)
        {
            await _dialogs.ShowWarningAsync("Producto no preparado",
                $"No fue posible preparar el producto contextual. {ex.Message}");
        }
    }

    private static void Replace<T>(ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }

    private void ResetForm()
    {
        _correctionOfId = null;
        OperationType = "INVENTARIO";
        Beneficiary = string.Empty;
        Reason = string.Empty;
        Reference = null;
        EvidencePath = null;
        OperationDate = DateTime.Today;
        ExpenseLines.Clear();
        InventoryLines.Clear();
        AddExpenseLine();
        ExpenseLines[0].Account = ExpenseAccounts.FirstOrDefault();
        AddInventoryLine();
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(IsCorrection));
        OnPropertyChanged(nameof(FormTitle));
    }
}

public partial class OperacionSinSustentoGastoLineaViewModel : ObservableObject
{
    [ObservableProperty] private CuentaGastoDto? account;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private decimal amount;
}

public partial class OperacionSinSustentoInventarioLineaViewModel : ObservableObject
{
    private readonly Func<string, Task<IReadOnlyList<CandidatoProductoCompraDto>>> _search;
    private readonly Func<long, Task<OperacionSinSustentoCostoProductoViewModel?>> _loadCost;
    private readonly Func<long, Task> _discardDraft;
    private int _searchVersion;
    private int _costVersion;
    private decimal? _selectedPresentationReferenceCost;
    public ObservableCollection<CandidatoProductoCompraDto> Suggestions { get; } = [];
    public ObservableCollection<OperacionSinSustentoLoteViewModel> Lots { get; } = [];
    public ObservableCollection<OperacionSinSustentoSerieViewModel> Series { get; } = [];
    public List<OperacionSinSustentoPrecioRequest> PendingPrices { get; } = [];
    public bool IsContextualProductDraft { get; set; }
    public long? ContextualDraftProductId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProductSearchHasNoMatches))]
    [NotifyPropertyChangedFor(nameof(HasSearchText))]
    private string searchText = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProductDisplay))]
    [NotifyPropertyChangedFor(nameof(RequiresControl))]
    [NotifyPropertyChangedFor(nameof(HasProduct))]
    [NotifyPropertyChangedFor(nameof(RequiredBaseQuantity))]
    [NotifyPropertyChangedFor(nameof(IsControlComplete))]
    [NotifyPropertyChangedFor(nameof(ControlSummary))]
    [NotifyPropertyChangedFor(nameof(ProductSearchHasNoMatches))]
    [NotifyPropertyChangedFor(nameof(PresentationCostLabel))]
    [NotifyPropertyChangedFor(nameof(PurchaseUnitCost))]
    [NotifyPropertyChangedFor(nameof(BaseUnitCost))]
    [NotifyPropertyChangedFor(nameof(ProjectedAverage))]
    [NotifyPropertyChangedFor(nameof(ProjectedPresentationCost))]
    [NotifyPropertyChangedFor(nameof(ProjectedPresentationLabel))]
    [NotifyPropertyChangedFor(nameof(HasNonBasePresentation))]
    [NotifyPropertyChangedFor(nameof(IsAverageIncrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageDecrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageUnchanged))]
    [NotifyPropertyChangedFor(nameof(HasCostProjection))]
    [NotifyPropertyChangedFor(nameof(HasSignificantCostVariation))]
    private CandidatoProductoCompraDto? product;
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RequiredBaseQuantity))]
    [NotifyPropertyChangedFor(nameof(IsControlComplete))]
    [NotifyPropertyChangedFor(nameof(ControlSummary))]
    [NotifyPropertyChangedFor(nameof(PurchaseUnitCost))]
    [NotifyPropertyChangedFor(nameof(BaseUnitCost))]
    [NotifyPropertyChangedFor(nameof(ProjectedAverage))]
    [NotifyPropertyChangedFor(nameof(ProjectedPresentationCost))]
    [NotifyPropertyChangedFor(nameof(IsAverageIncrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageDecrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageUnchanged))]
    [NotifyPropertyChangedFor(nameof(HasCostProjection))]
    [NotifyPropertyChangedFor(nameof(HasSignificantCostVariation))]
    private decimal quantity;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PurchaseUnitCost))]
    [NotifyPropertyChangedFor(nameof(BaseUnitCost))]
    [NotifyPropertyChangedFor(nameof(ProjectedAverage))]
    [NotifyPropertyChangedFor(nameof(ProjectedPresentationCost))]
    [NotifyPropertyChangedFor(nameof(IsAverageIncrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageDecrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageUnchanged))]
    [NotifyPropertyChangedFor(nameof(HasCostProjection))]
    [NotifyPropertyChangedFor(nameof(HasSignificantCostVariation))]
    private decimal totalCost;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectedAverage))]
    [NotifyPropertyChangedFor(nameof(ProjectedPresentationCost))]
    [NotifyPropertyChangedFor(nameof(IsAverageIncrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageDecrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageUnchanged))]
    [NotifyPropertyChangedFor(nameof(HasSignificantCostVariation))]
    private decimal averageBefore;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProjectedAverage))]
    [NotifyPropertyChangedFor(nameof(ProjectedPresentationCost))]
    [NotifyPropertyChangedFor(nameof(IsAverageIncrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageDecrease))]
    [NotifyPropertyChangedFor(nameof(IsAverageUnchanged))]
    [NotifyPropertyChangedFor(nameof(HasSignificantCostVariation))]
    [NotifyPropertyChangedFor(nameof(AverageProjectionLabel))]
    private decimal stockBefore;
    [ObservableProperty] private bool showSuggestions;
    [ObservableProperty] private int quantityFocusRequest;
    [ObservableProperty] private int costFocusRequest;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProductSearchHasNoMatches))]
    private bool isSearching;

    public string ProductDescription => Product is null ? string.Empty :
        $"{Product.ProductoConMarca} · {Product.PresentacionNombre}";
    public string ProductDisplay => Product is null ? string.Empty :
        $"{ProductDescription} → $ " +
        $"{(_selectedPresentationReferenceCost ?? Product.CostoPromedioPresentacion):N2}";
    public bool HasProduct => Product is not null;
    public bool HasSearchText => !string.IsNullOrWhiteSpace(SearchText);
    public bool ProductSearchHasNoMatches => !HasProduct && !IsSearching &&
        SearchText.Trim().Length >= 2 && Suggestions.Count == 0;
    public bool RequiresControl => Product?.ManejaLotes == true ||
                                   Product?.ManejaSeries == true;
    public decimal RequiredBaseQuantity => Math.Max(0,
        Quantity * (Product?.FactorConversion ?? 1));
    public string PresentationCostLabel => Product is null
        ? "COSTO POR PRESENTACIÓN"
        : $"COSTO / {Product.PresentacionNombre.ToUpperInvariant()}";
    public decimal PurchaseUnitCost => Quantity > 0
        ? TotalCost / Quantity : 0;
    public decimal BaseUnitCost => RequiredBaseQuantity > 0
        ? TotalCost / RequiredBaseQuantity : 0;
    public decimal ProjectedAverage
    {
        get
        {
            var stockAfter = StockBefore + RequiredBaseQuantity;
            return stockAfter <= 0 ? 0 :
                ((StockBefore * AverageBefore) + TotalCost) / stockAfter;
        }
    }
    public string AverageProjectionLabel =>
        $"PROMEDIO PONDERADO BASE · STOCK ACTUAL: {StockBefore:0.######}";
    public bool HasNonBasePresentation =>
        Product is { EsPresentacionBase: false };
    public decimal ProjectedPresentationCost =>
        ProjectedAverage * (Product?.FactorConversion ?? 1);
    public string ProjectedPresentationLabel => Product is null
        ? "COSTO PROMEDIO PROYECTADO DE LA PRESENTACIÓN"
        : $"PROY. {Product.PresentacionNombre.ToUpperInvariant()} " +
          $"(×{Product.FactorConversion:0.######})";
    public bool HasCostProjection => Product is not null &&
        Quantity > 0 && TotalCost > 0;
    public bool IsAverageIncrease => HasCostProjection && AverageBefore > 0 &&
        ProjectedAverage > AverageBefore + 0.000001m;
    public bool IsAverageDecrease => HasCostProjection && AverageBefore > 0 &&
        ProjectedAverage < AverageBefore - 0.000001m;
    public bool IsAverageUnchanged => HasCostProjection &&
        !IsAverageIncrease && !IsAverageDecrease;
    public bool HasSignificantCostVariation => HasCostProjection &&
        AverageBefore > 0 &&
        Math.Abs((ProjectedAverage - AverageBefore) / AverageBefore) >= 0.10m;
    public bool IsControlComplete
    {
        get
        {
            if (!RequiresControl) return true;
            var lotsOk = Product?.ManejaLotes != true ||
                Lots.Count > 0 && Lots.All(x => !string.IsNullOrWhiteSpace(x.Number) &&
                    x.BaseQuantity > 0) &&
                Math.Abs(Lots.Sum(x => x.BaseQuantity) - RequiredBaseQuantity) < 0.000001m;
            var seriesOk = Product?.ManejaSeries != true ||
                RequiredBaseQuantity == decimal.Truncate(RequiredBaseQuantity) &&
                Series.Count == (int)RequiredBaseQuantity &&
                Series.All(x => !string.IsNullOrWhiteSpace(x.Number) &&
                    (Product?.ManejaLotes != true || !string.IsNullOrWhiteSpace(x.LotNumber))) &&
                Series.Select(x => x.Number.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() == Series.Count;
            return lotsOk && seriesOk;
        }
    }
    public string ControlSummary => !RequiresControl ? "Sin control" :
        IsControlComplete ? "Control configurado" : "Control pendiente";

    public OperacionSinSustentoInventarioLineaViewModel(
        Func<string, Task<IReadOnlyList<CandidatoProductoCompraDto>>> search,
        Func<long, Task<OperacionSinSustentoCostoProductoViewModel?>> loadCost,
        Func<long, Task> discardDraft)
    {
        _search = search;
        _loadCost = loadCost;
        _discardDraft = discardDraft;
    }

    partial void OnSearchTextChanged(string value)
    {
        var uppercase = value.ToUpperInvariant();
        if (!string.Equals(value, uppercase, StringComparison.Ordinal))
        {
            SearchText = uppercase;
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            ClearDependentValues();
            return;
        }

        _ = SearchAsync(value);
    }

    partial void OnProductChanged(CandidatoProductoCompraDto? value)
    {
        if (value is null)
        {
            _costVersion++;
            AverageBefore = 0;
            StockBefore = 0;
            return;
        }
        Description = ProductDescription;
        SearchText = ProductDisplay.ToUpperInvariant();
        ShowSuggestions = false;
        Lots.Clear();
        Series.Clear();
        _ = LoadCostAsync(value.ProductoId);
        QuantityFocusRequest++;
    }

    [RelayCommand]
    private void SelectProduct(CandidatoProductoCompraDto? candidate)
    {
        if (candidate is null) return;
        Product = candidate;
        SearchText = ProductDisplay;
        ShowSuggestions = false;
    }

    public void ApplyProduct(CandidatoProductoCompraDto candidate,
        decimal? presentationReferenceCost = null)
    {
        _selectedPresentationReferenceCost = presentationReferenceCost;
        SelectProduct(candidate);
        OnPropertyChanged(nameof(ProductDisplay));
    }

    [RelayCommand]
    private void ClearProduct()
    {
        SearchText = string.Empty;
        ClearDependentValues();
    }

    private void ClearDependentValues()
    {
        var draftId = ContextualDraftProductId;
        _searchVersion++;
        _costVersion++;
        Product = null;
        _selectedPresentationReferenceCost = null;
        Description = string.Empty;
        Quantity = 0;
        TotalCost = 0;
        Suggestions.Clear();
        ShowSuggestions = false;
        IsSearching = false;
        Lots.Clear();
        Series.Clear();
        PendingPrices.Clear();
        IsContextualProductDraft = false;
        ContextualDraftProductId = null;
        OnPropertyChanged(nameof(ProductDisplay));
        OnPropertyChanged(nameof(HasPendingPrices));
        OnPropertyChanged(nameof(PendingPricesDisplay));
        if (draftId.HasValue) _ = _discardDraft(draftId.Value);
    }

    public void ReplacePendingPrices(
        IEnumerable<OperacionSinSustentoPrecioRequest> prices)
    {
        PendingPrices.Clear();
        PendingPrices.AddRange(prices);
        OnPropertyChanged(nameof(HasPendingPrices));
        OnPropertyChanged(nameof(PendingPricesDisplay));
    }

    public bool HasPendingPrices => PendingPrices.Count > 0;
    public string PendingPricesDisplay =>
        $"PRECIOS LISTOS · {PendingPrices.Count}";

    public void ReplaceControl(IEnumerable<OperacionSinSustentoLoteViewModel> lots,
        IEnumerable<OperacionSinSustentoSerieViewModel> series)
    {
        Lots.Clear();
        foreach (var lot in lots)
        {
            lot.PropertyChanged += (_, _) => NotifyControlChanged();
            Lots.Add(lot);
        }
        Series.Clear();
        foreach (var item in series)
        {
            item.PropertyChanged += (_, _) => NotifyControlChanged();
            Series.Add(item);
        }
        NotifyControlChanged();
    }

    private void NotifyControlChanged()
    {
        OnPropertyChanged(nameof(IsControlComplete));
        OnPropertyChanged(nameof(ControlSummary));
    }

    public OperacionSinSustentoDetalleRequest ToRequest() => new()
    {
        ProductoCreadoContextualmente = IsContextualProductDraft,
        ProductoId = Product?.ProductoId,
        ProductoPresentacionId = Product?.ProductoPresentacionId,
        Descripcion = Description,
        Cantidad = Quantity,
        CostoTotal = TotalCost,
        Precios = PendingPrices.Select(x => new OperacionSinSustentoPrecioRequest
        {
            ProductoPresentacionId = x.ProductoPresentacionId,
            ListaPrecioId = x.ListaPrecioId,
            MetodoCalculo = x.MetodoCalculo,
            Porcentaje = x.Porcentaje,
            Precio = x.Precio,
            Estado = x.Estado
        }).ToList(),
        Lotes = Lots.Select(x => new IngresoInventarioLoteRequest
        {
            NumeroLote = x.Number,
            CantidadBase = x.BaseQuantity,
            PermitirCrearLoteSimilar = x.AllowSimilarNewLot,
            FechaElaboracion = x.ManufacturingDate.HasValue
                ? DateOnly.FromDateTime(x.ManufacturingDate.Value) : null,
            FechaCaducidad = x.ExpirationDate.HasValue
                ? DateOnly.FromDateTime(x.ExpirationDate.Value) : null
        }).ToList(),
        Series = Series.Select(x => new IngresoInventarioSerieRequest
            {
                NumeroSerie = x.Number,
                NumeroLote = string.IsNullOrWhiteSpace(x.LotNumber)
                    ? null : x.LotNumber
            })
            .ToList()
    };

    private async Task SearchAsync(string value)
    {
        var version = ++_searchVersion;
        if (Product is not null && string.Equals(value, ProductDisplay,
                StringComparison.OrdinalIgnoreCase))
        {
            IsSearching = false;
            return;
        }
        Product = null;
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length < 2)
        {
            Suggestions.Clear();
            ShowSuggestions = false;
            IsSearching = false;
            return;
        }
        IsSearching = true;
        try
        {
            await Task.Delay(220);
            if (version != _searchVersion) return;
            var result = await _search(value.Trim());
            if (version != _searchVersion || !string.Equals(value, SearchText,
                    StringComparison.Ordinal)) return;
            Suggestions.Clear();
            foreach (var item in result
                .GroupBy(x => x.ProductoPresentacionId)
                .Select(x => x.First()))
                Suggestions.Add(item);
            ShowSuggestions = Suggestions.Count > 0;
            OnPropertyChanged(nameof(ProductSearchHasNoMatches));
        }
        catch
        {
            if (version == _searchVersion)
            {
                Suggestions.Clear();
                ShowSuggestions = false;
            }
        }
        finally
        {
            if (version == _searchVersion)
            {
                IsSearching = false;
                OnPropertyChanged(nameof(ProductSearchHasNoMatches));
            }
        }
    }

    private async Task LoadCostAsync(long productId)
    {
        var version = ++_costVersion;
        try
        {
            var cost = await _loadCost(productId);
            if (version != _costVersion || Product?.ProductoId != productId)
                return;
            AverageBefore = cost?.AverageCost ?? 0;
            StockBefore = cost?.CurrentStock ?? 0;
        }
        catch
        {
            if (version != _costVersion) return;
            AverageBefore = 0;
            StockBefore = 0;
        }
    }
}

public partial class OperacionSinSustentoLoteViewModel : ObservableObject
{
    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty] private decimal baseQuantity;
    [ObservableProperty] private DateTime? manufacturingDate;
    [ObservableProperty] private DateTime? expirationDate;
    [ObservableProperty] private bool allowSimilarNewLot;
}

public sealed record OperacionSinSustentoCostoProductoViewModel(
    decimal AverageCost, decimal CurrentStock);

public partial class OperacionSinSustentoSerieViewModel : ObservableObject
{
    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty] private string? lotNumber;
}

public sealed class OperacionSinSustentoLoteReferenciaViewModel
{
    public EstadoControlLoteDto Lot { get; }
    public string Warehouse { get; }
    public string Number => Lot.NumeroLote;
    public decimal Stock => Lot.StockActual;
    public string StockDisplay => Stock.ToString("0.######");
    public string DatesDisplay
    {
        get
        {
            var manufacturing = Lot.FechaElaboracion.HasValue
                ? $"Elab. {Lot.FechaElaboracion.Value:dd/MM/yyyy}"
                : "Elaboración no registrada";
            var expiration = Lot.FechaCaducidad.HasValue
                ? $"Cad. {Lot.FechaCaducidad.Value:dd/MM/yyyy}"
                : "Sin caducidad";
            return $"{manufacturing}  ·  {expiration}";
        }
    }

    public OperacionSinSustentoLoteReferenciaViewModel(
        EstadoControlLoteDto lot, string warehouse)
    {
        Lot = lot;
        Warehouse = warehouse;
    }
}

public partial class OperacionSinSustentoControlEditorViewModel : ObservableObject
{
    private readonly OperacionSinSustentoInventarioLineaViewModel _source;
    public ObservableCollection<OperacionSinSustentoLoteViewModel> Lots { get; } = [];
    public ObservableCollection<OperacionSinSustentoSerieViewModel> Series { get; } = [];

    public string ProductDisplay => _source.ProductDisplay;
    public bool ManagesLots => _source.Product?.ManejaLotes == true;
    public bool ManagesSeries => _source.Product?.ManejaSeries == true;
    public bool ManagesExpiration => _source.Product?.ManejaFechaCaducidad == true;
    public decimal Required => _source.RequiredBaseQuantity;
    public decimal Assigned => ManagesLots
        ? Lots.Sum(x => x.BaseQuantity)
        : ManagesSeries ? Series.Count : 0;
    public decimal Pending => Required - Assigned;
    public bool HasExcess => Pending < -0.000001m;
    public string DistributionDisplay =>
        $"Asignado: {Assigned:0.######}  ·  Requerido: {Required:0.######}  ·  Pendiente: {Pending:0.######}";

    public OperacionSinSustentoControlEditorViewModel(
        OperacionSinSustentoInventarioLineaViewModel source)
    {
        _source = source;
        foreach (var sourceLot in source.Lots)
            AddLot(new OperacionSinSustentoLoteViewModel
            {
                Number = sourceLot.Number,
                BaseQuantity = sourceLot.BaseQuantity,
                ManufacturingDate = sourceLot.ManufacturingDate,
                ExpirationDate = sourceLot.ExpirationDate,
                AllowSimilarNewLot = sourceLot.AllowSimilarNewLot
            });
        if (ManagesLots && Lots.Count == 0) AddLot();
        foreach (var sourceSeries in source.Series)
            AddSeries(new OperacionSinSustentoSerieViewModel
            {
                Number = sourceSeries.Number,
                LotNumber = sourceSeries.LotNumber
            });
        if (ManagesSeries && Series.Count == 0) AddSeries();
    }

    [RelayCommand]
    private void AddLot() => AddLot(new OperacionSinSustentoLoteViewModel());

    private void AddLot(OperacionSinSustentoLoteViewModel lot)
    {
        lot.PropertyChanged += (_, _) => NotifyDistribution();
        Lots.Add(lot);
        NotifyDistribution();
    }

    [RelayCommand]
    private void RemoveLot(OperacionSinSustentoLoteViewModel? lot)
    {
        if (lot is not null && Lots.Count > 1)
        {
            Lots.Remove(lot);
            foreach (var series in Series.Where(x => string.Equals(x.LotNumber,
                         lot.Number, StringComparison.OrdinalIgnoreCase)))
                series.LotNumber = null;
        }
        NotifyDistribution();
    }

    [RelayCommand]
    private void AddSeries() => AddSeries(new OperacionSinSustentoSerieViewModel());

    private void AddSeries(OperacionSinSustentoSerieViewModel series)
    {
        series.PropertyChanged += (_, _) => NotifyDistribution();
        Series.Add(series);
        NotifyDistribution();
    }

    [RelayCommand]
    private void RemoveSeries(OperacionSinSustentoSerieViewModel? series)
    {
        if (series is not null && Series.Count > 1) Series.Remove(series);
        NotifyDistribution();
    }

    public void UseExistingLot(OperacionSinSustentoLoteReferenciaViewModel reference)
    {
        var target = Lots.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Number)) ??
            new OperacionSinSustentoLoteViewModel();
        if (!Lots.Contains(target)) AddLot(target);
        target.Number = reference.Lot.NumeroLote;
        target.ManufacturingDate = reference.Lot.FechaElaboracion;
        target.ExpirationDate = reference.Lot.FechaCaducidad;
        target.AllowSimilarNewLot = false;
    }

    public string? Validate()
    {
        if (ManagesLots)
        {
            if (Lots.Any(x => string.IsNullOrWhiteSpace(x.Number) || x.BaseQuantity <= 0))
                return "Completa el número y una cantidad mayor que cero para cada lote.";
            if (Lots.GroupBy(x => x.Number.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(x => x.Count() > 1))
                return "No repitas el mismo lote en la distribución.";
            if (Math.Abs(Pending) >= 0.000001m)
                return "La cantidad distribuida entre lotes debe coincidir con la cantidad base requerida.";
            if (ManagesExpiration && Lots.Any(x => !x.ExpirationDate.HasValue))
                return "Ingresa la fecha de caducidad de todos los lotes.";
            if (Lots.Any(x => x.ManufacturingDate.HasValue && x.ExpirationDate.HasValue &&
                x.ManufacturingDate.Value.Date > x.ExpirationDate.Value.Date))
                return "La fecha de elaboración no puede ser posterior a la caducidad.";
        }
        if (ManagesSeries)
        {
            if (Required != decimal.Truncate(Required))
                return "Un producto serializado requiere una cantidad base entera.";
            if (Series.Count != (int)Required || Series.Any(x =>
                    string.IsNullOrWhiteSpace(x.Number)))
                return $"Ingresa exactamente {Required:0} series.";
            if (Series.Select(x => x.Number.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count() != Series.Count)
                return "No repitas números de serie.";
            if (ManagesLots && Series.Any(x => string.IsNullOrWhiteSpace(x.LotNumber) ||
                Lots.All(l => !string.Equals(l.Number, x.LotNumber,
                    StringComparison.OrdinalIgnoreCase))))
                return "Asocia cada serie con uno de los lotes distribuidos.";
        }
        return null;
    }

    public void Apply() => _source.ReplaceControl(
        Lots.Select(x => new OperacionSinSustentoLoteViewModel
        {
            Number = x.Number.Trim().ToUpperInvariant(),
            BaseQuantity = x.BaseQuantity,
            ManufacturingDate = x.ManufacturingDate,
            ExpirationDate = x.ExpirationDate,
            AllowSimilarNewLot = x.AllowSimilarNewLot
        }), Series.Select(x => new OperacionSinSustentoSerieViewModel
        {
            Number = x.Number.Trim().ToUpperInvariant(),
            LotNumber = string.IsNullOrWhiteSpace(x.LotNumber)
                ? null : x.LotNumber.Trim().ToUpperInvariant()
        }));

    private void NotifyDistribution()
    {
        OnPropertyChanged(nameof(Assigned));
        OnPropertyChanged(nameof(Pending));
        OnPropertyChanged(nameof(HasExcess));
        OnPropertyChanged(nameof(DistributionDisplay));
    }
}
