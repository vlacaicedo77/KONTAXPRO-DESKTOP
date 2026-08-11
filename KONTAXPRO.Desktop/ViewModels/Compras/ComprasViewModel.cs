using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Inventory;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.ViewModels.Proveedores;

namespace KONTAXPRO.Desktop.ViewModels.Compras;

public partial class ComprasViewModel : ObservableObject, IDisposable
{
    private readonly ICompraService _purchaseService;
    private readonly ICompraImportacionService _importService;
    private readonly ICompraProductoResolverService _resolver;
    private readonly ICompraRecepcionService _receiptService;
    private readonly IInventoryService _inventoryService;
    private readonly CurrentSession _session;
    private readonly IMessageDialogService _dialogs;
    private readonly INotificationService _notifications;
    private CancellationTokenSource? _operationCancellation;
    private FacturaCompraXmlDto? _invoice;
    private Guid? _importId;
    private long? _supplierId;
    private bool _registeringManualSupplier;
    private bool _updatingManualSupplierSearch;
    private CompraImportLineaViewModel? _creatingProductFor;
    private CompraManualLineViewModel? _creatingManualProductFor;
    private bool _importReceiptPrepared;
    private readonly List<CompraCatalogoItemBasicoDto> _allWarehouses = [];
    private readonly HashSet<string> _existingReceiptSeries =
        new(StringComparer.OrdinalIgnoreCase);

    public event Func<Task>? SelectXmlRequested;
    public event Action? FocusDueDateRequested;
    public event Action? FocusManualDueDateRequested;
    public event Action? FocusManualSupplierSearchRequested;

    public ObservableCollection<CompraCatalogoItemDto> Compras { get; } = [];
    public ObservableCollection<PaginaCompraItemViewModel> PaginasVisibles
        { get; } = [];
    public IReadOnlyList<int> TamanosPagina { get; } = [25, 50, 100];
    public ObservableCollection<CompraImportLineaViewModel> LineasImportadas
        { get; } = [];
    public ObservableCollection<CompraProveedorItemDto> ManualSuppliers { get; }
        = [];
    public ObservableCollection<CompraProveedorItemDto>
        ManualSupplierSuggestions { get; } = [];
    public ObservableCollection<CompraCatalogoItemBasicoDto> Establishments
        { get; } = [];
    public ObservableCollection<CompraCatalogoItemBasicoDto> Warehouses
        { get; } = [];
    public ObservableCollection<CompraCatalogoItemBasicoDto> DocumentTypes
        { get; } = [];
    public ObservableCollection<CompraPresentacionItemDto> Presentations
        { get; } = [];
    public ObservableCollection<CompraCuentaContableItemDto> AccountingAccounts
        { get; } = [];
    public ObservableCollection<CompraTarifaImpuestoItemDto> TaxRates { get; }
        = [];
    public ObservableCollection<CompraManualLineViewModel> ManualLines { get; }
        = [];
    public ObservableCollection<CompraReceiptLineViewModel> ReceiptLines { get; }
        = [];
    public ObservableCollection<CompraRecepcionResumenDto> ConfirmedReceipts
        { get; } = [];
    public ObservableCollection<CompraReceiptLineViewModel> ImportReceiptLines
        { get; } = [];
    public ObservableCollection<CompraReceiptLotEditorViewModel>
        TraceabilityLots { get; } = [];
    public ObservableCollection<CompraReceiptSeriesEditorViewModel>
        TraceabilitySeries { get; } = [];
    public ObservableCollection<EstadoControlLoteDto> ExistingReceiptLots
        { get; } = [];
    public ProductFormViewModel ProductForm { get; }
    public ProveedorFormViewModel ProveedorForm { get; }

    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isImportWizardOpen;
    [ObservableProperty] private bool isProductFormOpen;
    [ObservableProperty] private bool isSupplierFormOpen;
    [ObservableProperty] private bool isManualFormOpen;
    [ObservableProperty] private bool isManualSupplierFormOpen;
    [ObservableProperty] private bool isReceiptFormOpen;
    [ObservableProperty] private bool isCancelFormOpen;
    [ObservableProperty] private bool isReceiptCancelFormOpen;
    [ObservableProperty] private int importStep = 1;
    [ObservableProperty] private string? searchText;
    [ObservableProperty] private string? selectedStatus;
    [ObservableProperty] private int paginaActual = 1;
    [ObservableProperty] private int tamanoPagina = 25;
    [ObservableProperty] private int totalItems;
    [ObservableProperty] private int totalPaginas;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EmptyStateTitle))]
    private string? statusMessage;
    [ObservableProperty] private string importFileName = string.Empty;
    [ObservableProperty] private string supplierName = string.Empty;
    [ObservableProperty] private string supplierRuc = string.Empty;
    [ObservableProperty] private string supplierAddress = string.Empty;
    [ObservableProperty] private string documentNumber = string.Empty;
    [ObservableProperty] private string environmentText = string.Empty;
    [ObservableProperty] private string? importWarning;
    [ObservableProperty] private bool supplierReady;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupplierBadgeText))]
    private bool isNewSupplier;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SupplierStateTitle))]
    [NotifyPropertyChangedFor(nameof(SupplierStateDescription))]
    [NotifyPropertyChangedFor(nameof(SupplierActionText))]
    private EstadoProveedorImportacion supplierImportState;
    [ObservableProperty] private bool isCredit;
    [ObservableProperty] private DateTime? dueDate;
    [ObservableProperty] private int totalPurchases;
    [ObservableProperty] private int pendingReceipts;
    [ObservableProperty] private int partialReceipts;
    [ObservableProperty] private int receivedPurchases;
    [ObservableProperty] private decimal purchasesAmount;
    [ObservableProperty] private CompraCatalogoItemDto? selectedPurchase;
    [ObservableProperty] private CompraProveedorItemDto? manualSupplier;
    [ObservableProperty] private string manualSupplierSearchText = string.Empty;
    [ObservableProperty] private bool showManualSupplierSuggestions;
    [ObservableProperty] private CompraCatalogoItemBasicoDto? manualEstablishment;
    [ObservableProperty] private CompraCatalogoItemBasicoDto? manualDocumentType;
    [ObservableProperty] private string manualPurchaseType = "FACTURADA";
    [ObservableProperty] private string manualDocumentNumber = string.Empty;
    [ObservableProperty] private string manualDocumentEstablishment = string.Empty;
    [ObservableProperty] private string manualDocumentEmissionPoint = string.Empty;
    [ObservableProperty] private string manualDocumentSequential = string.Empty;
    [ObservableProperty] private DateTime manualIssueDate = DateTime.Today;
    [ObservableProperty] private bool manualIsCredit;
    [ObservableProperty] private DateTime? manualDueDate;
    [ObservableProperty] private string? manualObservation;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ManualFormTitle))]
    [NotifyPropertyChangedFor(nameof(ManualFormDescription))]
    [NotifyPropertyChangedFor(nameof(ManualSaveText))]
    private long? manualEditingPurchaseId;
    [ObservableProperty] private CompraCatalogoItemBasicoDto? receiptWarehouse;
    [ObservableProperty] private DateTime receiptDate = DateTime.Now;
    [ObservableProperty] private string? receiptObservation;
    [ObservableProperty] private string receiptTitle = string.Empty;
    [ObservableProperty] private bool receiveImportedNow = true;
    [ObservableProperty] private CompraCatalogoItemBasicoDto?
        importReceiptWarehouse;
    [ObservableProperty] private DateTime importReceiptDate = DateTime.Now;
    [ObservableProperty] private string? importReceiptObservation;
    [ObservableProperty] private bool isTraceabilityEditorOpen;
    [ObservableProperty] private CompraReceiptLineViewModel? traceabilityLine;
    [ObservableProperty] private string? traceabilityError;
    [ObservableProperty] private string cancelReason = string.Empty;
    [ObservableProperty] private CompraRecepcionResumenDto? selectedReceiptToCancel;
    [ObservableProperty] private string receiptCancelReason = string.Empty;

    public bool TraceabilityHandlesLots => TraceabilityLine?.HandlesLots == true;

    public bool ManualIsInvoiced => string.Equals(
        ManualPurchaseType, "FACTURADA", StringComparison.Ordinal);
    public string ManualFormTitle => ManualEditingPurchaseId.HasValue
        ? "Corregir compra manual" : "Nueva compra manual";
    public string ManualFormDescription => ManualEditingPurchaseId.HasValue
        ? "La versión anterior se anulará y esta corrección quedará enlazada en el historial"
        : "Registra el comprobante, clasifica cada línea y deja la compra lista para su recepción";
    public string ManualSaveText => ManualEditingPurchaseId.HasValue
        ? "Guardar corrección" : "Guardar compra";
    public bool ManualSupplierHasMatches =>
        ManualSupplierSuggestions.Count > 0;
    public bool HasManualSupplierSelected => ManualSupplier is not null;
    public decimal ManualGrossSubtotal => ManualLines.Sum(x =>
        x.Quantity * x.UnitPrice);
    public decimal ManualDiscountTotal => ManualLines.Sum(x => x.Discount);
    public decimal ManualSubtotal =>
        ManualGrossSubtotal - ManualDiscountTotal;
    public decimal ManualTaxTotal => ManualLines.Sum(x => x.Tax);
    public decimal ManualTotal => ManualSubtotal + ManualTaxTotal;
    public bool TraceabilityHandlesSeries => TraceabilityLine?.HandlesSeries == true;
    public bool TraceabilityHandlesExpiration =>
        TraceabilityLine?.HandlesExpiration == true;
    public decimal TraceabilityRequiredBase => TraceabilityLine is null
        ? 0 : TraceabilityLine.ReceiveNow * TraceabilityLine.FactorConversion;
    public decimal TraceabilityAssignedLots =>
        TraceabilityLots.Sum(x => x.QuantityBase);
    public decimal TraceabilityPendingLots =>
        TraceabilityRequiredBase - TraceabilityAssignedLots;
    public int TraceabilityAssignedSeries => TraceabilitySeries.Count;
    public decimal TraceabilityPendingSeries =>
        TraceabilityRequiredBase - TraceabilityAssignedSeries;
    public decimal TraceabilityAssignedControl => TraceabilityHandlesLots
        ? TraceabilityAssignedLots
        : TraceabilityAssignedSeries;
    public decimal TraceabilityPendingControl =>
        TraceabilityRequiredBase - TraceabilityAssignedControl;
    public bool TraceabilityHasExcess => TraceabilityPendingControl < 0;

    public IReadOnlyList<CompraEstadoFiltroItem> StatusOptions { get; } =
    [
        new("TODAS", "Todos los estados"),
        new("PENDIENTE_RECEPCION", "Pendiente de recepción"),
        new("PARCIALMENTE_RECIBIDA", "Recibida parcialmente"),
        new("RECIBIDA", "Recibida"),
        new("ANULADA", "Anulada")
    ];
    public string StepTitle => ImportStep switch
    {
        1 => "1 · Proveedor y comprobante",
        2 => "2 · Relacionar productos",
        3 => "3 · Revisar compra",
        _ => "4 · Recibir mercadería"
    };
    public string StepNumber => ImportStep.ToString();
    public string StepName => ImportStep switch
    {
        1 => "Proveedor y comprobante",
        2 => "Relacionar productos",
        3 => "Revisar compra",
        _ => "Recibir mercadería"
    };
    public string StepInstruction => ImportStep switch
    {
        1 => "Confirma la identidad del emisor y los valores del documento.",
        2 => "Asocia cada línea del XML con su producto y presentación.",
        3 => "Verifica el resumen y las condiciones comerciales.",
        _ => "Confirma el ingreso físico ahora o guarda la recepción para después."
    };
    public decimal ImportedTotal => _invoice?.ImporteTotal ?? 0m;
    public decimal ImportedSubtotal => _invoice?.TotalSinImpuestos ?? 0m;
    public decimal ImportedDiscount => _invoice?.TotalDescuento ?? 0m;
    public decimal ImportedTaxTotal => _invoice?.Impuestos.Sum(x => x.Valor) ?? 0m;
    public string ImportAccessKey => _invoice?.ClaveAcceso ?? string.Empty;
    public bool IsDocumentAuthorizedSri =>
        _invoice?.EstadoValidacion == "AUTORIZADO_SRI";
    public string DocumentValidationBadgeText => IsDocumentAuthorizedSri
        ? "AUTORIZADO SRI"
        : "VALIDACIÓN LOCAL";
    public string DocumentValidationDescription =>
        _invoice?.MensajeValidacion ?? string.Empty;
    public DateOnly? ImportedIssueDate => _invoice?.FechaEmision;
    public int ImportedLineCount => _invoice?.Detalles.Count ?? 0;
    public string SupplierXmlCommercialName =>
        _invoice?.NombreComercialEmisor ?? string.Empty;
    public string SupplierXmlAddress => _invoice?.DireccionMatriz ?? string.Empty;
    public string SupplierStateTitle => SupplierImportState switch
    {
        EstadoProveedorImportacion.ProveedorActivo => "Proveedor reconocido",
        EstadoProveedorImportacion.ProveedorInactivo => "Proveedor inactivo",
        EstadoProveedorImportacion.TerceroSinProveedor => "Contacto existente",
        _ => "Proveedor nuevo"
    };
    public string SupplierStateDescription => SupplierImportState switch
    {
        EstadoProveedorImportacion.ProveedorActivo =>
            "Está activo y listo para asociarse a esta compra.",
        EstadoProveedorImportacion.ProveedorInactivo =>
            "Existe en KONTAXPRO, pero debe reactivarse antes de continuar.",
        EstadoProveedorImportacion.TerceroSinProveedor =>
            "Ya existe como tercero; completa su configuración como proveedor.",
        _ => "No está registrado. Crea su ficha con los datos obtenidos del XML."
    };
    public string SupplierActionText => SupplierImportState switch
    {
        EstadoProveedorImportacion.ProveedorInactivo => "Reactivar proveedor",
        EstadoProveedorImportacion.TerceroSinProveedor =>
            "Completar como proveedor",
        _ => "Registrar proveedor"
    };
    public string SupplierBadgeText => IsNewSupplier
        ? "NUEVO · REGISTRADO"
        : "EN BASE";
    public bool IsKpiAll => SelectedStatus == "TODAS";
    public bool IsKpiPending => SelectedStatus == "PENDIENTE_RECEPCION";
    public bool IsKpiPartial => SelectedStatus == "PARCIALMENTE_RECIBIDA";
    public bool IsKpiReceived => SelectedStatus == "RECIBIDA";
    public bool PuedeIrPaginaAnterior => PaginaActual > 1;
    public bool PuedeIrPaginaSiguiente =>
        TotalPaginas > 0 && PaginaActual < TotalPaginas;
    public string TextoPaginacion
    {
        get
        {
            if (TotalItems == 0)
                return "Mostrando 0 de 0 compras";

            var inicio = (PaginaActual - 1) * TamanoPagina + 1;
            var fin = Math.Min(PaginaActual * TamanoPagina, TotalItems);
            return $"Mostrando {inicio:N0}–{fin:N0} de {TotalItems:N0} compras";
        }
    }
    public string EmptyStateTitle => StatusMessage ==
        "No fue posible cargar las compras."
            ? StatusMessage
            : "No se encontraron compras";
    public bool CanContinue => ImportStep switch
    {
        1 => SupplierReady,
        2 => LineasImportadas.Count > 0 &&
             LineasImportadas.All(x => x.IsClassified),
        3 => !IsCredit ||
             (DueDate.HasValue && _invoice is not null &&
              DateOnly.FromDateTime(DueDate.Value) >= _invoice.FechaEmision),
        _ => !ReceiveImportedNow || !HasImportInventory ||
             (ImportReceiptWarehouse is not null &&
              ImportReceiptLines.Count > 0)
    };
    public bool HasImportInventory => LineasImportadas.Any(x =>
        !x.IsNonInventory && x.PresentationId.HasValue);

    public ComprasViewModel(
        ICompraService purchaseService,
        ICompraImportacionService importService,
        ICompraProductoResolverService resolver,
        ICompraRecepcionService receiptService,
        IInventoryService inventoryService,
        CurrentSession session,
        IMessageDialogService dialogs,
        INotificationService notifications,
        ProductFormViewModel productForm,
        ProveedorFormViewModel proveedorForm)
    {
        _purchaseService = purchaseService;
        _importService = importService;
        _resolver = resolver;
        _receiptService = receiptService;
        _inventoryService = inventoryService;
        _session = session;
        _dialogs = dialogs;
        _notifications = notifications;
        ProductForm = productForm;
        ProveedorForm = proveedorForm;
        SelectedStatus = "TODAS";
        ProductForm.CloseRequested += OnProductFormClosed;
        ProductForm.ProductSaved += OnProductSaved;
        ProveedorForm.CloseRequested += OnSupplierFormClosed;
        ProveedorForm.Saved += OnSupplierSaved;
        _session.EmpresaActivaChanged += OnCompanyChanged;
    }

    public Task InitializeAsync() => LoadAsync();

    public async Task ImportXmlAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;
        CancelOperation();
        var token = _operationCancellation!.Token;
        IsLoading = true;
        try
        {
            await using var stream = new FileStream(filePath, FileMode.Open,
                FileAccess.Read, FileShare.Read);
            var result = await _importService.AnalizarXmlAsync(stream,
                Path.GetFileName(filePath), token);
            if (!result.Exito || result.Factura is null ||
                !result.ImportacionId.HasValue)
            {
                await _dialogs.ShowErrorAsync("XML no aceptado",
                    result.Mensaje);
                return;
            }
            if (result.EsDuplicado)
            {
                await _dialogs.ShowErrorAsync("Comprobante duplicado",
                    result.Mensaje);
                return;
            }
            _invoice = result.Factura;
            _importId = result.ImportacionId;
            _supplierId = result.TerceroId;
            ImportFileName = result.Factura.NombreArchivo;
            SupplierName = result.RazonSocialProveedorLocal ??
                           result.Factura.RazonSocialEmisor;
            SupplierRuc = result.Factura.RucEmisor;
            SupplierAddress = result.DireccionProveedorLocal ??
                              result.Factura.DireccionMatriz ?? string.Empty;
            DocumentNumber = result.Factura.NumeroDocumento;
            EnvironmentText = result.EsAmbientePruebas
                ? "PRUEBAS" : "PRODUCCIÓN";
            ImportWarning = result.AdvertenciaAmbiente;
            SupplierReady = result.EstadoProveedor ==
                            EstadoProveedorImportacion.ProveedorActivo &&
                            result.TerceroId.HasValue;
            SupplierImportState = result.EstadoProveedor;
            IsNewSupplier = result.ProveedorCreadoAutomaticamente;
            // La condición comercial real la confirma el usuario. Los pagos
            // declarados en el XML se conservan como información tributaria,
            // pero no determinan si la compra fue a crédito ni su vencimiento.
            IsCredit = false;
            DueDate = null;
            await LoadFormCatalogsAsync();
            ImportStep = 1;
            IsImportWizardOpen = true;
            await ResolveLinesAsync(token);
            NotifyWizardState();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await _dialogs.ShowErrorAsync("No se pudo importar",
                "No fue posible procesar el archivo seleccionado.");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task SelectXmlAsync()
    {
        if (SelectXmlRequested is not null)
            await SelectXmlRequested.Invoke();
    }

    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();

    [RelayCommand]
    private async Task ApplyFiltersAsync()
    {
        PaginaActual = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task SelectKpiStatusAsync(string? status)
    {
        SelectedStatus = string.IsNullOrWhiteSpace(status) ? "TODAS" : status;
        PaginaActual = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearSearchAsync()
    {
        SearchText = null;
        PaginaActual = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ClearFiltersAsync()
    {
        SearchText = null;
        SelectedStatus = "TODAS";
        PaginaActual = 1;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task IrPaginaAsync(PaginaCompraItemViewModel? pagina)
    {
        if (pagina is null || pagina.EsSeparador ||
            pagina.Numero == PaginaActual) return;
        PaginaActual = pagina.Numero;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PaginaAnteriorAsync()
    {
        if (!PuedeIrPaginaAnterior) return;
        PaginaActual--;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task PaginaSiguienteAsync()
    {
        if (!PuedeIrPaginaSiguiente) return;
        PaginaActual++;
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenManualAsync()
    {
        await LoadFormCatalogsAsync();
        ManualEditingPurchaseId = null;
        ManualSupplier = null;
        ManualSupplierSearchText = string.Empty;
        ManualEstablishment = Establishments.FirstOrDefault(x =>
            x.Id == _session.EstablecimientoId) ?? Establishments.FirstOrDefault();
        ManualPurchaseType = "FACTURADA";
        ManualDocumentType = DocumentTypes.FirstOrDefault(x => x.Codigo == "01");
        ManualDocumentNumber = string.Empty;
        ManualDocumentEstablishment = string.Empty;
        ManualDocumentEmissionPoint = string.Empty;
        ManualDocumentSequential = string.Empty;
        ManualIssueDate = DateTime.Today;
        ManualIsCredit = false;
        ManualDueDate = null;
        ManualObservation = null;
        ClearManualLines();
        AddManualLine();
        IsManualFormOpen = true;
    }

    [RelayCommand]
    private async Task OpenManualEditAsync(CompraCatalogoItemDto? purchase)
    {
        if (purchase is not null)
            SelectedPurchase = purchase;
        if (SelectedPurchase is null)
        {
            await _dialogs.ShowErrorAsync("Selecciona una compra",
                "Selecciona primero la compra manual que deseas corregir.");
            return;
        }
        await LoadFormCatalogsAsync();
        var edit = await _purchaseService.ObtenerManualParaEdicionAsync(
            SelectedPurchase.Id);
        if (edit is null)
        {
            await _dialogs.ShowErrorAsync("Edición no disponible",
                "Solo pueden corregirse compras manuales sin recepciones confirmadas y con autorización de edición.");
            return;
        }

        var supplier = ManualSuppliers.FirstOrDefault(x =>
            x.Id == edit.TerceroProveedorId);
        var establishment = Establishments.FirstOrDefault(x =>
            x.Id == edit.EstablecimientoId);
        var documentType = edit.TipoComprobanteId.HasValue
            ? DocumentTypes.FirstOrDefault(x => x.Id == edit.TipoComprobanteId)
            : null;
        if (supplier is null || establishment is null ||
            (edit.TipoComprobanteId.HasValue && documentType is null))
        {
            await _dialogs.ShowErrorAsync("Datos no disponibles",
                "El proveedor, establecimiento o tipo de comprobante ya no está activo. Reactívalo antes de corregir la compra.");
            return;
        }

        ManualEditingPurchaseId = edit.Id;
        SelectManualSupplier(supplier);
        ManualEstablishment = establishment;
        ManualPurchaseType = edit.TipoCompra;
        ManualDocumentType = documentType;
        ManualDocumentNumber = edit.NumeroDocumento ?? string.Empty;
        var documentParts = ManualDocumentNumber.Split('-');
        ManualDocumentEstablishment = documentParts.Length == 3
            ? documentParts[0] : string.Empty;
        ManualDocumentEmissionPoint = documentParts.Length == 3
            ? documentParts[1] : string.Empty;
        ManualDocumentSequential = documentParts.Length == 3
            ? documentParts[2] : string.Empty;
        ManualIssueDate = edit.FechaEmision.ToDateTime(TimeOnly.MinValue);
        ManualIsCredit = edit.EsCredito;
        ManualDueDate = edit.FechaVencimiento?.ToDateTime(TimeOnly.MinValue);
        ManualObservation = edit.Observacion;
        ClearManualLines();
        foreach (var source in edit.Lineas)
        {
            var line = new CompraManualLineViewModel(Presentations,
                AccountingAccounts, TaxRates)
            {
                IsInventory = source.EsInventariable,
                Description = source.Descripcion,
                Quantity = source.CantidadPresentacion,
                UnitPrice = source.PrecioUnitario,
                Discount = source.DescuentoValor,
                AccountingClassification = source.ClasificacionContable,
                IsBonus = source.EsBonificacion
            };
            if (source.EsInventariable)
                line.SelectedPresentation = Presentations.FirstOrDefault(x =>
                    x.Id == source.ProductoPresentacionId);
            else
            {
                line.SelectedAccountingAccount = AccountingAccounts
                    .FirstOrDefault(x => x.Id == source.CuentaContableId);
                line.SelectedTaxRate = TaxRates.FirstOrDefault(x =>
                    x.Id == source.TarifaImpuestoId);
            }
            if ((source.EsInventariable && line.SelectedPresentation is null) ||
                (!source.EsInventariable &&
                 (line.SelectedAccountingAccount is null ||
                  line.SelectedTaxRate is null)))
            {
                ClearManualLines();
                ManualEditingPurchaseId = null;
                await _dialogs.ShowErrorAsync("Línea no disponible",
                    "Un producto, cuenta o tarifa usada por la compra ya no está activa. Reactívala antes de corregirla.");
                return;
            }
            line.PropertyChanged += OnManualLinePropertyChanged;
            ManualLines.Add(line);
        }
        NotifyManualTotals();
        IsManualFormOpen = true;
    }

    [RelayCommand]
    private void AddManualLine()
    {
        var line = new CompraManualLineViewModel(
            Presentations, AccountingAccounts, TaxRates);
        line.PropertyChanged += OnManualLinePropertyChanged;
        ManualLines.Add(line);
        NotifyManualTotals();
    }

    [RelayCommand]
    private void RemoveManualLine(CompraManualLineViewModel? line)
    {
        if (line is not null && ManualLines.Count > 1)
        {
            line.PropertyChanged -= OnManualLinePropertyChanged;
            ManualLines.Remove(line);
            NotifyManualTotals();
        }
    }

    [RelayCommand]
    private void CloseManual() => IsManualFormOpen = false;

    [RelayCommand]
    private void SelectManualSupplier(CompraProveedorItemDto? supplier)
    {
        if (supplier is null) return;
        _updatingManualSupplierSearch = true;
        try
        {
            ManualSupplier = supplier;
            ManualSupplierSearchText = supplier.Display;
            ShowManualSupplierSuggestions = false;
            OnPropertyChanged(nameof(HasManualSupplierSelected));
        }
        finally { _updatingManualSupplierSearch = false; }
    }

    [RelayCommand]
    private void ClearManualSupplierSelection()
    {
        _updatingManualSupplierSearch = true;
        try
        {
            ManualSupplier = null;
            ManualSupplierSearchText = string.Empty;
            ManualSupplierSuggestions.Clear();
            ShowManualSupplierSuggestions = false;
            OnPropertyChanged(nameof(ManualSupplierHasMatches));
            OnPropertyChanged(nameof(HasManualSupplierSelected));
        }
        finally { _updatingManualSupplierSearch = false; }
        FocusManualSupplierSearchRequested?.Invoke();
    }

    [RelayCommand]
    private async Task RegisterManualSupplierAsync()
    {
        _registeringManualSupplier = true;
        ShowManualSupplierSuggestions = false;
        IsManualSupplierFormOpen = true;
        await ProveedorForm.NuevoAsync();
        var digits = new string(ManualSupplierSearchText
            .Where(char.IsDigit).ToArray());
        if (digits.Length == 13)
            ProveedorForm.NumeroRuc = digits;
    }

    [RelayCommand]
    private async Task SaveManualAsync()
    {
        NormalizeManualInvoiceNumber();
        if (ManualSupplier is null || ManualEstablishment is null)
        {
            await _dialogs.ShowErrorAsync("Compra incompleta",
                "Selecciona proveedor y establecimiento.");
            return;
        }
        if (ManualIsInvoiced && (ManualDocumentType is null ||
            ManualDocumentType.Codigo != "01" ||
            !IsValidManualInvoiceNumber(ManualDocumentNumber)))
        {
            await _dialogs.ShowErrorAsync("Comprobante incompleto",
                "Ingresa el número completo de la factura con formato 001-001-000000001.");
            return;
        }
        if (ManualIsCredit && !ManualDueDate.HasValue)
        {
            await _dialogs.ShowErrorAsync("Fecha de vencimiento requerida",
                "Indica la fecha de vencimiento de la compra a crédito.");
            return;
        }
        if (ManualIsCredit && ManualDueDate!.Value.Date < ManualIssueDate.Date)
        {
            await _dialogs.ShowErrorAsync("Fecha de vencimiento inválida",
                "La fecha de vencimiento no puede ser anterior a la fecha de emisión.");
            return;
        }
        if (ManualLines.Any(x => !x.TaxRateId.HasValue))
        {
            await _dialogs.ShowErrorAsync("Tarifa de IVA requerida",
                "Relaciona el producto o selecciona la tarifa de IVA en cada línea no inventariable.");
            return;
        }
        IsLoading = true;
        try
        {
            var request = new GuardarCompraManualRequest
                {
                    TerceroProveedorId = ManualSupplier.Id,
                    EstablecimientoId = ManualEstablishment.Id,
                    TipoComprobanteId = ManualPurchaseType == "FACTURADA"
                        ? ManualDocumentType?.Id : null,
                    TipoCompra = ManualPurchaseType,
                    NumeroDocumento = ManualPurchaseType == "FACTURADA"
                        ? ManualDocumentNumber : null,
                    FechaEmision = DateOnly.FromDateTime(ManualIssueDate),
                    EsCredito = ManualIsCredit,
                    FechaVencimiento = ManualIsCredit && ManualDueDate.HasValue
                        ? DateOnly.FromDateTime(ManualDueDate.Value) : null,
                    Observacion = ManualObservation,
                    Lineas = ManualLines.Select(x =>
                        new GuardarCompraManualLineaRequest
                        {
                            Descripcion = x.Description,
                            EsInventariable = x.IsInventory,
                            ProductoPresentacionId = x.IsInventory
                                ? x.SelectedPresentation?.Id : null,
                            ClasificacionContable = x.IsInventory
                                ? "INVENTARIO" : x.AccountingClassification,
                            CuentaContableId = x.IsInventory
                                ? null : x.SelectedAccountingAccount?.Id,
                            CantidadPresentacion = x.Quantity,
                            PrecioUnitario = x.UnitPrice,
                            DescuentoValor = x.Discount,
                            TarifaImpuestoId = x.TaxRateId,
                            EsBonificacion = x.IsBonus
                        }).ToList()
                };
            var result = ManualEditingPurchaseId.HasValue
                ? await _purchaseService.SustituirManualAsync(
                    ManualEditingPurchaseId.Value, request)
                : await _purchaseService.GuardarManualAsync(request);
            if (!result.Success)
            {
                await _dialogs.ShowErrorAsync("Compra no guardada",
                    result.Message);
                return;
            }
            IsManualFormOpen = false;
            await LoadAsync();
            await _notifications.ShowSuccessAsync(result.Message);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task OpenReceiptAsync(CompraCatalogoItemDto? purchase)
    {
        if (purchase is not null)
            SelectedPurchase = purchase;
        if (SelectedPurchase is null)
        {
            await _dialogs.ShowErrorAsync("Selecciona una compra",
                "Selecciona primero la compra que deseas recibir.");
            return;
        }
        if (SelectedPurchase.Estado is "RECIBIDA" or "ANULADA")
        {
            await _dialogs.ShowErrorAsync("Recepción no disponible",
                "La compra seleccionada no tiene mercadería pendiente.");
            return;
        }
        await LoadFormCatalogsAsync();
        var detail = await _purchaseService.ObtenerDetalleAsync(
            SelectedPurchase.Id);
        if (detail is null) return;
        ReceiptLines.Clear();
        foreach (var line in detail.Lineas.Where(x =>
                     x.EsInventariable && x.CantidadPendiente > 0))
            ReceiptLines.Add(new CompraReceiptLineViewModel(line));
        ShowCompatibleWarehouses(detail.TipoCompra);
        ReceiptWarehouse = Warehouses.FirstOrDefault(x =>
            x.Id == _session.BodegaId) ?? Warehouses.FirstOrDefault();
        ReceiptDate = DateTime.Now;
        ReceiptObservation = null;
        ReceiptTitle = $"{detail.NumeroDocumento} · {detail.Proveedor}";
        IsReceiptFormOpen = true;
    }

    [RelayCommand]
    private async Task OpenCancelAsync(CompraCatalogoItemDto? purchase)
    {
        if (purchase is not null)
            SelectedPurchase = purchase;
        if (SelectedPurchase is null)
        {
            await _dialogs.ShowErrorAsync("Selecciona una compra",
                "Selecciona primero la compra que deseas anular.");
            return;
        }
        CancelReason = string.Empty;
        IsCancelFormOpen = true;
    }

    [RelayCommand]
    private void CloseCancel() => IsCancelFormOpen = false;

    [RelayCommand]
    private async Task ConfirmCancelAsync()
    {
        if (SelectedPurchase is null) return;
        var result = await _purchaseService.AnularAsync(
            SelectedPurchase.Id, CancelReason);
        if (!result.Success)
        {
            await _dialogs.ShowErrorAsync("Compra no anulada", result.Message);
            return;
        }
        IsCancelFormOpen = false;
        await LoadAsync();
        await _notifications.ShowSuccessAsync(result.Message);
    }

    [RelayCommand]
    private void CloseReceipt() => IsReceiptFormOpen = false;

    [RelayCommand]
    private async Task OpenReceiptCancelAsync(CompraCatalogoItemDto? purchase)
    {
        if (purchase is not null)
            SelectedPurchase = purchase;
        if (SelectedPurchase is null)
        {
            await _dialogs.ShowErrorAsync("Selecciona una compra",
                "Selecciona primero la compra cuya recepción deseas revertir.");
            return;
        }
        var receipts = await _receiptService.ObtenerConfirmadasAsync(
            SelectedPurchase.Id);
        ConfirmedReceipts.Clear();
        foreach (var receipt in receipts)
            ConfirmedReceipts.Add(receipt);
        if (ConfirmedReceipts.Count == 0)
        {
            await _dialogs.ShowErrorAsync("Sin recepciones confirmadas",
                "La compra seleccionada no tiene recepciones que puedan revertirse.");
            return;
        }
        SelectedReceiptToCancel = ConfirmedReceipts[0];
        ReceiptCancelReason = string.Empty;
        IsReceiptCancelFormOpen = true;
    }

    [RelayCommand]
    private void CloseReceiptCancel() => IsReceiptCancelFormOpen = false;

    [RelayCommand]
    private async Task ConfirmReceiptCancelAsync()
    {
        if (SelectedReceiptToCancel is null) return;
        IsLoading = true;
        try
        {
            var result = await _receiptService.AnularAsync(
                new AnularCompraRecepcionRequest
                {
                    RecepcionId = SelectedReceiptToCancel.Id,
                    Motivo = ReceiptCancelReason
                });
            if (!result.Success)
            {
                await _dialogs.ShowErrorAsync("Recepción no anulada",
                    result.Message);
                return;
            }
            IsReceiptCancelFormOpen = false;
            await LoadAsync();
            await _notifications.ShowSuccessAsync(result.Message);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ConfirmReceiptAsync()
    {
        if (SelectedPurchase is null || ReceiptWarehouse is null)
        {
            await _dialogs.ShowErrorAsync("Recepción incompleta",
                "Selecciona la compra y la bodega.");
            return;
        }
        var activeLines = ReceiptLines.Where(x => x.ReceiveNow > 0).ToList();
        if (activeLines.Count == 0)
        {
            await _dialogs.ShowErrorAsync("Recepción vacía",
                "Indica al menos una cantidad a recibir.");
            return;
        }
        var requests = new List<ConfirmarCompraRecepcionLineaRequest>();
        foreach (var line in activeLines)
        {
            var parsed = line.BuildRequest();
            if (parsed.Error is not null)
            {
                await _dialogs.ShowErrorAsync("Revisa lotes y series",
                    $"{line.Description}: {parsed.Error}");
                return;
            }
            requests.Add(parsed.Request!);
        }
        IsLoading = true;
        try
        {
            var result = await _receiptService.ConfirmarAsync(
                new ConfirmarCompraRecepcionRequest
                {
                    OperacionUuid = Guid.NewGuid(),
                    CompraId = SelectedPurchase.Id,
                    BodegaId = ReceiptWarehouse.Id,
                    FechaRecepcion = ReceiptDate,
                    Observacion = ReceiptObservation,
                    Lineas = requests
                });
            if (!result.Success)
            {
                await _dialogs.ShowErrorAsync("Recepción no confirmada",
                    result.Message);
                return;
            }
            IsReceiptFormOpen = false;
            await LoadAsync();
            await _notifications.ShowSuccessAsync(result.Message);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void CloseImport()
    {
        CancelOperation();
        IsImportWizardOpen = false;
        ResetImport();
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (ImportStep > 1) ImportStep--;
        NotifyWizardState();
    }

    [RelayCommand]
    private async Task NextStepAsync()
    {
        if (!CanContinue)
        {
            var message = ImportStep switch
            {
                1 => "Registra o activa el proveedor antes de continuar.",
                2 => "Relaciona cada línea con un producto o márcala como no inventariable.",
                3 when !DueDate.HasValue =>
                    "Indica la fecha de vencimiento de la compra a crédito.",
                3 => "La fecha de vencimiento no puede ser anterior a la fecha de emisión.",
                _ => "Completa la información pendiente antes de continuar."
            };
            await _dialogs.ShowErrorAsync("Información pendiente", message);
            return;
        }
        if (ImportStep == 3) PrepareImportReceipt();
        if (ImportStep < 4) ImportStep++;
        NotifyWizardState();
    }

    [RelayCommand]
    private async Task SaveImportAsync()
    {
        if (!_importId.HasValue || !_supplierId.HasValue ||
            _invoice is null || !CanContinue ||
            !_session.EstablecimientoId.HasValue)
        {
            await _dialogs.ShowErrorAsync("No se puede guardar",
                "La importación aún tiene información pendiente.");
            return;
        }
        IsLoading = true;
        try
        {
            GuardarCompraRecepcionInmediataRequest? immediateReceipt = null;
            if (ReceiveImportedNow && HasImportInventory)
            {
                if (ImportReceiptWarehouse is null)
                {
                    await _dialogs.ShowErrorAsync("Recepción incompleta",
                        "Selecciona la bodega donde ingresará la mercadería.");
                    return;
                }
                var receiptLines = new List<GuardarCompraRecepcionInmediataLineaRequest>();
                foreach (var line in ImportReceiptLines)
                {
                    var parsed = line.BuildRequest();
                    if (parsed.Error is not null)
                    {
                        await _dialogs.ShowErrorAsync("Revisa lotes y series",
                            $"{line.Description}: {parsed.Error}");
                        return;
                    }
                    receiptLines.Add(new GuardarCompraRecepcionInmediataLineaRequest
                    {
                        Orden = line.Order,
                        CantidadPresentacion = parsed.Request!.CantidadPresentacion,
                        Lotes = parsed.Request.Lotes,
                        Series = parsed.Request.Series
                    });
                }
                immediateReceipt = new GuardarCompraRecepcionInmediataRequest
                {
                    OperacionUuid = Guid.NewGuid(),
                    BodegaId = ImportReceiptWarehouse.Id,
                    FechaRecepcion = ImportReceiptDate,
                    Observacion = ImportReceiptObservation,
                    Lineas = receiptLines
                };
            }
            foreach (var line in LineasImportadas.Where(x =>
                         x.RememberEquivalence && !x.IsNonInventory &&
                         x.PresentationId.HasValue))
            {
                var code = !string.IsNullOrWhiteSpace(line.MainCode)
                    ? line.MainCode : line.AuxiliaryCode;
                var type = !string.IsNullOrWhiteSpace(line.MainCode)
                    ? "PRINCIPAL" : "AUXILIAR";
                if (string.IsNullOrWhiteSpace(code)) continue;
                var equivalence = await _resolver.GuardarEquivalenciaAsync(
                    new GuardarEquivalenciaProveedorProductoRequest
                    {
                        TerceroProveedorId = _supplierId.Value,
                        ProductoPresentacionId = line.PresentationId.GetValueOrDefault(),
                        CodigoProveedor = code,
                        TipoCodigo = type,
                        DescripcionOriginal = line.Description
                    });
                if (!equivalence.Success)
                {
                    await _dialogs.ShowErrorAsync(
                        "Equivalencia no guardada", equivalence.Message);
                    return;
                }
            }
            var result = await _purchaseService.GuardarImportadaAsync(
                new GuardarCompraImportadaRequest
                {
                    ImportacionId = _importId.Value,
                    TerceroProveedorId = _supplierId.Value,
                    EstablecimientoId = _session.EstablecimientoId.Value,
                    EsCredito = IsCredit,
                    FechaVencimiento = IsCredit && DueDate.HasValue
                        ? DateOnly.FromDateTime(DueDate.Value) : null,
                    RecepcionInmediata = immediateReceipt,
                    Lineas = LineasImportadas.Select(x =>
                        new GuardarLineaCompraImportadaRequest
                        {
                            Orden = x.Order,
                            EsInventariable = !x.IsNonInventory,
                            ProductoPresentacionId = x.IsNonInventory
                                ? null : x.PresentationId,
                            ClasificacionContable = x.IsNonInventory
                                ? x.AccountingClassification : "INVENTARIO",
                            CuentaContableId = x.IsNonInventory
                                ? x.SelectedAccountingAccount?.Id : null,
                            EsBonificacion = x.IsBonus
                        }).ToList()
                });
            if (!result.Success)
            {
                await _dialogs.ShowErrorAsync("Compra no guardada",
                    result.Message);
                return;
            }
            IsImportWizardOpen = false;
            ResetImport();
            await LoadAsync();
            await _notifications.ShowSuccessAsync(result.Message);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task RegisterSupplierAsync()
    {
        _registeringManualSupplier = false;
        await ProveedorForm.NuevoAsync();
        ProveedorForm.NumeroRuc = SupplierRuc;
        IsSupplierFormOpen = true;
    }

    [RelayCommand]
    private async Task CreateProductAsync(CompraImportLineaViewModel? line)
    {
        if (line is null) return;
        _creatingProductFor = line;
        var barcodeCandidates = CodigoBarrasCompraRules.ObtenerCandidatos(
            line.MainCode, line.AuxiliaryCode);
        string? barcode = barcodeCandidates.Count == 1
            ? barcodeCandidates[0]
            : null;
        if (barcodeCandidates.Count > 1)
        {
            var usePrincipal = await _dialogs.ConfirmAsync(
                "Dos códigos GS1 detectados",
                $"Código principal: {barcodeCandidates[0]}\n" +
                $"Código auxiliar: {barcodeCandidates[1]}\n\n" +
                "Selecciona cuál corresponde al código de barras del fabricante. Podrás modificarlo antes de guardar.",
                "Usar principal",
                "Usar auxiliar");
            barcode = usePrincipal
                ? barcodeCandidates[0]
                : barcodeCandidates[1];
        }
        var referenceCost = line.Quantity <= 0
            ? 0
            : line.LineTotal / line.Quantity;
        await ProductForm.NuevoDesdeCompraAsync(
            barcode, barcode is null, referenceCost);
        var prefill = DescripcionProductoCompraParser.Analizar(
            line.Description,
            ProductForm.Marcas.Select(x =>
                new MarcaProductoCompra(x.Id, x.Nombre)));
        ProductForm.Nombre = prefill.Nombre;
        ProductForm.MarcaId = prefill.MarcaId;
        ProductForm.PresentacionNombre = prefill.PresentacionNombre;
        ProductForm.ConfigurarPresentacionCompraDesdeXml(
            prefill.PresentacionCompraNombre,
            prefill.FactorPresentacionCompra,
            referenceCost,
            line.Quantity);
        IsProductFormOpen = true;
    }

    [RelayCommand]
    private async Task CreateManualProductAsync(
        CompraManualLineViewModel? line)
    {
        if (line is null || !line.IsInventory) return;
        if (string.IsNullOrWhiteSpace(line.Description))
        {
            await _dialogs.ShowWarningAsync("Descripción requerida",
                "Escribe la descripción de la línea de la factura antes de crear el producto.");
            return;
        }

        _creatingManualProductFor = line;
        var referenceCost = Math.Max(0, line.UnitPrice);
        await ProductForm.NuevoDesdeCompraAsync(
            codigoBarras: null,
            sinCodigoBarras: true,
            costoReferencial: referenceCost);
        var prefill = DescripcionProductoCompraParser.Analizar(
            line.Description,
            ProductForm.Marcas.Select(x =>
                new MarcaProductoCompra(x.Id, x.Nombre)));
        ProductForm.Nombre = prefill.Nombre;
        ProductForm.MarcaId = prefill.MarcaId;
        ProductForm.PresentacionNombre = prefill.PresentacionNombre;
        ProductForm.ConfigurarPresentacionCompraDesdeXml(
            prefill.PresentacionCompraNombre,
            prefill.FactorPresentacionCompra,
            referenceCost,
            line.Quantity);
        IsProductFormOpen = true;
    }

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var status = SelectedStatus == "TODAS" ? null : SelectedStatus;
            var result = await _purchaseService.ObtenerCatalogoAsync(
                SearchText, status, PaginaActual, TamanoPagina);
            TotalItems = result.TotalFiltrado;
            TotalPaginas = TotalItems == 0 ? 0 :
                (int)Math.Ceiling(TotalItems / (double)TamanoPagina);
            if (TotalPaginas > 0 && PaginaActual > TotalPaginas)
            {
                PaginaActual = TotalPaginas;
                await LoadAsync();
                return;
            }
            Compras.Clear();
            foreach (var item in result.Items) Compras.Add(item);
            TotalPurchases = result.Total;
            PendingReceipts = result.PendientesRecepcion;
            PartialReceipts = result.Parciales;
            ReceivedPurchases = result.Recibidas;
            PurchasesAmount = result.TotalCompras;
            ConstruirPaginasVisibles();
            NotificarPaginacion();
            StatusMessage = Compras.Count == 0
                ? "No existen compras con los filtros seleccionados." : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            StatusMessage = "No fue posible cargar las compras.";
        }
        finally { IsLoading = false; }
    }

    private async Task LoadFormCatalogsAsync()
    {
        var catalogs = await _purchaseService.ObtenerCatalogosFormularioAsync();
        Replace(ManualSuppliers, catalogs.Proveedores);
        Replace(Establishments, catalogs.Establecimientos);
        _allWarehouses.Clear();
        _allWarehouses.AddRange(catalogs.Bodegas);
        Replace(Warehouses, _allWarehouses);
        Replace(DocumentTypes, catalogs.TiposComprobante);
        Replace(Presentations, catalogs.Presentaciones);
        Replace(AccountingAccounts, catalogs.CuentasContables);
        Replace(TaxRates, catalogs.TarifasImpuesto);
    }

    private static void Replace<T>(ObservableCollection<T> target,
        IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source) target.Add(item);
    }

    private async Task ResolveLinesAsync(CancellationToken token)
    {
        if (_invoice is null) return;
        var resolved = await _resolver.ResolverAsync(
            new ResolverProductosCompraRequest
            {
                TerceroProveedorId = _supplierId,
                Lineas = _invoice.Detalles.Select(x =>
                    new LineaCompraAResolverDto
                    {
                        Orden = x.Orden,
                        CodigoPrincipal = x.CodigoPrincipal,
                        CodigoAuxiliar = x.CodigoAuxiliar,
                        Descripcion = x.Descripcion
                    }).ToList()
            }, token);
        ClearImportLines();
        foreach (var xml in _invoice.Detalles)
        {
            var match = resolved.SingleOrDefault(x => x.Orden == xml.Orden);
            var line = new CompraImportLineaViewModel(xml, match,
                (search, productId, cancellationToken) =>
                    _resolver.BuscarPresentacionesAsync(search, productId, 20,
                        cancellationToken), AccountingAccounts);
            line.PropertyChanged += (_, _) => NotifyWizardState();
            LineasImportadas.Add(line);
        }
    }

    private void OnProductFormClosed()
    {
        IsProductFormOpen = false;
        _creatingProductFor = null;
        _creatingManualProductFor = null;
    }

    private async void OnProductSaved(long productId)
    {
        IsProductFormOpen = false;
        if (_creatingManualProductFor is not null)
        {
            var targetManualLine = _creatingManualProductFor;
            var manualPurchasePresentationCode =
                ProductForm.PresentacionCompraXmlCodigo;
            try
            {
                await LoadFormCatalogsAsync();
                var presentation = Presentations.FirstOrDefault(x =>
                        x.ProductoId == productId && string.Equals(
                            x.Codigo, manualPurchasePresentationCode,
                            StringComparison.OrdinalIgnoreCase)) ??
                    Presentations.FirstOrDefault(x =>
                        x.ProductoId == productId);
                if (presentation is null)
                {
                    await _dialogs.ShowErrorAsync("Producto no relacionado",
                        "El producto se creó, pero no tiene una presentación activa habilitada para compras.");
                    return;
                }
                targetManualLine.SelectedPresentation = presentation;
            }
            finally { _creatingManualProductFor = null; }
            return;
        }
        if (_creatingProductFor is null) return;
        var targetLine = _creatingProductFor;
        var purchasePresentationCode =
            ProductForm.PresentacionCompraXmlCodigo;
        try
        {
            var presentations = await _resolver.BuscarPresentacionesAsync(
                productoId: productId, limite: 30);
            var presentation = presentations.FirstOrDefault(x =>
                    string.Equals(
                        x.PresentacionCodigo,
                        purchasePresentationCode,
                        StringComparison.OrdinalIgnoreCase)) ??
                presentations.FirstOrDefault();
            if (presentation is null)
            {
                await _dialogs.ShowErrorAsync("Producto no relacionado",
                    "El producto se creó, pero no tiene una presentación activa habilitada para compras.");
                return;
            }
            targetLine.ApplyManualCandidate(presentation);
        }
        finally { _creatingProductFor = null; }
    }

    private void OnSupplierFormClosed()
    {
        if (_registeringManualSupplier)
        {
            IsManualSupplierFormOpen = false;
            _registeringManualSupplier = false;
            return;
        }
        IsSupplierFormOpen = false;
    }

    private async void OnSupplierSaved(long supplierId)
    {
        if (_registeringManualSupplier)
        {
            IsManualSupplierFormOpen = false;
            _registeringManualSupplier = false;
            await LoadFormCatalogsAsync();
            var supplier = ManualSuppliers.FirstOrDefault(x =>
                x.Id == supplierId);
            SelectManualSupplier(supplier);
            return;
        }
        IsSupplierFormOpen = false;
        _supplierId = supplierId;
        SupplierReady = true;
        SupplierImportState = EstadoProveedorImportacion.ProveedorActivo;
        if (_invoice is not null)
            await ResolveLinesAsync(CancellationToken.None);
        NotifyWizardState();
    }

    private void OnCompanyChanged(object? sender,
        EmpresaActivaChangedEventArgs e)
    {
        CancelOperation();
        IsImportWizardOpen = false;
        ResetImport();
        _ = LoadAsync();
    }

    partial void OnImportStepChanged(int value) => NotifyWizardState();

    partial void OnIsCreditChanged(bool value)
    {
        if (!value)
            DueDate = null;
        else
            FocusDueDateRequested?.Invoke();
        NotifyWizardState();
    }

    partial void OnDueDateChanged(DateTime? value) => NotifyWizardState();

    partial void OnReceiveImportedNowChanged(bool value) => NotifyWizardState();

    partial void OnImportReceiptWarehouseChanged(
        CompraCatalogoItemBasicoDto? value) => NotifyWizardState();

    partial void OnSelectedStatusChanged(string? value)
    {
        OnPropertyChanged(nameof(IsKpiAll));
        OnPropertyChanged(nameof(IsKpiPending));
        OnPropertyChanged(nameof(IsKpiPartial));
        OnPropertyChanged(nameof(IsKpiReceived));
    }

    partial void OnTamanoPaginaChanged(int value)
    {
        PaginaActual = 1;
        _ = LoadAsync();
    }

    private void ConstruirPaginasVisibles()
    {
        PaginasVisibles.Clear();
        if (TotalPaginas <= 0) return;

        var paginas = new SortedSet<int>
        {
            1,
            TotalPaginas,
            Math.Max(1, PaginaActual - 2),
            Math.Max(1, PaginaActual - 1),
            PaginaActual,
            Math.Min(TotalPaginas, PaginaActual + 1),
            Math.Min(TotalPaginas, PaginaActual + 2)
        };
        var anterior = 0;
        foreach (var numero in paginas)
        {
            if (anterior > 0 && numero - anterior > 1)
                PaginasVisibles.Add(PaginaCompraItemViewModel.Separador());
            PaginasVisibles.Add(new PaginaCompraItemViewModel(
                numero, numero == PaginaActual));
            anterior = numero;
        }
    }

    private void NotificarPaginacion()
    {
        OnPropertyChanged(nameof(PuedeIrPaginaAnterior));
        OnPropertyChanged(nameof(PuedeIrPaginaSiguiente));
        OnPropertyChanged(nameof(TextoPaginacion));
    }

    private void NotifyWizardState()
    {
        OnPropertyChanged(nameof(StepTitle));
        OnPropertyChanged(nameof(StepNumber));
        OnPropertyChanged(nameof(StepName));
        OnPropertyChanged(nameof(StepInstruction));
        OnPropertyChanged(nameof(CanContinue));
        OnPropertyChanged(nameof(HasImportInventory));
        OnPropertyChanged(nameof(ImportedTotal));
        OnPropertyChanged(nameof(ImportedSubtotal));
        OnPropertyChanged(nameof(ImportedDiscount));
        OnPropertyChanged(nameof(ImportedTaxTotal));
        OnPropertyChanged(nameof(ImportAccessKey));
        OnPropertyChanged(nameof(IsDocumentAuthorizedSri));
        OnPropertyChanged(nameof(DocumentValidationBadgeText));
        OnPropertyChanged(nameof(DocumentValidationDescription));
        OnPropertyChanged(nameof(ImportedIssueDate));
        OnPropertyChanged(nameof(ImportedLineCount));
        OnPropertyChanged(nameof(SupplierXmlCommercialName));
        OnPropertyChanged(nameof(SupplierXmlAddress));
    }

    private void CancelOperation()
    {
        _operationCancellation?.Cancel();
        _operationCancellation?.Dispose();
        _operationCancellation = new CancellationTokenSource();
    }

    private void ResetImport()
    {
        _invoice = null;
        _importId = null;
        _supplierId = null;
        ClearImportLines();
        SupplierReady = false;
        IsNewSupplier = false;
        SupplierAddress = string.Empty;
        SupplierImportState = EstadoProveedorImportacion.NoExiste;
        ImportWarning = null;
        ImportReceiptLines.Clear();
        ReceiveImportedNow = true;
        ImportReceiptWarehouse = null;
        ImportReceiptDate = DateTime.Now;
        ImportReceiptObservation = null;
        _importReceiptPrepared = false;
        ImportStep = 1;
    }

    private void PrepareImportReceipt()
    {
        ShowCompatibleWarehouses("FACTURADA");
        var previousLines = ImportReceiptLines
            .GroupBy(x => x.Order)
            .ToDictionary(x => x.Key, x => x.First());
        CloseTraceabilityEditor();
        ImportReceiptLines.Clear();
        foreach (var line in LineasImportadas.Where(x =>
                     !x.IsNonInventory && x.PresentationId.HasValue))
        {
            var receiptLine = new CompraReceiptLineViewModel(line);
            if (previousLines.TryGetValue(line.Order, out var previous))
                receiptLine.RestoreDraftFrom(previous);
            ImportReceiptLines.Add(receiptLine);
        }
        if (!_importReceiptPrepared)
        {
            ReceiveImportedNow = ImportReceiptLines.Count > 0;
            ImportReceiptWarehouse = Warehouses.FirstOrDefault(x =>
                x.Id == _session.BodegaId) ?? Warehouses.FirstOrDefault();
            ImportReceiptDate = DateTime.Now;
            ImportReceiptObservation = null;
            _importReceiptPrepared = true;
        }
        NotifyWizardState();
    }

    private void ShowCompatibleWarehouses(string purchaseType) =>
        Replace(Warehouses, _allWarehouses.Where(x =>
            CompraBodegaRules.EsCompatible(
                purchaseType, x.PermiteVentaFacturada)));

    [RelayCommand]
    private async Task OpenTraceabilityEditorAsync(
        CompraReceiptLineViewModel? line)
    {
        if (line is null || !line.NeedsTraceability ||
            line.ProductId <= 0 || !_session.EmpresaId.HasValue)
            return;

        TraceabilityLine = line;
        TraceabilityError = null;
        TraceabilityLots.Clear();
        TraceabilitySeries.Clear();
        ExistingReceiptLots.Clear();
        _existingReceiptSeries.Clear();

        try
        {
            var state = await _inventoryService.ObtenerEstadoControlAsync(
                _session.EmpresaId.Value, line.ProductId);
            if (state is not null)
            {
                foreach (var group in state.Bodegas
                             .SelectMany(warehouse => warehouse.Lotes.Select(lot =>
                                 new { warehouse.BodegaId, Lot = lot }))
                             .GroupBy(x => x.Lot.LoteId)
                             .OrderBy(x => x.First().Lot.NumeroLote))
                {
                    var reference = group.First().Lot;
                    var destinationWarehouseId = line.DetailId > 0
                        ? ReceiptWarehouse?.Id
                        : ImportReceiptWarehouse?.Id;
                    var local = group.FirstOrDefault(x => x.BodegaId ==
                        destinationWarehouseId)?.Lot;
                    ExistingReceiptLots.Add(new EstadoControlLoteDto
                    {
                        LoteId = reference.LoteId,
                        NumeroLote = reference.NumeroLote,
                        StockActual = local?.StockActual ?? 0,
                        StockReservado = local?.StockReservado ?? 0,
                        FechaElaboracion = reference.FechaElaboracion,
                        FechaCaducidad = reference.FechaCaducidad,
                        UltimoCostoUnitarioBase =
                            reference.UltimoCostoUnitarioBase
                    });
                }
                foreach (var number in state.SeriesProducto)
                    _existingReceiptSeries.Add(number.Trim());
            }

            foreach (var value in line.TraceabilityLots)
            {
                var editor = CreateLotEditor();
                editor.Number = value.Number;
                editor.QuantityBase = value.QuantityBase;
                editor.ManufactureDate = value.ManufactureDate;
                editor.ExpirationDate = value.ExpirationDate;
                editor.AllowSimilarCreation = value.AllowSimilarCreation;
                editor.SelectedExistingLot = ExistingReceiptLots.FirstOrDefault(x =>
                    string.Equals(x.NumeroLote, value.Number,
                        StringComparison.OrdinalIgnoreCase));
                TraceabilityLots.Add(editor);
            }
            foreach (var value in line.TraceabilitySeries)
                TraceabilitySeries.Add(new CompraReceiptSeriesEditorViewModel
                {
                    Number = value.Number,
                    LotRowId = TraceabilityLots.FirstOrDefault(x =>
                        string.Equals(x.Number, value.LotNumber,
                            StringComparison.OrdinalIgnoreCase))?.RowId
                });

            if (line.HandlesLots && TraceabilityLots.Count == 0)
            {
                var first = CreateLotEditor();
                first.QuantityBase = 0;
                TraceabilityLots.Add(first);
            }
            if (line.HandlesSeries && TraceabilitySeries.Count == 0)
                TraceabilitySeries.Add(
                    new CompraReceiptSeriesEditorViewModel());

            IsTraceabilityEditorOpen = true;
            NotifyTraceabilitySummary();
        }
        catch (Exception ex)
        {
            TraceabilityError =
                $"No fue posible consultar los lotes existentes: {ex.Message}";
            IsTraceabilityEditorOpen = true;
        }
    }

    [RelayCommand]
    private void CloseTraceabilityEditor()
    {
        IsTraceabilityEditorOpen = false;
        TraceabilityLine = null;
        TraceabilityError = null;
        TraceabilityLots.Clear();
        TraceabilitySeries.Clear();
        ExistingReceiptLots.Clear();
        _existingReceiptSeries.Clear();
        NotifyTraceabilitySummary();
    }

    [RelayCommand]
    private void AddTraceabilityLot()
    {
        var editor = CreateLotEditor();
        editor.QuantityBase = 0;
        TraceabilityLots.Add(editor);
        TraceabilityError = null;
        NotifyTraceabilitySummary();
    }

    [RelayCommand]
    private void RemoveTraceabilityLot(CompraReceiptLotEditorViewModel? lot)
    {
        if (lot is null) return;
        foreach (var series in TraceabilitySeries.Where(x =>
                     x.LotRowId == lot.RowId))
            series.LotRowId = null;
        TraceabilityLots.Remove(lot);
        TraceabilityError = null;
        NotifyTraceabilitySummary();
    }

    [RelayCommand]
    private void AddTraceabilitySeries()
    {
        TraceabilitySeries.Add(new CompraReceiptSeriesEditorViewModel());
        TraceabilityError = null;
        NotifyTraceabilitySummary();
    }

    [RelayCommand]
    private void RemoveTraceabilitySeries(
        CompraReceiptSeriesEditorViewModel? series)
    {
        if (series is null) return;
        TraceabilitySeries.Remove(series);
        TraceabilityError = null;
        NotifyTraceabilitySummary();
    }

    [RelayCommand]
    private void UseExistingReceiptLot(CompraReceiptLotEditorViewModel? lot)
    {
        if (lot?.SimilarLot is null) return;
        ApplyExistingReceiptLot(lot, lot.SimilarLot);
        TraceabilityError = null;
    }

    [RelayCommand]
    private async Task CreateSimilarReceiptLotAsync(
        CompraReceiptLotEditorViewModel? lot)
    {
        if (lot?.SimilarLot is null) return;
        var create = await _dialogs.ConfirmWarningAsync(
            "Confirmar lote diferente",
            $"Existe el lote '{lot.SimilarLot.NumeroLote}', posiblemente equivalente a '{lot.Number}'. ¿Confirma que se trata de un lote distinto?",
            "Crear lote diferente", "Volver");
        if (!create) return;
        lot.AllowSimilarCreation = true;
        lot.SimilarLot = null;
        lot.ShowSuggestions = false;
        TraceabilityError = null;
    }

    [RelayCommand]
    private void SaveTraceability()
    {
        if (TraceabilityLine is null) return;
        TraceabilityError = ValidateTraceability();
        if (TraceabilityError is not null) return;

        var lotValues = new List<CompraReceiptLotValue>();
        foreach (var lot in TraceabilityLots)
        {
            var number = lot.Number.Trim().ToUpperInvariant();
            var exact = ExistingReceiptLots.FirstOrDefault(x =>
                string.Equals(x.NumeroLote.Trim(), number,
                    StringComparison.OrdinalIgnoreCase));
            var allowSimilar = lot.AllowSimilarCreation;
            if (exact is not null)
            {
                lot.SelectedExistingLot = exact;
                number = exact.NumeroLote;
            }
            else
            {
                if (lot.SimilarLot is not null && !allowSimilar)
                {
                    TraceabilityError =
                        "Resuelve la advertencia de lote posiblemente equivalente antes de aplicar el control.";
                    return;
                }
            }
            lotValues.Add(new CompraReceiptLotValue(number,
                lot.QuantityBase, lot.ManufactureDate, lot.ExpirationDate,
                allowSimilar));
        }

        var seriesValues = TraceabilitySeries.Select(x =>
            new CompraReceiptSeriesValue(
                x.Number.Trim().ToUpperInvariant(),
                x.LotRowId.HasValue
                    ? TraceabilityLots.FirstOrDefault(lot =>
                        lot.RowId == x.LotRowId.Value)?.Number.Trim()
                        .ToUpperInvariant()
                    : null)).ToList();
        TraceabilityLine.SetTraceability(lotValues, seriesValues);
        IsTraceabilityEditorOpen = false;
        TraceabilityLine = null;
        TraceabilityError = null;
        NotifyWizardState();
    }

    private CompraReceiptLotEditorViewModel CreateLotEditor()
    {
        var editor = new CompraReceiptLotEditorViewModel();
        editor.PropertyChanged += ReceiptLotPropertyChanged;
        return editor;
    }

    private void ReceiptLotPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (sender is not CompraReceiptLotEditorViewModel lot) return;
        TraceabilityError = null;
        if (e.PropertyName == nameof(CompraReceiptLotEditorViewModel.Number))
            ResolveReceiptLot(lot);
        NotifyTraceabilitySummary();
    }

    private void ResolveReceiptLot(CompraReceiptLotEditorViewModel lot)
    {
        var number = lot.Number.Trim();
        lot.Suggestions.Clear();
        if (number.Length > 0)
            foreach (var suggestion in ExistingReceiptLots
                         .Where(x => AjusteInventarioRules.CoincideBusquedaLote(
                             x.NumeroLote, number))
                         .OrderBy(x => AjusteInventarioRules
                             .PrioridadBusquedaLote(x.NumeroLote, number))
                         .ThenBy(x => x.NumeroLote.Length)
                         .ThenBy(x => x.NumeroLote))
                lot.Suggestions.Add(suggestion);
        lot.ShowSuggestions = number.Length > 0 && lot.Suggestions.Count > 0;
        lot.AllowSimilarCreation = false;
        if (number.Length == 0)
        {
            lot.SelectedExistingLot = null;
            lot.SimilarLot = null;
            return;
        }
        var repeated = TraceabilityLots.FirstOrDefault(x =>
            !ReferenceEquals(x, lot) &&
            AjusteInventarioRules.NormalizarLoteExacto(x.Number) ==
            AjusteInventarioRules.NormalizarLoteExacto(number));
        if (repeated is not null)
        {
            lot.SelectedExistingLot = null;
            lot.SimilarLot = null;
            TraceabilityError =
                $"El lote {number.ToUpperInvariant()} ya está incluido. Modifica la cantidad de la fila existente.";
            return;
        }
        var exact = ExistingReceiptLots.FirstOrDefault(x =>
            string.Equals(x.NumeroLote.Trim(), number,
                StringComparison.OrdinalIgnoreCase));
        if (exact is not null)
        {
            ApplyExistingReceiptLot(lot, exact);
            return;
        }
        lot.SelectedExistingLot = null;
        lot.ManufactureDate = null;
        lot.ExpirationDate = null;
        lot.SimilarLot = ExistingReceiptLots.FirstOrDefault(x =>
            AjusteInventarioRules.EsPosibleEquivalente(
                x.NumeroLote, number));
    }

    private static void ApplyExistingReceiptLot(
        CompraReceiptLotEditorViewModel editor,
        EstadoControlLoteDto existing)
    {
        editor.SelectedExistingLot = existing;
        editor.Number = existing.NumeroLote;
        editor.ManufactureDate = existing.FechaElaboracion;
        editor.ExpirationDate = existing.FechaCaducidad;
        editor.SimilarLot = null;
        editor.AllowSimilarCreation = false;
        editor.ShowSuggestions = false;
    }

    private string? ValidateTraceability()
    {
        if (TraceabilityLine is null) return "No se seleccionó un producto.";
        const decimal tolerance = 0.000001m;
        var required = TraceabilityRequiredBase;
        var today = DateTime.Today;

        if (TraceabilityLine.HandlesLots)
        {
            var lotError = AjusteInventarioRules.ValidarLotes(required,
                TraceabilityLots.Select(x => new LoteAjusteSnapshot(
                    x.Number, x.QuantityBase, x.ExistingStock,
                    x.IsExisting)).ToList(), false);
            if (lotError is not null) return lotError;
            if (TraceabilityLots.Any(x => x.SimilarLot is not null &&
                                          !x.AllowSimilarCreation))
                return "Resuelve la advertencia de lote posiblemente equivalente antes de continuar.";
            foreach (var lot in TraceabilityLots)
            {
                if (!lot.IsExisting && lot.ManufactureDate?.Date > today)
                    return $"La elaboración del lote {lot.Number} no puede ser futura.";
                if (!lot.IsExisting && TraceabilityLine.HandlesExpiration &&
                    !lot.ExpirationDate.HasValue)
                    return $"La caducidad del lote {lot.Number} es obligatoria.";
                if (!lot.IsExisting && lot.ExpirationDate?.Date <= today)
                    return $"La caducidad del lote {lot.Number} debe ser posterior a hoy.";
                if (!lot.IsExisting && lot.ManufactureDate.HasValue &&
                    lot.ExpirationDate.HasValue &&
                    lot.ManufactureDate.Value.Date >
                    lot.ExpirationDate.Value.Date)
                    return $"La elaboración del lote {lot.Number} no puede ser posterior a su caducidad.";
            }
        }

        if (TraceabilityLine.HandlesSeries)
        {
            var seriesSnapshots = TraceabilitySeries.Select(x =>
                new SerieAjusteSnapshot(x.Number,
                    x.LotRowId.HasValue
                        ? TraceabilityLots.FirstOrDefault(lot =>
                            lot.RowId == x.LotRowId.Value)?.Number
                        : null)).ToList();
            var seriesError = AjusteInventarioRules.ValidarSeriesNuevas(
                required, seriesSnapshots, _existingReceiptSeries);
            if (seriesError is not null) return seriesError;
            if (TraceabilityLine.HandlesLots)
            {
                if (TraceabilitySeries.Any(x =>
                        !x.LotRowId.HasValue || TraceabilityLots.All(lot =>
                            lot.RowId != x.LotRowId.Value)))
                    return "Cada serie debe asociarse con uno de los lotes ingresados.";
                foreach (var lot in TraceabilityLots)
                {
                    var count = TraceabilitySeries.Count(x =>
                        x.LotRowId == lot.RowId);
                    if (Math.Abs(count - lot.QuantityBase) > tolerance)
                        return $"El lote {lot.Number} requiere {lot.QuantityBase:0} serie(s), pero tiene {count}.";
                }
            }
        }
        return null;
    }

    private void NotifyTraceabilitySummary()
    {
        OnPropertyChanged(nameof(TraceabilityHandlesLots));
        OnPropertyChanged(nameof(TraceabilityHandlesSeries));
        OnPropertyChanged(nameof(TraceabilityHandlesExpiration));
        OnPropertyChanged(nameof(TraceabilityRequiredBase));
        OnPropertyChanged(nameof(TraceabilityAssignedLots));
        OnPropertyChanged(nameof(TraceabilityPendingLots));
        OnPropertyChanged(nameof(TraceabilityAssignedSeries));
        OnPropertyChanged(nameof(TraceabilityPendingSeries));
        OnPropertyChanged(nameof(TraceabilityAssignedControl));
        OnPropertyChanged(nameof(TraceabilityPendingControl));
        OnPropertyChanged(nameof(TraceabilityHasExcess));
    }

    private void ClearImportLines()
    {
        foreach (var line in LineasImportadas) line.Dispose();
        LineasImportadas.Clear();
    }

    private void ClearManualLines()
    {
        foreach (var line in ManualLines)
            line.PropertyChanged -= OnManualLinePropertyChanged;
        ManualLines.Clear();
        NotifyManualTotals();
    }

    private void OnManualLinePropertyChanged(
        object? sender, PropertyChangedEventArgs e) => NotifyManualTotals();

    private void NotifyManualTotals()
    {
        OnPropertyChanged(nameof(ManualGrossSubtotal));
        OnPropertyChanged(nameof(ManualDiscountTotal));
        OnPropertyChanged(nameof(ManualSubtotal));
        OnPropertyChanged(nameof(ManualTaxTotal));
        OnPropertyChanged(nameof(ManualTotal));
    }

    partial void OnManualPurchaseTypeChanged(string value)
    {
        OnPropertyChanged(nameof(ManualIsInvoiced));
        if (ManualIsInvoiced)
        {
            ManualDocumentType ??= DocumentTypes.FirstOrDefault(x =>
                x.Codigo == "01");
            return;
        }

        ManualDocumentType = null;
        ManualDocumentNumber = string.Empty;
        ManualDocumentEstablishment = string.Empty;
        ManualDocumentEmissionPoint = string.Empty;
        ManualDocumentSequential = string.Empty;
    }

    partial void OnManualSupplierChanged(CompraProveedorItemDto? value)
    {
        OnPropertyChanged(nameof(HasManualSupplierSelected));
        if (_updatingManualSupplierSearch || value is null) return;
        _updatingManualSupplierSearch = true;
        try { ManualSupplierSearchText = value.Display; }
        finally { _updatingManualSupplierSearch = false; }
    }

    partial void OnManualSupplierSearchTextChanged(string value)
    {
        if (_updatingManualSupplierSearch) return;
        if (ManualSupplier is not null &&
            !string.Equals(value.Trim(), ManualSupplier.Display,
                StringComparison.OrdinalIgnoreCase))
            ManualSupplier = null;

        ManualSupplierSuggestions.Clear();
        var search = value.Trim();
        if (search.Length == 0)
        {
            ShowManualSupplierSuggestions = false;
            OnPropertyChanged(nameof(ManualSupplierHasMatches));
            return;
        }

        var tokens = search.Split(' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
        var matches = ManualSuppliers
            .Where(x => tokens.All(token =>
                x.Identificacion.Contains(token,
                    StringComparison.OrdinalIgnoreCase) ||
                x.Nombre.Contains(token,
                    StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x =>
                x.Identificacion.StartsWith(search,
                    StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(x => x.Nombre.StartsWith(search,
                StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Nombre)
            .Take(8);
        foreach (var match in matches)
            ManualSupplierSuggestions.Add(match);
        ShowManualSupplierSuggestions = true;
        OnPropertyChanged(nameof(ManualSupplierHasMatches));
    }

    partial void OnManualDocumentEstablishmentChanged(string value) =>
        NormalizeManualDocumentSegment(value, 3,
            normalized => ManualDocumentEstablishment = normalized);

    partial void OnManualDocumentEmissionPointChanged(string value) =>
        NormalizeManualDocumentSegment(value, 3,
            normalized => ManualDocumentEmissionPoint = normalized);

    partial void OnManualDocumentSequentialChanged(string value) =>
        NormalizeManualDocumentSegment(value, 9,
            normalized => ManualDocumentSequential = normalized);

    public void PadManualDocumentSegments()
    {
        if (!ManualIsInvoiced) return;
        ManualDocumentEstablishment = PadSegment(
            ManualDocumentEstablishment, 3);
        ManualDocumentEmissionPoint = PadSegment(
            ManualDocumentEmissionPoint, 3);
        ManualDocumentSequential = PadSegment(
            ManualDocumentSequential, 9);
        ComposeManualDocumentNumber();
    }

    private void NormalizeManualInvoiceNumber()
    {
        if (!ManualIsInvoiced) return;
        PadManualDocumentSegments();
        ManualDocumentType = DocumentTypes.FirstOrDefault(x =>
            x.Codigo == "01");
    }

    private void NormalizeManualDocumentSegment(
        string value, int maxLength, Action<string> assign)
    {
        var normalized = new string(value.Where(char.IsDigit)
            .Take(maxLength).ToArray());
        if (!string.Equals(value, normalized, StringComparison.Ordinal))
        {
            assign(normalized);
            return;
        }
        ComposeManualDocumentNumber();
    }

    private void ComposeManualDocumentNumber() =>
        ManualDocumentNumber = string.Join("-",
            ManualDocumentEstablishment,
            ManualDocumentEmissionPoint,
            ManualDocumentSequential);

    private static string PadSegment(string value, int length) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.PadLeft(length, '0');

    private static bool IsValidManualInvoiceNumber(string value)
    {
        var parts = value.Split('-');
        return parts.Length == 3 && parts[0].Length == 3 &&
            parts[1].Length == 3 && parts[2].Length == 9 &&
            parts.All(x => x.All(char.IsDigit));
    }

    partial void OnManualIsCreditChanged(bool value)
    {
        if (!value)
        {
            ManualDueDate = null;
            return;
        }

        FocusManualDueDateRequested?.Invoke();
    }

    public void Dispose()
    {
        CancelOperation();
        _operationCancellation?.Dispose();
        ClearImportLines();
        ClearManualLines();
        ProductForm.CloseRequested -= OnProductFormClosed;
        ProductForm.ProductSaved -= OnProductSaved;
        ProveedorForm.CloseRequested -= OnSupplierFormClosed;
        ProveedorForm.Saved -= OnSupplierSaved;
        _session.EmpresaActivaChanged -= OnCompanyChanged;
    }
}

public sealed record CompraEstadoFiltroItem(string Value, string Text);

public partial class CompraImportLineaViewModel : ObservableObject, IDisposable
{
    private readonly Func<string?, long?, CancellationToken,
        Task<IReadOnlyList<CandidatoProductoCompraDto>>> _searchPresentations;
    private CancellationTokenSource? _searchCancellation;

    public int Order { get; }
    public string? MainCode { get; }
    public string? AuxiliaryCode { get; }
    public string Description { get; }
    public decimal Quantity { get; }
    public decimal UnitPrice { get; }
    public decimal LineTotal { get; }
    public IReadOnlyList<CandidatoProductoCompraDto> Candidates { get; }
    public IReadOnlyList<CompraCuentaContableItemDto> AccountingAccounts { get; }
    public IReadOnlyList<CompraAccountingClassificationItem> AccountingClassifications
        { get; } = CompraAccountingClassificationItem.All;
    public ObservableCollection<CandidatoProductoCompraDto> ManualCandidates
        { get; } = [];

    [ObservableProperty] private bool isNonInventory;
    [ObservableProperty] private bool isBonus;
    [ObservableProperty] private bool rememberEquivalence;
    [ObservableProperty] private CandidatoProductoCompraDto? selectedCandidate;
    [ObservableProperty] private CandidatoProductoCompraDto?
        selectedManualCandidate;
    [ObservableProperty] private long? productId;
    [ObservableProperty] private long? presentationId;
    [ObservableProperty] private string productDisplay = "Sin relacionar";
    [ObservableProperty] private string matchStatus = "NO RELACIONADO";
    [ObservableProperty] private bool isAutomaticallyResolved;
    [ObservableProperty] private bool isManualSearchVisible;
    [ObservableProperty] private bool isSearching;
    [ObservableProperty] private string manualSearchText = string.Empty;
    [ObservableProperty] private string searchMessage = string.Empty;
    [ObservableProperty] private string accountingClassification = "GASTO";
    [ObservableProperty] private CompraCuentaContableItemDto?
        selectedAccountingAccount;
    public decimal FactorConversion { get; private set; } = 1m;
    public bool HandlesLots { get; private set; }
    public bool HandlesSeries { get; private set; }
    public bool HandlesExpiration { get; private set; }

    public bool IsClassified => PresentationId.HasValue ||
        (IsNonInventory && SelectedAccountingAccount is not null &&
         AccountingClassification is "GASTO" or "ACTIVO" or "OTRO");
    public bool NeedsResolution => !IsClassified;
    public bool HasSuggestions => Candidates.Count > 0 && NeedsResolution;
    public bool HasManualResults => ManualCandidates.Count > 0;
    public bool IsManuallyResolved => PresentationId.HasValue &&
                                      !IsAutomaticallyResolved;
    public bool ShowSearchAction => !IsAutomaticallyResolved &&
                                    !IsNonInventory &&
                                    !IsManualSearchVisible;
    public bool ShowCreateAction => NeedsResolution && !IsNonInventory;
    public bool ShowNonInventoryOption => NeedsResolution || IsNonInventory;
    public bool ShowRememberEquivalence => IsManuallyResolved &&
        (!string.IsNullOrWhiteSpace(MainCode) ||
         !string.IsNullOrWhiteSpace(AuxiliaryCode));
    public bool ShowClearRelationAction => PresentationId.HasValue &&
                                            !IsNonInventory;
    public bool ShowBonusOption => PresentationId.HasValue && !IsNonInventory;
    public string SearchActionText => IsManuallyResolved
        ? "Cambiar producto" : "Buscar producto";

    public CompraImportLineaViewModel(
        DetalleFacturaCompraXmlDto xml,
        LineaCompraResueltaDto? resolved,
        Func<string?, long?, CancellationToken,
            Task<IReadOnlyList<CandidatoProductoCompraDto>>> searchPresentations,
        IReadOnlyList<CompraCuentaContableItemDto>? accountingAccounts = null)
    {
        _searchPresentations = searchPresentations;
        AccountingAccounts = accountingAccounts ?? [];
        Order = xml.Orden;
        MainCode = xml.CodigoPrincipal;
        AuxiliaryCode = xml.CodigoAuxiliar;
        Description = xml.Descripcion;
        Quantity = xml.Cantidad;
        UnitPrice = xml.PrecioUnitario;
        LineTotal = xml.PrecioTotalSinImpuesto;
        Candidates = resolved?.Candidatos ?? [];
        if (resolved?.Estado == "RECONOCIDA" &&
            resolved.ProductoPresentacionId.HasValue)
        {
            ProductId = resolved.ProductoId;
            PresentationId = resolved.ProductoPresentacionId;
            ProductDisplay = $"{resolved.ProductoNombre} · {resolved.PresentacionNombre}";
            MatchStatus = "AUTOMÁTICO";
            IsAutomaticallyResolved = true;
            FactorConversion = resolved.FactorConversion ?? 1m;
            HandlesLots = resolved.ManejaLotes;
            HandlesSeries = resolved.ManejaSeries;
            HandlesExpiration = resolved.ManejaFechaCaducidad;
        }
    }

    partial void OnSelectedCandidateChanged(CandidatoProductoCompraDto? value)
    {
        if (value is null) return;
        ApplyManualCandidate(value);
    }

    partial void OnSelectedManualCandidateChanged(
        CandidatoProductoCompraDto? value)
    {
        if (value is null) return;
        ApplyManualCandidate(value);
    }

    public void ApplyManualCandidate(CandidatoProductoCompraDto value)
    {
        IsNonInventory = false;
        IsAutomaticallyResolved = false;
        ProductId = value.ProductoId;
        PresentationId = value.ProductoPresentacionId;
        ProductDisplay = $"{value.ProductoConMarca} · {value.PresentacionNombre}";
        MatchStatus = "RELACIONADO";
        FactorConversion = value.FactorConversion;
        HandlesLots = value.ManejaLotes;
        HandlesSeries = value.ManejaSeries;
        HandlesExpiration = value.ManejaFechaCaducidad;
        RememberEquivalence = !string.IsNullOrWhiteSpace(MainCode) ||
                              !string.IsNullOrWhiteSpace(AuxiliaryCode);
        IsManualSearchVisible = false;
        NotifyResolutionState();
    }

    partial void OnIsNonInventoryChanged(bool value)
    {
        if (value)
        {
            CancelSearch();
            ProductId = null;
            PresentationId = null;
            SelectedCandidate = null;
            SelectedManualCandidate = null;
            ProductDisplay = "No inventariable";
            MatchStatus = "NO INVENTARIO";
            ResetInventoryControl();
            IsManualSearchVisible = false;
            RememberEquivalence = false;
            SelectedAccountingAccount ??= AccountingAccounts.FirstOrDefault(x =>
                x.Codigo.StartsWith("5", StringComparison.Ordinal)) ??
                AccountingAccounts.FirstOrDefault();
        }
        else if (!PresentationId.HasValue)
        {
            ProductDisplay = "Sin relacionar";
            MatchStatus = "NO RELACIONADO";
        }
        NotifyResolutionState();
    }

    [RelayCommand]
    private void SetNonInventory(bool? value) =>
        IsNonInventory = value == true;

    partial void OnPresentationIdChanged(long? value)
    {
        if (!value.HasValue) IsBonus = false;
        NotifyResolutionState();
    }

    partial void OnSelectedAccountingAccountChanged(
        CompraCuentaContableItemDto? value) => NotifyResolutionState();

    partial void OnAccountingClassificationChanged(string value) =>
        NotifyResolutionState();

    partial void OnIsAutomaticallyResolvedChanged(bool value) =>
        NotifyResolutionState();

    partial void OnIsManualSearchVisibleChanged(bool value) =>
        NotifyResolutionState();

    partial void OnManualSearchTextChanged(string value)
    {
        CancelSearch();
        ManualCandidates.Clear();
        SelectedManualCandidate = null;
        OnPropertyChanged(nameof(HasManualResults));
        if (!IsManualSearchVisible) return;
        if (value.Trim().Length < 2)
        {
            SearchMessage = "Escribe al menos 2 caracteres.";
            return;
        }
        _searchCancellation = new CancellationTokenSource();
        _ = SearchAsync(value.Trim(), _searchCancellation.Token);
    }

    [RelayCommand]
    private void OpenManualSearch()
    {
        IsManualSearchVisible = true;
        SearchMessage = "Busca por código, código de barras, nombre, modelo o presentación.";
    }

    [RelayCommand]
    private void CloseManualSearch()
    {
        CancelSearch();
        IsManualSearchVisible = false;
        ManualSearchText = string.Empty;
        ManualCandidates.Clear();
        SearchMessage = string.Empty;
        OnPropertyChanged(nameof(HasManualResults));
    }

    [RelayCommand]
    private void ClearRelation()
    {
        CancelSearch();
        IsBonus = false;
        ProductId = null;
        PresentationId = null;
        SelectedCandidate = null;
        SelectedManualCandidate = null;
        IsAutomaticallyResolved = false;
        IsManualSearchVisible = false;
        ManualSearchText = string.Empty;
        ManualCandidates.Clear();
        SearchMessage = string.Empty;
        RememberEquivalence = false;
        ProductDisplay = "Sin relacionar";
        MatchStatus = "NO RELACIONADO";
        ResetInventoryControl();
        OnPropertyChanged(nameof(HasManualResults));
        NotifyResolutionState();
    }

    private void ResetInventoryControl()
    {
        FactorConversion = 1m;
        HandlesLots = false;
        HandlesSeries = false;
        HandlesExpiration = false;
    }

    private async Task SearchAsync(string search, CancellationToken token)
    {
        try
        {
            await Task.Delay(300, token);
            IsSearching = true;
            SearchMessage = string.Empty;
            var results = await _searchPresentations(search, null, token);
            token.ThrowIfCancellationRequested();
            ManualCandidates.Clear();
            foreach (var candidate in results) ManualCandidates.Add(candidate);
            SearchMessage = results.Count == 0
                ? "No se encontraron productos. Puedes crear uno nuevo."
                : $"{results.Count} resultado(s).";
            OnPropertyChanged(nameof(HasManualResults));
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            SearchMessage = "No fue posible buscar productos.";
        }
        finally
        {
            if (!token.IsCancellationRequested) IsSearching = false;
        }
    }

    private void NotifyResolutionState()
    {
        OnPropertyChanged(nameof(IsClassified));
        OnPropertyChanged(nameof(NeedsResolution));
        OnPropertyChanged(nameof(HasSuggestions));
        OnPropertyChanged(nameof(IsManuallyResolved));
        OnPropertyChanged(nameof(ShowSearchAction));
        OnPropertyChanged(nameof(ShowCreateAction));
        OnPropertyChanged(nameof(ShowNonInventoryOption));
        OnPropertyChanged(nameof(ShowRememberEquivalence));
        OnPropertyChanged(nameof(ShowClearRelationAction));
        OnPropertyChanged(nameof(ShowBonusOption));
        OnPropertyChanged(nameof(SearchActionText));
    }

    private void CancelSearch()
    {
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
        IsSearching = false;
    }

    public void Dispose() => CancelSearch();
}

public partial class CompraManualLineViewModel : ObservableObject
{
    private readonly IReadOnlyList<CompraPresentacionItemDto> _presentations;
    private bool _updatingProductSearch;
    public IReadOnlyList<CompraCuentaContableItemDto> AccountingAccounts { get; }
    public IReadOnlyList<CompraTarifaImpuestoItemDto> TaxRates { get; }
    public IReadOnlyList<CompraAccountingClassificationItem> AccountingClassifications
        { get; } = CompraAccountingClassificationItem.All;
    public ObservableCollection<CompraPresentacionItemDto> ProductSuggestions
        { get; } = [];
    public ObservableCollection<CompraPresentacionItemDto>
        DescriptionSuggestions { get; } = [];
    [ObservableProperty] private string description = string.Empty;
    [ObservableProperty] private CompraPresentacionItemDto?
        selectedDescriptionSuggestion;
    [ObservableProperty] private bool showDescriptionSuggestions;
    [ObservableProperty] private bool isInventory = true;
    [ObservableProperty] private CompraPresentacionItemDto? selectedPresentation;
    [ObservableProperty] private string productSearchText = string.Empty;
    [ObservableProperty] private bool showProductSuggestions;
    [ObservableProperty] private decimal quantity = 1m;
    [ObservableProperty] private decimal unitPrice;
    [ObservableProperty] private decimal discount;
    [ObservableProperty] private decimal tax;
    [ObservableProperty] private CompraTarifaImpuestoItemDto? selectedTaxRate;
    [ObservableProperty] private bool isBonus;
    [ObservableProperty] private string accountingClassification = "GASTO";
    [ObservableProperty] private CompraCuentaContableItemDto?
        selectedAccountingAccount;

    public CompraManualLineViewModel(
        IReadOnlyList<CompraPresentacionItemDto>? presentations = null,
        IReadOnlyList<CompraCuentaContableItemDto>? accountingAccounts = null,
        IReadOnlyList<CompraTarifaImpuestoItemDto>? taxRates = null)
    {
        _presentations = presentations ?? [];
        AccountingAccounts = accountingAccounts ?? [];
        TaxRates = taxRates ?? [];
    }

    public decimal Total => Quantity * UnitPrice - Discount + Tax;
    public bool HasSelectedPresentation => SelectedPresentation is not null;
    public bool ProductSearchHasNoMatches => IsInventory &&
        SelectedPresentation is null && ProductSearchText.Trim().Length >= 2 &&
        ProductSuggestions.Count == 0;
    public long? TaxRateId => IsInventory
        ? SelectedPresentation?.TarifaImpuestoId
        : SelectedTaxRate?.Id;
    public string TaxRateText => EffectiveTaxPercentage is decimal percentage
        ? $"{percentage:0.##} %" : "—";
    public string TaxSuggestionText => IsInventory
        ? SelectedPresentation?.PorcentajeImpuesto is decimal percentage
            ? $"{SelectedPresentation.NombreImpuesto ?? "IVA"}: {percentage:0.##} % · calculado automáticamente"
            : "Relaciona un producto con tarifa de IVA configurada."
        : SelectedTaxRate is null
            ? "Selecciona la tarifa indicada en el comprobante."
            : $"{SelectedTaxRate.Nombre} · calculado automáticamente";
    private decimal? EffectiveTaxPercentage => IsInventory
        ? SelectedPresentation?.PorcentajeImpuesto
        : SelectedTaxRate?.Porcentaje;

    [RelayCommand]
    private void SelectDescriptionSuggestion(
        CompraPresentacionItemDto? suggestion)
    {
        if (suggestion is null) return;
        SelectedPresentation = suggestion;
        SelectedDescriptionSuggestion = null;
        DescriptionSuggestions.Clear();
        ShowDescriptionSuggestions = false;
    }

    [RelayCommand]
    private void SelectProductSuggestion(
        CompraPresentacionItemDto? suggestion)
    {
        if (suggestion is null) return;
        SelectedPresentation = suggestion;
    }

    [RelayCommand]
    private void ClearProductRelation()
    {
        _updatingProductSearch = true;
        try
        {
            SelectedPresentation = null;
            ProductSearchText = string.Empty;
            ProductSuggestions.Clear();
            DescriptionSuggestions.Clear();
            ShowProductSuggestions = false;
            ShowDescriptionSuggestions = false;
            IsBonus = false;
            OnPropertyChanged(nameof(HasSelectedPresentation));
            OnPropertyChanged(nameof(ProductSearchHasNoMatches));
        }
        finally { _updatingProductSearch = false; }
    }

    partial void OnSelectedPresentationChanged(CompraPresentacionItemDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedPresentation));
        OnPropertyChanged(nameof(ProductSearchHasNoMatches));
        OnPropertyChanged(nameof(TaxRateId));
        OnPropertyChanged(nameof(TaxRateText));
        OnPropertyChanged(nameof(TaxSuggestionText));
        if (value is null)
        {
            IsBonus = false;
            UpdateSuggestedTax();
            return;
        }
        _updatingProductSearch = true;
        try
        {
            ProductSearchText = value.Display.ToUpperInvariant();
            ProductSuggestions.Clear();
            ShowProductSuggestions = false;
        }
        finally { _updatingProductSearch = false; }
        if (value is not null && string.IsNullOrWhiteSpace(Description))
            Description = value.Producto;
        UpdateSuggestedTax();
    }

    partial void OnDescriptionChanged(string value)
    {
        var uppercase = value.ToUpperInvariant();
        if (!string.Equals(value, uppercase, StringComparison.Ordinal))
        {
            Description = uppercase;
            return;
        }
        if (!IsInventory || SelectedPresentation is not null)
        {
            DescriptionSuggestions.Clear();
            ShowDescriptionSuggestions = false;
            return;
        }
        RefreshDescriptionSuggestions(value);
    }

    partial void OnProductSearchTextChanged(string value)
    {
        if (_updatingProductSearch || !IsInventory) return;
        var uppercase = value.ToUpperInvariant();
        if (!string.Equals(value, uppercase, StringComparison.Ordinal))
        {
            ProductSearchText = uppercase;
            return;
        }
        if (SelectedPresentation is not null &&
            !string.Equals(value.Trim(), SelectedPresentation.Display,
                StringComparison.OrdinalIgnoreCase))
            SelectedPresentation = null;
        RefreshProductSuggestions(value);
    }

    partial void OnShowProductSuggestionsChanged(bool value)
    {
        if (value && ProductSuggestions.Count == 0 && IsInventory)
            RefreshProductSuggestions(ProductSearchText);
    }

    private void RefreshProductSuggestions(string search)
    {
        ProductSuggestions.Clear();
        if (search.Trim().Length < 2)
        {
            ShowProductSuggestions = false;
            OnPropertyChanged(nameof(ProductSearchHasNoMatches));
            return;
        }
        var tokens = search.Trim().Split(' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
        var results = _presentations
            .Where(x => tokens.Length == 0 || tokens.All(token =>
                x.Codigo.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                x.Producto.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                x.Presentacion.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => x.Producto.StartsWith(search.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Producto)
            .ThenByDescending(x => x.EsPresentacionBase)
            .ThenBy(x => x.FactorConversion)
            .ThenBy(x => x.Presentacion)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .Take(20);
        foreach (var result in results)
            ProductSuggestions.Add(result);
        ShowProductSuggestions = ProductSuggestions.Count > 0;
        OnPropertyChanged(nameof(ProductSearchHasNoMatches));
    }

    private void RefreshDescriptionSuggestions(string search)
    {
        DescriptionSuggestions.Clear();
        if (search.Trim().Length < 3)
        {
            ShowDescriptionSuggestions = false;
            return;
        }
        var tokens = search.Trim().Split(' ',
            StringSplitOptions.RemoveEmptyEntries |
            StringSplitOptions.TrimEntries);
        var results = _presentations
            .Where(x => tokens.All(token =>
                x.Codigo.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                x.Producto.Contains(token, StringComparison.OrdinalIgnoreCase) ||
                x.Presentacion.Contains(token, StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(x => x.Producto.StartsWith(search.Trim(),
                StringComparison.OrdinalIgnoreCase))
            .ThenBy(x => x.Producto)
            .ThenByDescending(x => x.EsPresentacionBase)
            .ThenBy(x => x.FactorConversion)
            .ThenBy(x => x.Presentacion)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .Take(8);
        foreach (var result in results)
            DescriptionSuggestions.Add(result);
        ShowDescriptionSuggestions = DescriptionSuggestions.Count > 0;
    }

    partial void OnIsInventoryChanged(bool value)
    {
        OnPropertyChanged(nameof(TaxRateId));
        OnPropertyChanged(nameof(TaxRateText));
        OnPropertyChanged(nameof(TaxSuggestionText));
        if (value)
        {
            SelectedAccountingAccount = null;
            SelectedTaxRate = null;
            UpdateSuggestedTax();
            return;
        }
        IsBonus = false;
        SelectedPresentation = null;
        ProductSearchText = string.Empty;
        ProductSuggestions.Clear();
        DescriptionSuggestions.Clear();
        ShowProductSuggestions = false;
        ShowDescriptionSuggestions = false;
        SelectedAccountingAccount ??= AccountingAccounts.FirstOrDefault(x =>
            x.Codigo.StartsWith("5", StringComparison.Ordinal)) ??
            AccountingAccounts.FirstOrDefault();
        UpdateSuggestedTax();
    }

    partial void OnSelectedTaxRateChanged(CompraTarifaImpuestoItemDto? value)
    {
        OnPropertyChanged(nameof(TaxRateId));
        OnPropertyChanged(nameof(TaxRateText));
        OnPropertyChanged(nameof(TaxSuggestionText));
        UpdateSuggestedTax();
    }

    partial void OnQuantityChanged(decimal value)
    {
        UpdateSuggestedTax();
        OnPropertyChanged(nameof(Total));
    }

    partial void OnUnitPriceChanged(decimal value)
    {
        UpdateSuggestedTax();
        OnPropertyChanged(nameof(Total));
    }

    partial void OnDiscountChanged(decimal value)
    {
        UpdateSuggestedTax();
        OnPropertyChanged(nameof(Total));
    }

    partial void OnTaxChanged(decimal value) => OnPropertyChanged(nameof(Total));

    private void UpdateSuggestedTax()
    {
        if (EffectiveTaxPercentage is not decimal percentage)
        {
            Tax = 0m;
            return;
        }
        var taxableBase = Math.Max(0m, Quantity * UnitPrice - Discount);
        Tax = Math.Round(taxableBase * percentage / 100m, 2,
            MidpointRounding.AwayFromZero);
    }
}

public sealed record CompraAccountingClassificationItem(string Code, string Name)
{
    public static IReadOnlyList<CompraAccountingClassificationItem> All { get; } =
    [
        new("GASTO", "Gasto"),
        new("ACTIVO", "Activo"),
        new("OTRO", "Otro")
    ];
}

public partial class CompraReceiptLineViewModel : ObservableObject
{
    public int Order { get; }
    public long DetailId { get; }
    public long ProductId { get; }
    public string Description { get; }
    public string Product { get; }
    public string Presentation { get; }
    public string PresentationSummary =>
        $"{Presentation} · {(FactorConversion == 1m ? "BASE · ×1" : $"×{FactorConversion:0.######}")}";
    public decimal Invoiced { get; }
    public decimal Received { get; }
    public decimal Pending { get; }
    public decimal FactorConversion { get; }
    public bool HandlesLots { get; }
    public bool HandlesSeries { get; }
    public bool HandlesExpiration { get; }
    private IReadOnlyList<CompraReceiptLotValue> _traceabilityLots = [];
    private IReadOnlyList<CompraReceiptSeriesValue> _traceabilitySeries = [];

    [ObservableProperty] private decimal receiveNow;
    [ObservableProperty] private string lotsText = string.Empty;
    [ObservableProperty] private string seriesText = string.Empty;

    public bool NeedsTraceability => HandlesLots || HandlesSeries;
    public bool TraceabilityConfigured { get; private set; }
    public string TraceabilityButtonText => TraceabilityConfigured
        ? "Editar control" : "Configurar control";
    public string TraceabilitySummary => !NeedsTraceability
        ? "Control normal"
        : !TraceabilityConfigured
            ? HandlesLots && HandlesSeries
                ? "Lotes y series pendientes"
                : HandlesLots ? "Lotes pendientes" : "Series pendientes"
            : HandlesLots && HandlesSeries
                ? $"{_traceabilityLots.Count} lote(s) · {_traceabilitySeries.Count} serie(s)"
                : HandlesLots
                    ? $"{_traceabilityLots.Count} lote(s) configurado(s)"
                    : $"{_traceabilitySeries.Count} serie(s) configurada(s)";
    public IReadOnlyList<CompraReceiptLotValue> TraceabilityLots =>
        _traceabilityLots;
    public IReadOnlyList<CompraReceiptSeriesValue> TraceabilitySeries =>
        _traceabilitySeries;

    public CompraReceiptLineViewModel(CompraDetalleLineaDto line)
    {
        Order = line.Orden;
        DetailId = line.Id;
        ProductId = line.ProductoId;
        Description = line.Descripcion;
        Product = line.Producto ?? line.Descripcion;
        Presentation = line.Presentacion ?? "PRESENTACIÓN NO IDENTIFICADA";
        Invoiced = line.CantidadFacturada;
        Received = line.CantidadRecibida;
        Pending = line.CantidadPendiente;
        FactorConversion = line.FactorConversion;
        HandlesLots = line.ManejaLotes;
        HandlesSeries = line.ManejaSeries;
        HandlesExpiration = line.ManejaFechaCaducidad;
        ReceiveNow = Pending;
    }

    public CompraReceiptLineViewModel(CompraImportLineaViewModel line)
    {
        Order = line.Order;
        ProductId = line.ProductId.GetValueOrDefault();
        Description = line.Description;
        Product = line.ProductDisplay;
        Presentation = string.Empty;
        Invoiced = line.Quantity;
        Pending = line.Quantity;
        FactorConversion = line.FactorConversion;
        HandlesLots = line.HandlesLots;
        HandlesSeries = line.HandlesSeries;
        HandlesExpiration = line.HandlesExpiration;
        ReceiveNow = Pending;
    }

    public void SetTraceability(
        IReadOnlyList<CompraReceiptLotValue> lots,
        IReadOnlyList<CompraReceiptSeriesValue> series)
    {
        _traceabilityLots = lots;
        _traceabilitySeries = series;
        TraceabilityConfigured = true;
        OnPropertyChanged(nameof(TraceabilityConfigured));
        OnPropertyChanged(nameof(TraceabilityButtonText));
        OnPropertyChanged(nameof(TraceabilitySummary));
    }

    partial void OnReceiveNowChanged(decimal value)
    {
        if (!TraceabilityConfigured) return;
        _traceabilityLots = [];
        _traceabilitySeries = [];
        TraceabilityConfigured = false;
        OnPropertyChanged(nameof(TraceabilityConfigured));
        OnPropertyChanged(nameof(TraceabilityButtonText));
        OnPropertyChanged(nameof(TraceabilitySummary));
    }

    public bool RestoreDraftFrom(CompraReceiptLineViewModel previous)
    {
        if (previous.Order != Order || previous.ProductId != ProductId ||
            previous.FactorConversion != FactorConversion ||
            previous.HandlesLots != HandlesLots ||
            previous.HandlesSeries != HandlesSeries ||
            previous.HandlesExpiration != HandlesExpiration)
            return false;

        ReceiveNow = Math.Min(previous.ReceiveNow, Pending);
        LotsText = previous.LotsText;
        SeriesText = previous.SeriesText;
        if (previous.TraceabilityConfigured)
            SetTraceability(previous.TraceabilityLots.ToList(),
                previous.TraceabilitySeries.ToList());
        return true;
    }

    public ReceiptLineBuildResult BuildRequest()
    {
        if (ReceiveNow <= 0 || ReceiveNow - Pending > 0.000001m)
            return ReceiptLineBuildResult.Fail(
                "la cantidad debe ser mayor que cero y no superar el pendiente.");
        var baseQuantity = ReceiveNow * FactorConversion;
        if (NeedsTraceability && !TraceabilityConfigured &&
            string.IsNullOrWhiteSpace(LotsText) &&
            string.IsNullOrWhiteSpace(SeriesText))
            return ReceiptLineBuildResult.Fail(
                "configura los lotes o series requeridos antes de guardar.");
        if (TraceabilityConfigured)
        {
            var configuredLots = _traceabilityLots.Select(x =>
                new CompraRecepcionLoteRequest
                {
                    NumeroLote = x.Number,
                    CantidadBase = x.QuantityBase,
                    FechaElaboracion = x.ManufactureDate.HasValue
                        ? DateOnly.FromDateTime(x.ManufactureDate.Value) : null,
                    FechaCaducidad = x.ExpirationDate.HasValue
                        ? DateOnly.FromDateTime(x.ExpirationDate.Value) : null,
                    PermitirCrearLoteSimilar = x.AllowSimilarCreation
                }).ToList();
            var configuredSeries = _traceabilitySeries.Select(x =>
                new CompraRecepcionSerieRequest
                {
                    NumeroSerie = x.Number,
                    NumeroLote = x.LotNumber
                }).ToList();
            return ReceiptLineBuildResult.Ok(
                new ConfirmarCompraRecepcionLineaRequest
                {
                    CompraDetalleId = DetailId,
                    CantidadPresentacion = ReceiveNow,
                    Lotes = configuredLots,
                    Series = configuredSeries
                });
        }
        var lots = new List<CompraRecepcionLoteRequest>();
        if (HandlesLots)
        {
            foreach (var raw in LotsText.Split(';',
                         StringSplitOptions.RemoveEmptyEntries |
                         StringSplitOptions.TrimEntries))
            {
                var parts = raw.Split('|', StringSplitOptions.TrimEntries);
                if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[0]) ||
                    !decimal.TryParse(parts[1],
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.CurrentCulture,
                        out var quantity) || quantity <= 0)
                    return ReceiptLineBuildResult.Fail(
                        "usa LOTE|cantidad|elaboración|caducidad y separa lotes con punto y coma.");
                if (!TryDate(parts, 2, out var manufacture) ||
                    !TryDate(parts, 3, out var expiration))
                    return ReceiptLineBuildResult.Fail(
                        "las fechas de lote deben usar yyyy-MM-dd.");
                if (HandlesExpiration && !expiration.HasValue)
                    return ReceiptLineBuildResult.Fail(
                        "la fecha de caducidad es obligatoria.");
                lots.Add(new CompraRecepcionLoteRequest
                {
                    NumeroLote = parts[0],
                    CantidadBase = quantity,
                    FechaElaboracion = manufacture,
                    FechaCaducidad = expiration
                });
            }
            if (lots.Count == 0 || Math.Abs(lots.Sum(x => x.CantidadBase) -
                                           baseQuantity) > 0.000001m)
                return ReceiptLineBuildResult.Fail(
                    $"los lotes deben sumar {baseQuantity:N6} unidades base.");
        }
        else if (!string.IsNullOrWhiteSpace(LotsText))
            return ReceiptLineBuildResult.Fail("el producto no maneja lotes.");

        var serials = SeriesText.Split([';', '\r', '\n'],
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(raw => raw.Split('@', StringSplitOptions.TrimEntries))
            .Select(parts => new CompraRecepcionSerieRequest
            {
                NumeroSerie = parts[0],
                NumeroLote = parts.Length > 1 ? parts[1] : null
            }).ToList();
        if (HandlesSeries &&
            (baseQuantity != decimal.Truncate(baseQuantity) ||
             serials.Count != (int)baseQuantity ||
             serials.Select(x => x.NumeroSerie)
                 .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
             serials.Count))
            return ReceiptLineBuildResult.Fail(
                $"debe haber una serie única por cada una de las {baseQuantity:N0} unidades base.");
        if (!HandlesSeries && serials.Count > 0)
            return ReceiptLineBuildResult.Fail("el producto no maneja series.");
        if (HandlesLots && HandlesSeries &&
            serials.Any(x => string.IsNullOrWhiteSpace(x.NumeroLote)))
            return ReceiptLineBuildResult.Fail(
                "cada serie debe indicar su lote con SERIE@LOTE.");
        return ReceiptLineBuildResult.Ok(new ConfirmarCompraRecepcionLineaRequest
        {
            CompraDetalleId = DetailId,
            CantidadPresentacion = ReceiveNow,
            Lotes = lots,
            Series = serials
        });
    }

    private static bool TryDate(string[] parts, int index, out DateOnly? value)
    {
        value = null;
        if (parts.Length <= index || string.IsNullOrWhiteSpace(parts[index]))
            return true;
        if (!DateOnly.TryParseExact(parts[index], "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var parsed))
            return false;
        value = parsed;
        return true;
    }
}

public sealed record CompraReceiptLotValue(
    string Number,
    decimal QuantityBase,
    DateTime? ManufactureDate,
    DateTime? ExpirationDate,
    bool AllowSimilarCreation);

public sealed record CompraReceiptSeriesValue(
    string Number,
    string? LotNumber);

public partial class CompraReceiptLotEditorViewModel : ObservableObject
{
    public Guid RowId { get; } = Guid.NewGuid();
    public ObservableCollection<EstadoControlLoteDto> Suggestions { get; } = [];
    [ObservableProperty] private EstadoControlLoteDto? selectedExistingLot;
    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty] private decimal quantityBase;
    [ObservableProperty] private DateTime? manufactureDate;
    [ObservableProperty] private DateTime? expirationDate;
    [ObservableProperty] private bool allowSimilarCreation;
    [ObservableProperty] private bool showSuggestions;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSimilarLot))]
    private EstadoControlLoteDto? similarLot;

    public bool IsExisting => SelectedExistingLot is not null;
    public bool IsNew => !IsExisting && !string.IsNullOrWhiteSpace(Number);
    public bool HasSimilarLot => SimilarLot is not null;
    public decimal ExistingStock => SelectedExistingLot?.StockActual ?? 0;

    partial void OnSelectedExistingLotChanged(EstadoControlLoteDto? value)
    {
        if (value is null) return;
        Number = value.NumeroLote;
        ManufactureDate = value.FechaElaboracion;
        ExpirationDate = value.FechaCaducidad;
        AllowSimilarCreation = false;
        SimilarLot = null;
        ShowSuggestions = false;
        OnPropertyChanged(nameof(IsExisting));
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(ExistingStock));
    }

    partial void OnNumberChanged(string value)
    {
        if (SelectedExistingLot is not null && !string.Equals(
                SelectedExistingLot.NumeroLote, value,
                StringComparison.OrdinalIgnoreCase))
            SelectedExistingLot = null;
        OnPropertyChanged(nameof(IsExisting));
        OnPropertyChanged(nameof(IsNew));
        OnPropertyChanged(nameof(ExistingStock));
    }
}

public partial class CompraReceiptSeriesEditorViewModel : ObservableObject
{
    [ObservableProperty] private string number = string.Empty;
    [ObservableProperty] private Guid? lotRowId;
}

public sealed record ReceiptLineBuildResult(
    ConfirmarCompraRecepcionLineaRequest? Request,
    string? Error)
{
    public static ReceiptLineBuildResult Ok(
        ConfirmarCompraRecepcionLineaRequest request) => new(request, null);
    public static ReceiptLineBuildResult Fail(string error) => new(null, error);
}

public sealed class PaginaCompraItemViewModel
{
    public int Numero { get; }
    public string Texto { get; }
    public bool EsActual { get; }
    public bool EsSeparador { get; }

    public PaginaCompraItemViewModel(int numero, bool esActual)
    {
        Numero = numero;
        Texto = numero.ToString();
        EsActual = esActual;
    }

    private PaginaCompraItemViewModel()
    {
        Texto = "…";
        EsSeparador = true;
    }

    public static PaginaCompraItemViewModel Separador() => new();
}
