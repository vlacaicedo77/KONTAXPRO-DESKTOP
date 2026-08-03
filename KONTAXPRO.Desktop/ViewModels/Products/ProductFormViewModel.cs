using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Common;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Application.Models.Inventario;
using KONTAXPRO.Application.Inventory;
using KONTAXPRO.Application.Products;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels.Products;

public partial class ProductFormViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IProductCatalogService _catalogService;
    private readonly IInventoryService _inventoryService;
    private readonly CurrentSession _currentSession;
    private readonly IMessageDialogService _messageDialogService;

    private CancellationTokenSource?
    _suggestionsCancellationTokenSource;
    private CancellationTokenSource? _correccionMensajeCancellationTokenSource;

    private long? _productoId;
    private string? _tipoCatalogoRapido;
    private readonly HashSet<long> _tarifasImpuestoIds = [];
    private readonly Dictionary<string, List<ProductoPrecioDto>>
        _preciosPorPresentacion = new(StringComparer.OrdinalIgnoreCase);
    public event Action? CloseRequested;
    public event Action<long>? ProductSaved;
    public event Action<long>? ExistingProductRequested;
    public event Action? PresentationAdded;
    public event Action? InitialFocusRequested;
    public ObservableCollection<ProductoSugerenciaDto>
    SugerenciasProductos
    { get; }
        = new();

    public ObservableCollection<CatalogItemDto> Categorias { get; }
        = new();

    public ObservableCollection<CatalogItemDto> Marcas { get; }
        = new();

    public ObservableCollection<CatalogItemDto> UnidadesMedida { get; }
        = new();

    public ObservableCollection<CatalogItemDto> TarifasImpuesto { get; }
        = new();

    public ObservableCollection<ListaPrecioEditorDto> ListasPrecio { get; }
        = new();

    public ObservableCollection<ProductoPresentacionDto>
        PresentacionesAdicionales { get; } = new();

    public ObservableCollection<PrecioListaEditorViewModel> Precios { get; } = new();
    public ObservableCollection<PrecioPresentacionConfiguradaViewModel>
        PreciosPresentacionesConfigurados { get; } = new();

    public ObservableCollection<ProductoExistenciaDto> Existencias { get; } = new();

    public ObservableCollection<ProductoPresentacionDto>
        PresentacionesInventarioInicial { get; } = new();

    public ObservableCollection<LoteInventarioInicialEditorViewModel>
        LotesInventarioInicial { get; } = new();
    public ObservableCollection<SerieInventarioInicialEditorViewModel>
        SeriesInventarioInicial { get; } = new();
    public ObservableCollection<ProductoInventarioInicialRequest>
        InventariosIniciales { get; } = new();
    public ObservableCollection<KardexItemDto> KardexItems { get; } = new();
    public ObservableCollection<ProductoPresentacionDto> PresentacionesOperacion { get; } = new();
    public ObservableCollection<ConversionBodegaEditorViewModel> ConversionBodegas { get; } = new();
    public ObservableCollection<string> TiposControlDestino { get; } = new();
    public ObservableCollection<CorreccionLoteEditorViewModel> LotesCorreccion { get; } = new();
    public ObservableCollection<CorreccionSerieEditorViewModel> SeriesCorreccion { get; } = new();
    public ObservableCollection<EstadoControlLoteDto> AjusteLotesDisponibles { get; } = new();
    public ObservableCollection<AjusteSerieDisponibleViewModel> AjusteSeriesDisponibles { get; } = new();
    public ObservableCollection<AjusteLoteEditorViewModel> AjusteLotes { get; } = new();
    public ObservableCollection<AjusteSerieNuevaEditorViewModel> AjusteSeriesNuevas { get; } = new();
    public ObservableCollection<MotivoOperacionInventarioDto> MotivosAjuste { get; } = new();
    public ObservableCollection<MotivoOperacionInventarioDto> MotivosConversion { get; } = new();
    public ObservableCollection<MotivoOperacionInventarioDto> MotivosCorreccion { get; } = new();
    private EstadoControlInventarioDto? _estadoControlAjuste;
    private readonly HashSet<string> _seriesExistentesAjuste =
        new(StringComparer.OrdinalIgnoreCase);
    private bool _restaurandoContextoAjuste;

    public ObservableCollection<string> TiposProducto { get; }
        = new()
        {
            "PRODUCTO",
            "SERVICIO"
        };

    public ObservableCollection<string> TiposControlInventario { get; }
        = new()
        {
            "NORMAL",
            "LOTE",
            "SERIE",
            "LOTE_Y_SERIE"
        };
    public IReadOnlyList<string> TiposAjuste { get; } = ["ENTRADA", "SALIDA"];

    public string TextoAyudaCodigoBarras =>
    SinCodigoBarras
        ? IsEditing
            ? "Este código de barras fue generado automáticamente por KONTAXPRO."
            : "KONTAXPRO generará automáticamente un código de barras al guardar."
        : "Ingresa o escanea el código de barras proporcionado por el fabricante.";

    [ObservableProperty]
    private string tituloFormulario = "Nuevo producto";

    [ObservableProperty]
    private bool isEditing;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isKardexOpen;

    [ObservableProperty]
    private bool isKardexLoading;

    [ObservableProperty] private bool isAjusteOpen;
    [ObservableProperty] private string ajusteTipo = "ENTRADA";
    [ObservableProperty] private long? ajusteBodegaId;
    [ObservableProperty] private long? ajustePresentacionId;
    [ObservableProperty] private decimal ajusteCantidad;
    [ObservableProperty] private decimal ajusteCostoPresentacion;
    [ObservableProperty] private string ajusteMotivo = string.Empty;
    [ObservableProperty] private long? ajusteMotivoId;
    [ObservableProperty] private string? ajusteObservacion;
    [ObservableProperty] private string? ajusteNumeroLote;
    [ObservableProperty] private string? ajusteSeriesTexto;
    [ObservableProperty] private bool ajusteManejaFechaCaducidad;
    [ObservableProperty] private bool isConversionOpen;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConversionDestinoLotes))]
    [NotifyPropertyChangedFor(nameof(ConversionDestinoSeries))]
    private string? conversionTipoNuevo;
    [ObservableProperty] private string conversionMotivo = string.Empty;
    [ObservableProperty] private long? conversionMotivoId;
    [ObservableProperty] private string conversionTipoAnterior = "NORMAL";
    [ObservableProperty] private string conversionUnidadBase = "UND";
    [ObservableProperty] private bool conversionControlCaducidad = true;
    [ObservableProperty] private int conversionDiasAnticipacionCaducidad = 30;
    [ObservableProperty] private bool isCorreccionControlOpen;
    [ObservableProperty] private string correccionMotivo = string.Empty;
    [ObservableProperty] private long? correccionMotivoId;
    [ObservableProperty] private bool isNuevoMotivoOpen;
    [ObservableProperty] private string nuevoMotivoNombre = string.Empty;
    [ObservableProperty] private string? nuevoMotivoDescripcion;
    [ObservableProperty] private string? nuevoMotivoError;
    [ObservableProperty] private bool isNuevoMotivoSaving;
    private string _nuevoMotivoDestino = string.Empty;
    private string _nuevoMotivoTipo = string.Empty;
    public bool PuedeCrearMotivo =>
        _currentSession.HasPermission("INVENTARIO_CREAR_MOTIVO");
    [ObservableProperty] private bool isCorreccionSaving;
    [ObservableProperty] private string? correccionMensajeExito;
    public bool ConversionDestinoLotes =>
        ConversionTipoNuevo is "LOTE" or "LOTE_Y_SERIE";
    public bool ConversionDestinoSeries =>
        ConversionTipoNuevo is "SERIE" or "LOTE_Y_SERIE";
    public bool MostrarConversionCaducidad => ConversionDestinoLotes;
    public bool MostrarColumnasConversionFecha =>
        MostrarConversionCaducidad && ConversionControlCaducidad;
    public bool PuedeConfirmarConversion =>
        !IsLoading &&
        !string.IsNullOrWhiteSpace(ConversionTipoNuevo) &&
        ConversionMotivoId.HasValue &&
        ConversionBodegas.All(x => x.DistribucionCompleta(
            ConversionDestinoLotes, ConversionDestinoSeries));

    [ObservableProperty]
    private string nombre = string.Empty;

    [ObservableProperty]
    private string? codigoInterno;

    [ObservableProperty]
    private string? descripcion;

    [ObservableProperty]
    private long? categoriaProductoId;

    [ObservableProperty]
    private long? marcaId;

    [ObservableProperty]
    private long? unidadMedidaBaseId;

    [ObservableProperty]
    private long? tarifaImpuestoId;

    [ObservableProperty]
    private long? listaPrecioBaseId;

    [ObservableProperty]
    private decimal precioBase;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CostoBaseInventarioInicial))]
    [NotifyPropertyChangedFor(nameof(CostoPresentacionPrecio))]
    private ProductoCostoDto costo = new();

    [ObservableProperty]
    private bool configurarPreciosAhora;

    [ObservableProperty]
    private string presentacionPrecioCodigo = "BASE";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarCapturaInventarioInicial))]
    private bool registrarInventarioInicial;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MostrarCapturaInventarioInicial))]
    [NotifyPropertyChangedFor(nameof(MostrarCancelarEntradaInicial))]
    private bool mostrarEntradaInicialExistente;

    [ObservableProperty]
    private bool puedeCompletarInventarioInicial = true;

    [ObservableProperty]
    private string? motivoNoPuedeCompletarInventarioInicial;

    [ObservableProperty]
    private long? inventarioBodegaId;

    [ObservableProperty]
    private string? inventarioUbicacion;

    [ObservableProperty]
    private decimal inventarioStockMinimo;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CantidadBaseInicial))]
    [NotifyPropertyChangedFor(nameof(CostoTotalInicial))]
    [NotifyPropertyChangedFor(nameof(CostoUnitarioBaseInicial))]
    private string inventarioPresentacionCodigo = "BASE";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CantidadBaseInicial))]
    [NotifyPropertyChangedFor(nameof(CostoTotalInicial))]
    [NotifyPropertyChangedFor(nameof(CostoUnitarioBaseInicial))]
    private decimal cantidadPresentacionesInicial;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CostoTotalInicial))]
    [NotifyPropertyChangedFor(nameof(CostoUnitarioBaseInicial))]
    private decimal costoPresentacionInicial;

    [ObservableProperty]
    private string? numeroLoteInicial;

    [ObservableProperty]
    private decimal cantidadLoteInicial;

    [ObservableProperty]
    private DateTime? fechaElaboracionInicial;

    [ObservableProperty]
    private DateTime? fechaCaducidadInicial;

    [ObservableProperty]
    private string? seriesInicialesTexto;

    [ObservableProperty]
    private string? numeroSerieInicial;

    [ObservableProperty]
    private string? loteSerieInicial;

    public decimal FactorInicial =>
        PresentacionesInventarioInicial
            .FirstOrDefault(x => x.Codigo == InventarioPresentacionCodigo)
            ?.FactorConversion ?? 1;
    public decimal CantidadBaseInicial =>
        ProductoNuevoRules.CalcularCantidadBase(
            CantidadPresentacionesInicial,
            FactorInicial);
    public decimal CostoTotalInicial =>
        ProductoNuevoRules.CalcularCostoTotal(
            CantidadPresentacionesInicial,
            CostoPresentacionInicial);
    public decimal CostoUnitarioBaseInicial =>
        ProductoNuevoRules.CalcularCostoUnitarioBase(
            CostoTotalInicial,
            CantidadBaseInicial);
    public string ResumenInventarioInicial
    {
        get
        {
            var presentacion = PresentacionesInventarioInicial
                .FirstOrDefault(x =>
                    x.Codigo == InventarioPresentacionCodigo);
            var nombreIngreso =
                presentacion?.Nombre ?? "PRESENTACIONES";
            var nombreBase = string.IsNullOrWhiteSpace(PresentacionNombre)
                ? "UNIDAD"
                : PresentacionNombre;
            return $"{CantidadPresentacionesInicial:0.##} {nombreIngreso} → " +
                   $"{CantidadBaseInicial:0.######} {nombreBase}\n" +
                   $"Costo total: {CostoTotalInicial:N2}  •  " +
                   $"Costo por {nombreBase}: {CostoUnitarioBaseInicial:N2}";
        }
    }
    public decimal CostoBaseInventarioInicial
    {
        get
        {
            var entradas = InventariosIniciales
                .Where(x => !IsEditing || !x.EsHistorico)
                .Select(x => new CostoInventarioEntrada(
                    x.CantidadPresentaciones *
                        (PresentacionesInventarioInicial.FirstOrDefault(p =>
                            p.Codigo == x.PresentacionCodigo)
                            ?.FactorConversion ?? 1),
                    x.CantidadPresentaciones *
                        x.CostoUnitarioPresentacion))
                .ToList();
            if (IsEditing && MostrarCapturaInventarioInicial &&
                CantidadPresentacionesInicial > 0)
                entradas.Add(new CostoInventarioEntrada(
                    CantidadBaseInicial,
                    CostoTotalInicial));
            if (!IsEditing)
                return entradas.Count == 0
                    ? CostoUnitarioBaseInicial
                    : ProductoNuevoRules.CalcularCostoPromedioPonderado(
                        entradas);

            return ProductoInventarioRules.CalcularCostoPromedioConPendientes(
                Existencias.Sum(x => x.StockActual),
                Costo.CostoPromedio,
                entradas);
        }
    }
    public decimal CostoPresentacionPrecio
    {
        get
        {
            var factor = PresentacionesInventarioInicial
                .FirstOrDefault(x => x.Codigo == PresentacionPrecioCodigo)
                ?.FactorConversion ?? 1;
            var costoBase = RegistrarInventarioInicial
                ? CostoBaseInventarioInicial
                : Costo.CostoPromedio;
            return costoBase * factor;
        }
    }
    public decimal TotalLotesInicial =>
        LotesInventarioInicial.Sum(x => x.CantidadBase);
    public decimal CantidadLotesPendiente =>
        CantidadBaseInicial - TotalLotesInicial;
    public int TotalSeriesInicial => SeriesInventarioInicial.Count;
    public decimal CantidadSeriesPendiente =>
        CantidadBaseInicial - TotalSeriesInicial;
    public bool PuedeConfigurarPrecios =>
        IsEditing || RegistrarInventarioInicial;
    public bool TieneEntradaInicialPendiente =>
        InventariosIniciales.Any(x => !x.EsHistorico);
    public bool MostrarCancelarEntradaInicial =>
        IsEditing && MostrarEntradaInicialExistente;

    [ObservableProperty]
    private string tipoProducto = "PRODUCTO";

    [ObservableProperty]
    private string tipoControlInventario = "NORMAL";

    [ObservableProperty]
    private bool manejaInventario = true;

    [ObservableProperty]
    private bool permiteVentaSinStock;

    [ObservableProperty]
    private bool alertaCaducidad = true;

    [ObservableProperty]
    private int diasAlertaCaducidad = 30;

    [ObservableProperty]
    private string? observacion;

    [ObservableProperty]
    private string? mensajeError;

    [ObservableProperty]
    private string? modelo;

    [ObservableProperty]
    private string presentacionNombre = "UNIDAD";

    [ObservableProperty]
    private string codigoBarras = string.Empty;

    [ObservableProperty]
    private bool sinCodigoBarras;

    [ObservableProperty]
    private string quickCatalogIcon = "ShapeOutline";

    [ObservableProperty]
    private bool isSearchingSuggestions;

    [ObservableProperty]
    private bool mostrarSugerencias;

    public bool HaySugerencias =>
    SugerenciasProductos.Count > 0;


    /*
     * ============================================================
     * CATÁLOGO RÁPIDO
     * ============================================================
     */

    [ObservableProperty]
    private bool isQuickCatalogOpen;

    [ObservableProperty]
    private string quickCatalogTitle = string.Empty;

    [ObservableProperty]
    private string quickCatalogLabel = string.Empty;

    [ObservableProperty]
    private string quickCatalogName = string.Empty;

    [ObservableProperty]
    private string? quickCatalogError;

    [ObservableProperty]
    private bool isQuickCatalogSaving;

    [ObservableProperty]
    private string campoSugerenciaActivo =
    string.Empty;

    public bool MostrarSugerenciasNombre =>
        MostrarSugerencias &&
        CampoSugerenciaActivo == "NOMBRE";

    public bool MostrarSugerenciasModelo =>
        MostrarSugerencias &&
        CampoSugerenciaActivo == "MODELO";

    public bool MostrarConfiguracionInventario =>
        TipoProducto == "PRODUCTO" &&
        ManejaInventario;

    public bool MostrarCapturaInventarioInicial =>
        IsEditing ? MostrarEntradaInicialExistente : RegistrarInventarioInicial;

    public bool MostrarConfiguracionCaducidad =>
        MostrarConfiguracionInventario &&
        TipoControlInventario is "LOTE" or "LOTE_Y_SERIE";
    public bool MostrarLoteAsociadoSeries =>
        TipoControlInventario == "LOTE_Y_SERIE";
    public bool MostrarCorreccionLotesSeries =>
        TipoControlInventario is "LOTE" or "SERIE" or "LOTE_Y_SERIE";
    public bool AjusteRequiereLotes =>
        TipoControlInventario is "LOTE" or "LOTE_Y_SERIE";
    public bool AjusteRequiereSeries =>
        TipoControlInventario is "SERIE" or "LOTE_Y_SERIE";
    public bool AjusteEsEntrada => AjusteTipo == "ENTRADA";
    public bool AjusteEsSalida => AjusteTipo == "SALIDA";
    public string AjusteEtiquetaCosto => AjusteEsEntrada
        ? "Costo presentación"
        : "Costo aplicado";
    public string AjusteAsteriscoCosto => AjusteEsEntrada ? " *" : string.Empty;
    public string AjusteAyudaCosto =>
        "Referencia basada en el último costo del lote; si no existe, se usa el costo actual del producto.";
    public bool MostrarAjusteLotes =>
        TipoControlInventario == "LOTE" ||
        TipoControlInventario == "LOTE_Y_SERIE" && AjusteEsEntrada;
    public bool MostrarAjusteSeriesNuevas => AjusteEsEntrada && AjusteRequiereSeries;
    public bool MostrarAjusteSeriesDisponibles => AjusteEsSalida && AjusteRequiereSeries;
    public decimal AjusteCantidadBase => AjusteCantidad *
        (PresentacionesOperacion.FirstOrDefault(x => x.Id == AjustePresentacionId)
            ?.FactorConversion ?? 1);
    public decimal AjusteLotesAsignado => AjusteLotes.Sum(x => x.CantidadBase);
    public decimal AjusteLotesPendiente => AjusteCantidadBase - AjusteLotesAsignado;
    public int AjusteSeriesSeleccionadas => AjusteSeriesDisponibles.Count(x => x.IsSelected);
    public int AjusteSeriesNuevasRegistradas => AjusteSeriesNuevas.Count(x =>
        !string.IsNullOrWhiteSpace(x.NumeroSerie));
    public decimal AjusteSeriesPendientes => AjusteCantidadBase -
        (AjusteEsEntrada
            ? AjusteSeriesNuevasRegistradas
            : AjusteSeriesSeleccionadas);
    public string AjusteResumenDistribucionSeries => string.Join(Environment.NewLine,
        AjusteSeriesDisponibles.Where(x => x.IsSelected)
            .GroupBy(x => x.NumeroLote ?? "SIN LOTE")
            .OrderBy(x => x.Key)
            .Select(x => $"{x.Key} → {x.Count()}"));
    public string AjusteOrigenCosto { get; private set; } =
        "Referencia: costo promedio actual del producto.";
    public bool PuedeAgregarLoteAjuste =>
        AjusteEsEntrada && AjusteCantidadBase > 0;
    public bool PuedeAgregarSerieNuevaAjuste => AjusteCantidadBase > 0 &&
        AjusteCantidadBase == decimal.Truncate(AjusteCantidadBase) &&
        AjusteSeriesNuevasRegistradas < AjusteCantidadBase &&
        (TipoControlInventario != "LOTE_Y_SERIE" ||
         AjusteLotes.Count > 0 && AjusteLotesPendiente == 0);
    public bool PuedeRegistrarAjuste =>
        AjusteEsEntrada && TipoControlInventario == "LOTE_Y_SERIE"
            ? ObtenerRazonesEntradaLoteYSerieNoValida().Count == 0
            : !IsLoading && AjusteBodegaId.HasValue &&
              AjustePresentacionId.HasValue && AjusteCantidad > 0 &&
              AjusteMotivoId.HasValue &&
              (AjusteEsSalida || AjusteCostoPresentacion >= 0) &&
              ValidarControlAjuste(AjusteCantidadBase) is null;
    public bool MostrarColumnaCaducidad =>
        MostrarConfiguracionCaducidad && AlertaCaducidad;

    public string TextoBotonGuardar =>
        IsEditing
            ? "Guardar cambios"
            : "Crear producto";

    partial void OnNombreChanged(
    string value)
    {
        CampoSugerenciaActivo =
            "NOMBRE";

        _ = BuscarSugerenciasConRetardoAsync();
    }

    partial void OnModeloChanged(
    string? value)
    {
        CampoSugerenciaActivo =
            "MODELO";

        _ = BuscarSugerenciasConRetardoAsync();
    }

    partial void OnMostrarSugerenciasChanged(
    bool value)
    {
        OnPropertyChanged(
            nameof(MostrarSugerenciasNombre));

        OnPropertyChanged(
            nameof(MostrarSugerenciasModelo));
    }

    partial void OnCampoSugerenciaActivoChanged(
    string value)
    {
        OnPropertyChanged(
            nameof(MostrarSugerenciasNombre));

        OnPropertyChanged(
            nameof(MostrarSugerenciasModelo));
    }

    private async Task BuscarSugerenciasConRetardoAsync()
    {
        _suggestionsCancellationTokenSource?.Cancel();
        _suggestionsCancellationTokenSource?.Dispose();

        _suggestionsCancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            _suggestionsCancellationTokenSource.Token;

        try
        {
            /*
             * Evitamos búsquedas demasiado tempranas.
             */
            var nombreValido =
                !string.IsNullOrWhiteSpace(Nombre) &&
                Nombre.Trim().Length >= 3;

            var modeloValido =
                !string.IsNullOrWhiteSpace(Modelo) &&
                Modelo.Trim().Length >= 3;

            if (!nombreValido &&
                !modeloValido)
            {
                LimpiarSugerencias();
                return;
            }


            await Task.Delay(
                TimeSpan.FromMilliseconds(450),
                cancellationToken);


            await BuscarSugerenciasAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // El usuario continuó escribiendo.
        }
    }

    [RelayCommand]
    private void AbrirProductoSugerido(
    ProductoSugerenciaDto? sugerencia)
    {
        if (sugerencia is null)
        {
            return;
        }

        LimpiarSugerencias();

        ExistingProductRequested?.Invoke(
            sugerencia.ProductoId);
    }
    private async Task BuscarSugerenciasAsync(
    CancellationToken cancellationToken)
    {
        var empresaId =
            ObtenerEmpresaId();

        if (empresaId <= 0)
        {
            LimpiarSugerencias();
            return;
        }


        IsSearchingSuggestions =
            true;

        try
        {
            var sugerencias =
                await _productService
                    .BuscarSimilaresAsync(
                        empresaId,
                        Nombre,
                        Modelo,
                        5,
                        cancellationToken);


            /*
             * Si estamos editando, no queremos sugerir
             * el mismo producto que se está editando.
             */
            if (_productoId.HasValue)
            {
                sugerencias =
                    sugerencias
                        .Where(x =>
                            x.ProductoId !=
                            _productoId.Value)
                        .ToList();
            }


            SugerenciasProductos.Clear();

            foreach (var sugerencia in sugerencias)
            {
                SugerenciasProductos.Add(
                    sugerencia);
            }


            MostrarSugerencias =
                SugerenciasProductos.Count > 0;

            OnPropertyChanged(
                nameof(HaySugerencias));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            LimpiarSugerencias();
        }
        finally
        {
            IsSearchingSuggestions =
                false;
        }
    }

    private void LimpiarSugerencias()
    {
        SugerenciasProductos.Clear();

        MostrarSugerencias =
            false;

        OnPropertyChanged(
            nameof(HaySugerencias));
    }

    public ProductFormViewModel(
        IProductService productService,
        IProductCatalogService catalogService,
        IInventoryService inventoryService,
        CurrentSession currentSession,
        IMessageDialogService messageDialogService)
    {
        _productService = productService;
        _catalogService = catalogService;
        _inventoryService = inventoryService;
        _currentSession = currentSession;
        _messageDialogService = messageDialogService;
        LotesInventarioInicial.CollectionChanged +=
            DistribucionInventarioCollectionChanged;
        SeriesInventarioInicial.CollectionChanged +=
            DistribucionInventarioCollectionChanged;
        InventariosIniciales.CollectionChanged +=
            DistribucionInventarioCollectionChanged;
    }

    private void DistribucionInventarioCollectionChanged(
        object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (var item in e.OldItems.OfType<
                         LoteInventarioInicialEditorViewModel>())
                item.PropertyChanged -= LoteInicialPropertyChanged;
        if (e.NewItems is not null)
            foreach (var item in e.NewItems.OfType<
                         LoteInventarioInicialEditorViewModel>())
                item.PropertyChanged += LoteInicialPropertyChanged;
        if (ReferenceEquals(sender, InventariosIniciales))
        {
            OnPropertyChanged(nameof(CostoBaseInventarioInicial));
            OnPropertyChanged(nameof(TieneEntradaInicialPendiente));
            OnPropertyChanged(nameof(MostrarCancelarEntradaInicial));
            ActualizarReferenciasPrecios();
            ReconstruirResumenesPrecios();
        }
        NotificarDistribucionInventario();
    }

    public async Task NuevoAsync(
    string? codigoBarras = null,
    bool sinCodigoBarras = false)
    {
        if (IsLoading)
        {
            return;
        }

        LimpiarFormulario();

        CodigoBarras =
            codigoBarras ?? string.Empty;

        SinCodigoBarras =
            sinCodigoBarras;

        TituloFormulario =
            "Nuevo producto";

        IsEditing =
            false;

        IsLoading =
            true;

        try
        {
            await CargarCatalogosAsync();
        }
        catch
        {
            MensajeError =
                "No fue posible cargar la información necesaria para registrar el producto.";
        }
        finally
        {
            IsLoading =
                false;
            InitialFocusRequested?.Invoke();
        }
    }

    public async Task EditarAsync(long productoId)
    {
        if (IsLoading)
        {
            return;
        }

        LimpiarFormulario();

        var empresaId = ObtenerEmpresaId();

        if (empresaId <= 0)
        {
            MensajeError =
                "No existe una empresa activa.";
            return;
        }

        IsLoading = true;

        try
        {
            await CargarCatalogosAsync();

            var producto =
                await _productService.ObtenerProductoAsync(
                    productoId,
                    empresaId);

            if (producto is null)
            {
                MensajeError =
                    "No fue posible encontrar el producto.";
                return;
            }

            _productoId = producto.Id;

            Nombre = producto.Nombre;
            CodigoInterno = producto.Codigo;
            Modelo = producto.Modelo;
            PresentacionNombre = producto.PresentacionNombre;
            CodigoBarras = producto.CodigoBarras ?? string.Empty;
            SinCodigoBarras = producto.CodigoBarrasInterno;
            Descripcion = producto.Descripcion;

            CategoriaProductoId =
                producto.CategoriaProductoId;

            MarcaId =
                producto.MarcaId;

            UnidadMedidaBaseId =
                producto.UnidadMedidaBaseId;
            ConversionUnidadBase = string.IsNullOrWhiteSpace(
                producto.UnidadMedidaBaseAbreviatura)
                ? "UND"
                : producto.UnidadMedidaBaseAbreviatura;

            TarifaImpuestoId =
                producto.Impuestos
                    .FirstOrDefault(x => x.Estado == 1)
                    ?.TarifaImpuestoId;
            _tarifasImpuestoIds.Clear();
            foreach (var impuesto in producto.Impuestos.Where(x => x.Estado == 1))
                _tarifasImpuestoIds.Add(impuesto.TarifaImpuestoId);

            var presentacionBase = producto.Presentaciones
                .FirstOrDefault(x => x.EsPresentacionBase);
            PresentacionesOperacion.Clear();
            foreach (var item in producto.Presentaciones.Where(x => x.Estado == 1))
                PresentacionesOperacion.Add(item);
            _preciosPorPresentacion.Clear();
            foreach (var item in producto.Presentaciones)
                _preciosPorPresentacion[item.Codigo] =
                    item.Precios.Select(CopiarPrecio).ToList();
            ConfigurarPreciosAhora =
                producto.Presentaciones.Any(
                    x => x.Precios.Any(p => p.Estado == 1));
            PresentacionPrecioCodigo = "BASE";
            CargarPrecios(presentacionBase?.Precios ?? []);

            foreach (var presentacion in producto.Presentaciones
                         .Where(x => !x.EsPresentacionBase))
            {
                PresentacionesAdicionales.Add(presentacion);
            }
            ActualizarPresentacionesInventarioInicial();

            TipoProducto =
                producto.TipoProducto;

            TipoControlInventario =
                producto.TipoControlInventario;

            ManejaInventario =
                producto.ManejaInventario;

            Costo = producto.Costo;
            ReconstruirResumenesPrecios();
            CombinarExistencias(producto.Existencias);
            foreach (var inventario in producto.InventariosIniciales)
                InventariosIniciales.Add(inventario);
            RegistrarInventarioInicial =
                producto.InventariosIniciales.Count > 0;
            PuedeCompletarInventarioInicial =
                producto.PuedeCompletarInventarioInicial;
            MotivoNoPuedeCompletarInventarioInicial =
                producto.MotivoNoPuedeCompletarInventarioInicial;
            AlertaCaducidad =
                producto.AlertaCaducidad;

            DiasAlertaCaducidad =
                producto.DiasAlertaCaducidad;

            Observacion =
                producto.Observacion;

            IsEditing = true;

            TituloFormulario =
                $"Editar {producto.Nombre}";

            NotificarPropiedadesCalculadas();
        }
        catch
        {
            MensajeError =
                "No fue posible cargar la información del producto.";
        }
        finally
        {
            IsLoading = false;

        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        MensajeError = null;

        var empresaId =
            ObtenerEmpresaId();

        if (empresaId <= 0)
        {
            MensajeError =
                "No existe una empresa activa.";
            return;
        }

        if (string.IsNullOrWhiteSpace(
            PresentacionNombre))
        {
            MensajeError =
                "Debe ingresar el nombre de la presentación base.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            MensajeError =
                "Debe ingresar el nombre del producto.";
            return;
        }

        if (!UnidadMedidaBaseId.HasValue)
        {
            MensajeError =
                "Debe seleccionar la unidad de medida.";
            return;
        }

        if (!TarifaImpuestoId.HasValue)
        {
            MensajeError =
                "Debe seleccionar el IVA / impuesto.";
            return;
        }

        IsLoading = true;

        try
        {
            var manejaInventarioReal =
                TipoProducto == "PRODUCTO" &&
                ManejaInventario;

            var manejaLotes =
                manejaInventarioReal &&
                TipoControlInventario
                    is "LOTE" or "LOTE_Y_SERIE";

            var manejaSeries =
                manejaInventarioReal &&
                TipoControlInventario
                    is "SERIE" or "LOTE_Y_SERIE";

            var manejaFechaCaducidad =
                manejaLotes &&
                AlertaCaducidad;

            var capturaInventarioActiva = IsEditing
                ? MostrarEntradaInicialExistente
                : RegistrarInventarioInicial;
            if (capturaInventarioActiva &&
                !InventariosIniciales.Any(x => !x.EsHistorico))
            {
                MensajeError =
                    "Agregue al menos una presentación y bodega al inventario inicial.";
                return;
            }
            if (capturaInventarioActiva)
            {
                var presentacionesSinPrecio =
                    PresentacionesInventarioInicial
                        .Where(x =>
                            !_preciosPorPresentacion.TryGetValue(
                                x.Codigo, out var precios) ||
                            precios.Count != ListasPrecio.Count)
                        .Select(x => x.Nombre)
                        .ToList();
                if (presentacionesSinPrecio.Count > 0)
                {
                    MensajeError =
                        "Agregue los precios de todas las presentaciones. " +
                        $"Pendientes: {string.Join(", ", presentacionesSinPrecio)}.";
                    return;
                }
            }

            var request =
                new ProductoGuardarRequest
                {
                    Id = _productoId,

                    CodigoInterno = CodigoInterno,

                    EmpresaId =
                        empresaId,

                    EstablecimientoId =
                        _currentSession.EstablecimientoId,

                    CategoriaProductoId =
                        CategoriaProductoId,

                    MarcaId =
                        MarcaId,

                    UnidadMedidaBaseId =
                        UnidadMedidaBaseId.Value,

                    TarifasImpuestoIds =
                        _tarifasImpuestoIds
                            .Append(TarifaImpuestoId ?? 0)
                            .Where(x => x > 0)
                            .Distinct()
                            .ToList(),

                    Nombre =
                        Nombre.Trim(),

                    Modelo = Modelo?.Trim(),

                    PresentacionNombre = PresentacionNombre.Trim(),

                    CodigoBarras = SinCodigoBarras
                        ? null
                        : CodigoBarras.Trim(),

                    SinCodigoBarras =
                        SinCodigoBarras,

                    Descripcion =
                        Descripcion?.Trim(),

                    TipoProducto =
                        TipoProducto,

                    TipoControlInventario =
                        manejaInventarioReal
                            ? TipoControlInventario
                            : "NORMAL",

                    ManejaInventario =
                        manejaInventarioReal,

                    ManejaLotes =
                        manejaLotes,

                    ManejaSeries =
                        manejaSeries,

                    ManejaFechaCaducidad =
                        manejaFechaCaducidad,

                    AlertaCaducidad =
                        manejaFechaCaducidad &&
                        AlertaCaducidad,

                    DiasAlertaCaducidad =
                        manejaFechaCaducidad
                            ? DiasAlertaCaducidad
                            : 0,

                    Observacion =
                        Observacion?.Trim(),

                    Presentaciones =
                    [
                        new ProductoPresentacionDto
                        {
                            Id = null,
                            Codigo = "BASE",
                            CodigoBarras = SinCodigoBarras
                                ? null
                                : CodigoBarras.Trim(),
                            Nombre = PresentacionNombre.Trim(),
                            FactorConversion = 1,
                            EsPresentacionBase = true,
                            PermiteCompra = true,
                            PermiteVenta = true,
                            Estado = 1,
                            Precios = ConfigurarPreciosAhora
                                ? ObtenerPrecios("BASE")
                                : []
                        },
                        .. PresentacionesAdicionales.Select(x =>
                        {
                            x.Precios = ConfigurarPreciosAhora
                                ? ObtenerPrecios(x.Codigo)
                                : x.Precios;
                            return x;
                        })
                    ],

                    Existencias = manejaInventarioReal
                        ? Existencias.Where(x =>
                            x.StockActual != 0 ||
                            x.StockReservado != 0 ||
                            x.StockMinimo != 0 ||
                            !string.IsNullOrWhiteSpace(x.Ubicacion)).ToList()
                        : [],

                    InventariosIniciales = capturaInventarioActiva
                        ? InventariosIniciales.Where(x => !x.EsHistorico)
                            .ToList()
                        : []
                };

            var resultado =
                await _productService
                    .GuardarProductoAsync(request);

            if (!resultado.Success)
            {
                MensajeError =
                    resultado.Message;
                return;
            }

            if (resultado.ProductoId.HasValue)
            {
                ProductSaved?.Invoke(
                    resultado.ProductoId.Value);
            }

            CloseRequested?.Invoke();
        }
        catch
        {
            MensajeError =
                "Ocurrió un error inesperado al guardar el producto.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CancelarAsync()
    {
        if (IsQuickCatalogOpen)
        {
            CerrarCatalogoRapido();
            return;
        }

        if (!IsEditing && TieneCambiosModoNuevo())
        {
            var confirmar = await _messageDialogService.ConfirmAsync(
                "Cancelar nuevo producto",
                "Hay información ingresada que todavía no se ha guardado. ¿Desea cancelar el registro?",
                "Cancelar registro",
                "Seguir editando",
                true);

            if (!confirmar)
                return;
        }

        CloseRequested?.Invoke();
    }

    private bool TieneCambiosModoNuevo() =>
        !string.IsNullOrWhiteSpace(Nombre) ||
        !string.IsNullOrWhiteSpace(Modelo) ||
        !string.IsNullOrWhiteSpace(Descripcion) ||
        !string.IsNullOrWhiteSpace(Observacion) ||
        !string.IsNullOrWhiteSpace(CodigoBarras) ||
        CategoriaProductoId.HasValue ||
        MarcaId.HasValue ||
        PresentacionesAdicionales.Count > 0 ||
        InventariosIniciales.Count > 0 ||
        RegistrarInventarioInicial;

    /*
     * ============================================================
     * CREACIÓN RÁPIDA DE CATEGORÍA / MARCA
     * ============================================================
     */

    [RelayCommand]
    private void NuevaCategoria()
    {
        _tipoCatalogoRapido = "CATEGORIA";

        QuickCatalogTitle =
            "Nueva categoría";

        QuickCatalogLabel =
            "Nombre de la categoría";

        QuickCatalogIcon = "ShapeOutline";

        QuickCatalogName =
            string.Empty;

        QuickCatalogError =
            null;

        IsQuickCatalogOpen =
            true;
    }

    [RelayCommand]
    private void NuevaMarca()
    {
        _tipoCatalogoRapido = "MARCA";

        QuickCatalogTitle =
            "Nueva marca";

        QuickCatalogLabel =
            "Nombre de la marca";

        QuickCatalogIcon = "TagOutline";

        QuickCatalogName =
            string.Empty;

        QuickCatalogError =
            null;

        IsQuickCatalogOpen =
            true;
    }

    [RelayCommand]
    private async Task GuardarCatalogoRapidoAsync()
    {
        QuickCatalogError = null;

        if (string.IsNullOrWhiteSpace(
            QuickCatalogName))
        {
            QuickCatalogError =
                $"Debe ingresar {QuickCatalogLabel.ToLower()}.";
            return;
        }

        if (_tipoCatalogoRapido is null)
        {
            QuickCatalogError =
                "No se pudo determinar el catálogo.";
            return;
        }

        IsQuickCatalogSaving = true;

        try
        {
            CatalogCreateResult resultado;

            if (_tipoCatalogoRapido == "CATEGORIA")
            {
                var empresaId =
                    ObtenerEmpresaId();

                resultado =
                    await _catalogService.CrearCategoriaAsync(
                        empresaId,
                        QuickCatalogName);
            }
            else
            {
                resultado =
                    await _catalogService.CrearMarcaAsync(
                        QuickCatalogName);
            }

            if (!resultado.Success ||
                resultado.Item is null)
            {
                QuickCatalogError =
                    resultado.Message;
                return;
            }

            if (_tipoCatalogoRapido == "CATEGORIA")
            {
                if (!Categorias.Any(
                    x => x.Id == resultado.Item.Id))
                {
                    Categorias.Add(resultado.Item);
                }

                OrdenarColeccion(Categorias);

                CategoriaProductoId =
                    resultado.Item.Id;
            }
            else
            {
                if (!Marcas.Any(
                    x => x.Id == resultado.Item.Id))
                {
                    Marcas.Add(resultado.Item);
                }

                OrdenarColeccion(Marcas);

                MarcaId =
                    resultado.Item.Id;
            }

            CerrarCatalogoRapido();
        }
        catch
        {
            QuickCatalogError =
                "No fue posible crear el registro.";
        }
        finally
        {
            IsQuickCatalogSaving = false;
        }
    }

    [RelayCommand]
    private void CancelarCatalogoRapido()
    {
        CerrarCatalogoRapido();
    }

    private void CerrarCatalogoRapido()
    {
        IsQuickCatalogOpen = false;
        QuickCatalogError = null;
        QuickCatalogName = string.Empty;
        _tipoCatalogoRapido = null;
    }

    private static void OrdenarColeccion(
        ObservableCollection<CatalogItemDto> collection)
    {
        var ordenados =
            collection
                .OrderBy(x => x.Nombre)
                .ToList();

        collection.Clear();

        foreach (var item in ordenados)
        {
            collection.Add(item);
        }
    }

    partial void OnTipoProductoChanged(string value)
    {
        RestablecerConfiguracionCaducidad();

        ManejaInventario =
            value == "PRODUCTO";

        if (!ManejaInventario)
        {
            TipoControlInventario =
                "NORMAL";
            RegistrarInventarioInicial = false;
        }

        NotificarPropiedadesCalculadas();
    }

    partial void OnTipoControlInventarioChanged(
        string value)
    {
        RestablecerConfiguracionCaducidad();
        NotificarPropiedadesCalculadas();
    }

    partial void OnAlertaCaducidadChanged(bool value)
    {
        DiasAlertaCaducidad = 30;
        if (!value)
        {
            foreach (var lote in LotesInventarioInicial)
            {
                lote.FechaElaboracion = null;
                lote.FechaCaducidad = null;
            }
        }
        OnPropertyChanged(nameof(MostrarColumnaCaducidad));
    }

    private void RestablecerConfiguracionCaducidad()
    {
        AlertaCaducidad = true;
        DiasAlertaCaducidad = 30;
    }

    partial void OnManejaInventarioChanged(
        bool value)
    {
        if (!value)
        {
            TipoControlInventario =
                "NORMAL";
        }

        NotificarPropiedadesCalculadas();
    }

    partial void OnIsEditingChanged(
    bool value)
    {
        OnPropertyChanged(
            nameof(TextoBotonGuardar));

        OnPropertyChanged(
            nameof(TextoAyudaCodigoBarras));
        OnPropertyChanged(nameof(PuedeConfigurarPrecios));
        OnPropertyChanged(nameof(MostrarCapturaInventarioInicial));
        ActualizarReferenciasPrecios();
    }

    private async Task CargarCatalogosAsync()
    {
        var empresaId =
            ObtenerEmpresaId();

        if (empresaId <= 0)
        {
            MensajeError =
                "No existe una empresa activa.";
            return;
        }

        var categorias =
            await _catalogService
                .ObtenerCategoriasAsync(
                    empresaId);

        var marcas =
            await _catalogService
                .ObtenerMarcasAsync();

        var unidades =
            await _catalogService
                .ObtenerUnidadesMedidaAsync();

        var tarifas =
            await _catalogService
                .ObtenerTarifasImpuestoAsync();

        var listas =
            await _catalogService
                .ObtenerListasPrecioAsync(empresaId);

        var bodegas =
            await _catalogService
                .ObtenerBodegasAsync(empresaId);

        ReemplazarColeccion(
            Categorias,
            categorias);

        ReemplazarColeccion(
            Marcas,
            marcas);

        ReemplazarColeccion(
            UnidadesMedida,
            unidades);

        ReemplazarColeccion(
            TarifasImpuesto,
            tarifas);
        ReemplazarColeccion(
            ListasPrecio,
            listas);
        ReemplazarColeccion(
            Existencias,
            bodegas);

        if (!_productoId.HasValue)
        {
            UnidadMedidaBaseId = unidades
                .FirstOrDefault(x => string.Equals(
                    x.Nombre.Trim(),
                    "UNIDAD",
                    StringComparison.OrdinalIgnoreCase))
                ?.Id;
            TarifaImpuestoId = tarifas
                .FirstOrDefault(x => string.Equals(
                    x.Nombre.Trim(),
                    "IVA 0 %",
                    StringComparison.OrdinalIgnoreCase))
                ?.Id;
            InventarioBodegaId = null;
            CargarPrecios([]);
            ActualizarPresentacionesInventarioInicial();
        }
    }

    private static void ReemplazarColeccion(
        ObservableCollection<CatalogItemDto> destino,
        IEnumerable<CatalogItemDto> origen)
    {
        destino.Clear();
        foreach (var item in origen)
        {
            destino.Add(item);
        }
    }

    private static void ReemplazarColeccion(
        ObservableCollection<ListaPrecioEditorDto> destino,
        IEnumerable<ListaPrecioEditorDto> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }

    private static void ReemplazarColeccion(
        ObservableCollection<ProductoExistenciaDto> destino,
        IEnumerable<ProductoExistenciaDto> origen)
    {
        destino.Clear();
        foreach (var item in origen)
            destino.Add(item);
    }

    private void CargarPrecios(IEnumerable<ProductoPrecioDto> existentes)
    {
        var porLista = existentes.ToDictionary(x => x.ListaPrecioId);
        foreach (var item in Precios)
            item.PropertyChanged -= PrecioEditorPropertyChanged;
        Precios.Clear();
        foreach (var lista in ListasPrecio)
        {
            porLista.TryGetValue(lista.Id, out var precio);
            var editor = new PrecioListaEditorViewModel(lista, precio);
            editor.PropertyChanged += PrecioEditorPropertyChanged;
            Precios.Add(editor);
        }
        ActualizarReferenciasPrecios();
    }

    private void PrecioEditorPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(PrecioListaEditorViewModel.MetodoCalculo)
            or nameof(PrecioListaEditorViewModel.Porcentaje)
            or nameof(PrecioListaEditorViewModel.Precio))
            ActualizarReferenciasPrecios();
    }

    private void ActualizarReferenciasPrecios()
    {
        var costoBase = RegistrarInventarioInicial
            ? CostoBaseInventarioInicial
            : Costo.CostoPromedio;
        var factor = PresentacionesInventarioInicial
            .FirstOrDefault(x => x.Codigo == PresentacionPrecioCodigo)
            ?.FactorConversion ?? 1;
        var costoEquivalente = costoBase * factor;
        var baseItem = Precios.FirstOrDefault(x => x.EsListaBase);
        baseItem?.ActualizarReferencias(costoEquivalente, 0);
        var basePrice = baseItem?.PrecioResultante ?? 0;
        foreach (var precio in Precios.Where(x => !x.EsListaBase))
            precio.ActualizarReferencias(costoEquivalente, basePrice);
        OnPropertyChanged(nameof(CostoPresentacionPrecio));
    }

    private void CombinarExistencias(IEnumerable<ProductoExistenciaDto> existentes)
    {
        var porBodega = existentes.ToDictionary(x => x.BodegaId);
        var combinadas = Existencias.Select(fila =>
        {
            if (!porBodega.TryGetValue(fila.BodegaId, out var guardada))
                return fila;

            return new ProductoExistenciaDto
            {
                BodegaId = fila.BodegaId,
                BodegaCodigo = fila.BodegaCodigo,
                BodegaNombre = fila.BodegaNombre,
                StockActual = guardada.StockActual,
                StockReservado = guardada.StockReservado,
                StockMinimo = guardada.StockMinimo,
                Ubicacion = guardada.Ubicacion
            };
        }).ToList();

        ReemplazarColeccion(Existencias, combinadas);
        OnPropertyChanged(nameof(CostoBaseInventarioInicial));
        ActualizarReferenciasPrecios();
        ReconstruirResumenesPrecios();
    }

    partial void OnSinCodigoBarrasChanged(bool value)
    {
        OnPropertyChanged(
            nameof(TextoAyudaCodigoBarras));
    }

    private void LimpiarFormulario()
    {
        _productoId = null;

        Nombre = string.Empty;
        CodigoInterno = null;
        Modelo = null;
        PresentacionNombre = "UNIDAD";
        CodigoBarras = string.Empty;
        SinCodigoBarras = false;
        Descripcion = null;

        CategoriaProductoId = null;
        MarcaId = null;
        UnidadMedidaBaseId = null;
        TarifaImpuestoId = null;
        _tarifasImpuestoIds.Clear();
        ListaPrecioBaseId = null;
        PrecioBase = 0;
        PresentacionesAdicionales.Clear();
        PresentacionesOperacion.Clear();
        Precios.Clear();
        PreciosPresentacionesConfigurados.Clear();
        Existencias.Clear();
        Costo = new ProductoCostoDto();
        ConfigurarPreciosAhora = false;
        RegistrarInventarioInicial = false;
        InventarioBodegaId = null;
        InventarioUbicacion = null;
        InventarioPresentacionCodigo = "BASE";
        CantidadPresentacionesInicial = 0;
        CostoPresentacionInicial = 0;
        NumeroLoteInicial = null;
        FechaElaboracionInicial = null;
        FechaCaducidadInicial = null;
        SeriesInicialesTexto = null;
        PresentacionesInventarioInicial.Clear();
        _preciosPorPresentacion.Clear();
        PresentacionPrecioCodigo = "BASE";
        LotesInventarioInicial.Clear();
        SeriesInventarioInicial.Clear();
        CantidadLoteInicial = 0;
        NumeroSerieInicial = null;
        LoteSerieInicial = null;
        InventariosIniciales.Clear();

        TipoProducto = "PRODUCTO";
        TipoControlInventario = "NORMAL";

        ManejaInventario = true;
        PermiteVentaSinStock = false;

        InventarioStockMinimo = 0;

        AlertaCaducidad = true;
        DiasAlertaCaducidad = 30;

        Observacion = null;
        MensajeError = null;

        IsEditing = false;

        CerrarCatalogoRapido();

        NotificarPropiedadesCalculadas();

        _suggestionsCancellationTokenSource?.Cancel();
        _suggestionsCancellationTokenSource?.Dispose();

        _suggestionsCancellationTokenSource =
            null;

        LimpiarSugerencias();
    }

    [RelayCommand]
    private void AgregarPresentacion()
    {
        var presentacion = new ProductoPresentacionDto
        {
            Codigo = $"P{PresentacionesAdicionales.Count + 1:00}",
            Nombre = string.Empty,
            FactorConversion = 1,
            PermiteCompra = true,
            PermiteVenta = true,
            Estado = 1
        };
        PresentacionesAdicionales.Add(presentacion);
        ActualizarPresentacionesInventarioInicial();
        PresentationAdded?.Invoke();
    }

    public void SincronizarPresentacionAdicional(
        ProductoPresentacionDto presentacion)
    {
        var indice = PresentacionesInventarioInicial.IndexOf(presentacion);
        if (indice >= 0)
            PresentacionesInventarioInicial[indice] = presentacion;

        for (var i = 0; i < InventariosIniciales.Count; i++)
        {
            var inventario = InventariosIniciales[i];
            if (!string.Equals(
                    inventario.PresentacionCodigo,
                    presentacion.Codigo,
                    StringComparison.OrdinalIgnoreCase))
                continue;
            inventario.PresentacionNombre = presentacion.Nombre;
            InventariosIniciales[i] = inventario;
        }

        if (_preciosPorPresentacion.ContainsKey(presentacion.Codigo))
            ActualizarResumenPrecioPresentacion(presentacion.Codigo);

        OnPropertyChanged(nameof(FactorInicial));
        OnPropertyChanged(nameof(CantidadBaseInicial));
        OnPropertyChanged(nameof(CostoUnitarioBaseInicial));
        OnPropertyChanged(nameof(ResumenInventarioInicial));
        ActualizarReferenciasPrecios();
    }

    [RelayCommand]
    private void QuitarPresentacion(ProductoPresentacionDto? presentacion)
    {
        if (presentacion is null)
            return;

        if (presentacion.Id.HasValue)
        {
            presentacion.Estado = 0;
            return;
        }

        PresentacionesAdicionales.Remove(presentacion);
        _preciosPorPresentacion.Remove(presentacion.Codigo);
        var resumenesPrecio = PreciosPresentacionesConfigurados
            .Where(x => string.Equals(
                x.PresentacionCodigo,
                presentacion.Codigo,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var resumenPrecio in resumenesPrecio)
            PreciosPresentacionesConfigurados.Remove(resumenPrecio);
        ActualizarPresentacionesInventarioInicial();
    }

    private void ActualizarPresentacionesInventarioInicial()
    {
        var codigoSeleccionado = PresentacionPrecioCodigo;
        PresentacionesInventarioInicial.Clear();
        PresentacionesInventarioInicial.Add(new ProductoPresentacionDto
        {
            Codigo = "BASE",
            Nombre = string.IsNullOrWhiteSpace(PresentacionNombre)
                ? "PRESENTACIÓN BASE"
                : PresentacionNombre,
            FactorConversion = 1,
            EsPresentacionBase = true
        });
        foreach (var item in PresentacionesAdicionales.Where(x => x.Estado == 1))
            PresentacionesInventarioInicial.Add(item);
        PresentacionPrecioCodigo =
            PresentacionesInventarioInicial.Any(
                x => x.Codigo == codigoSeleccionado)
                ? codigoSeleccionado
                : "BASE";
        OnPropertyChanged(nameof(FactorInicial));
        OnPropertyChanged(nameof(CantidadBaseInicial));
        OnPropertyChanged(nameof(CostoUnitarioBaseInicial));
    }

    [RelayCommand]
    private void AgregarPreciosPresentacion()
    {
        var presentacion = PresentacionesInventarioInicial
            .FirstOrDefault(x => string.Equals(
                x.Codigo,
                PresentacionPrecioCodigo,
                StringComparison.OrdinalIgnoreCase));
        if (presentacion is null)
        {
            MensajeError = "Seleccione la presentación que desea configurar.";
            return;
        }
        if (Precios.Count != ListasPrecio.Count || Precios.Count == 0)
        {
            MensajeError =
                "No fue posible cargar todas las listas de precio.";
            return;
        }
        if (Precios.Any(x =>
                x.UsaPorcentaje
                    ? !x.Porcentaje.HasValue || x.Porcentaje.Value < 0 ||
                      x.PrecioResultante <= 0
                    : !x.Precio.HasValue || x.Precio.Value <= 0))
        {
            MensajeError =
                "Complete un valor válido en cada lista. El precio resultante debe ser mayor a 0.";
            return;
        }
        var tienePrecioSinUtilidad =
            Precios.Any(x => x.Utilidad <= 0);

        _preciosPorPresentacion[PresentacionPrecioCodigo] =
            Precios.Select(x => x.ToDto()).ToList();
        ConfigurarPreciosAhora = true;
        ActualizarResumenPrecioPresentacion(PresentacionPrecioCodigo);
        MensajeError = tienePrecioSinUtilidad
            ? "Advertencia: una o más listas tienen utilidad igual o menor que cero."
            : null;

        var indiceActual = PresentacionesInventarioInicial
            .IndexOf(presentacion);
        if (indiceActual >= 0 &&
            indiceActual + 1 < PresentacionesInventarioInicial.Count)
        {
            PresentacionPrecioCodigo =
                PresentacionesInventarioInicial[indiceActual + 1].Codigo;
        }
        else
        {
            CargarPrecios([]);
        }
    }

    private List<ProductoPrecioDto> ObtenerPrecios(string codigo) =>
        _preciosPorPresentacion.TryGetValue(codigo, out var precios)
            ? precios.Select(CopiarPrecio).ToList()
            : [];

    private static ProductoPrecioDto CopiarPrecio(ProductoPrecioDto x) => new()
    {
        Id = x.Id,
        ListaPrecioId = x.ListaPrecioId,
        ListaPrecioNombre = x.ListaPrecioNombre,
        MetodoCalculo = x.MetodoCalculo,
        Porcentaje = x.Porcentaje,
        Precio = x.Precio,
        Estado = x.Estado
    };

    partial void OnPresentacionPrecioCodigoChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;
        CargarPrecios(ObtenerPrecios(value));
    }

    [RelayCommand]
    private void QuitarPreciosPresentacion(
        PrecioPresentacionConfiguradaViewModel? item)
    {
        if (item is null)
            return;

        _preciosPorPresentacion.Remove(item.PresentacionCodigo);
        var filas = PreciosPresentacionesConfigurados
            .Where(x => string.Equals(
                x.PresentacionCodigo,
                item.PresentacionCodigo,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var fila in filas)
            PreciosPresentacionesConfigurados.Remove(fila);
        ConfigurarPreciosAhora = _preciosPorPresentacion.Count > 0;
        PresentacionPrecioCodigo = item.PresentacionCodigo;
        CargarPrecios([]);
    }

    private void ReconstruirResumenesPrecios()
    {
        PreciosPresentacionesConfigurados.Clear();
        foreach (var codigo in _preciosPorPresentacion
                     .Where(x => x.Value.Count > 0)
                     .Select(x => x.Key))
            ActualizarResumenPrecioPresentacion(codigo);
    }

    private void ActualizarResumenPrecioPresentacion(string codigo)
    {
        var presentacion = PresentacionesInventarioInicial
            .FirstOrDefault(x => string.Equals(
                x.Codigo, codigo, StringComparison.OrdinalIgnoreCase));
        if (presentacion is null ||
            !_preciosPorPresentacion.TryGetValue(codigo, out var precios))
            return;

        var existente = PreciosPresentacionesConfigurados
            .Where(x => string.Equals(
                x.PresentacionCodigo,
                codigo,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        foreach (var item in existente)
            PreciosPresentacionesConfigurados.Remove(item);

        var costoBase = RegistrarInventarioInicial
            ? CostoBaseInventarioInicial
            : Costo.CostoPromedio;
        var costoEquivalente =
            costoBase * presentacion.FactorConversion;
        var precioListaBase = 0m;
        var detalles = new List<PrecioListaResumenViewModel>();

        foreach (var lista in ListasPrecio)
        {
            var precio = precios.FirstOrDefault(x =>
                x.ListaPrecioId == lista.Id);
            if (precio is null)
                continue;

            var precioResultante = precio.MetodoCalculo switch
            {
                "PORCENTAJE_COSTO" =>
                    ProductoNuevoRules.CalcularPrecioPorcentajeCosto(
                        costoEquivalente,
                        precio.Porcentaje ?? 0),
                "DESCUENTO_PORCENTAJE" =>
                    ProductoNuevoRules.CalcularPrecioConDescuento(
                        precioListaBase,
                        precio.Porcentaje ?? 0),
                _ => precio.Precio ?? 0
            };
            if (lista.EsListaBase)
                precioListaBase = precioResultante;

            var utilidad = precioResultante - costoEquivalente;
            detalles.Add(
                new PrecioListaResumenViewModel
                {
                    ListaNombre = lista.Nombre,
                    Precio = precioResultante,
                    Utilidad = utilidad,
                    Margen = costoEquivalente == 0
                        ? 0
                        : utilidad / costoEquivalente * 100m
                });
        }

        PreciosPresentacionesConfigurados.Add(
                new PrecioPresentacionConfiguradaViewModel
                {
                    PresentacionCodigo = codigo,
                    PresentacionNombre = presentacion.Nombre,
                    Detalles = detalles
                });
    }

    partial void OnRegistrarInventarioInicialChanged(bool value)
    {
        OnPropertyChanged(nameof(PuedeConfigurarPrecios));
        OnPropertyChanged(nameof(CostoBaseInventarioInicial));
        if (!value && !IsEditing)
            ConfigurarPreciosAhora = false;
        if (value)
        {
            ActualizarPresentacionesInventarioInicial();
        }
        ActualizarReferenciasPrecios();
    }

    partial void OnCantidadPresentacionesInicialChanged(decimal value)
    {
        OnPropertyChanged(nameof(CostoBaseInventarioInicial));
        ActualizarReferenciasPrecios();
        NotificarDistribucionInventario();
        OnPropertyChanged(nameof(ResumenInventarioInicial));
    }

    partial void OnCostoPresentacionInicialChanged(decimal value)
    {
        OnPropertyChanged(nameof(CostoBaseInventarioInicial));
        ActualizarReferenciasPrecios();
        OnPropertyChanged(nameof(ResumenInventarioInicial));
    }

    partial void OnInventarioPresentacionCodigoChanged(string value)
    {
        OnPropertyChanged(nameof(FactorInicial));
        OnPropertyChanged(nameof(CantidadBaseInicial));
        OnPropertyChanged(nameof(CostoTotalInicial));
        OnPropertyChanged(nameof(ResumenInventarioInicial));
        NotificarDistribucionInventario();
    }

    partial void OnInventarioBodegaIdChanged(long? value)
    {
        InventarioUbicacion = value.HasValue
            ? InventariosIniciales.LastOrDefault(x => x.BodegaId == value.Value)
                  ?.Ubicacion ??
              Existencias.FirstOrDefault(x => x.BodegaId == value.Value)
                  ?.Ubicacion
            : null;
        InventarioStockMinimo = value.HasValue
            ? InventariosIniciales.LastOrDefault(x => x.BodegaId == value.Value)
                  ?.StockMinimo ??
              Existencias.FirstOrDefault(x => x.BodegaId == value.Value)
                  ?.StockMinimo ?? 0
            : 0;
    }

    partial void OnPresentacionNombreChanged(string value)
    {
        for (var i = 0; i < InventariosIniciales.Count; i++)
        {
            var inventario = InventariosIniciales[i];
            if (!string.Equals(
                    inventario.PresentacionCodigo,
                    "BASE",
                    StringComparison.OrdinalIgnoreCase))
                continue;
            inventario.PresentacionNombre = value;
            InventariosIniciales[i] = inventario;
        }

        if (RegistrarInventarioInicial ||
            PresentacionesInventarioInicial.Count > 0)
        {
            var presentacionBase = new ProductoPresentacionDto
            {
                Codigo = "BASE",
                Nombre = string.IsNullOrWhiteSpace(value)
                    ? "UNIDAD"
                    : value,
                FactorConversion = 1,
                EsPresentacionBase = true
            };
            if (PresentacionesInventarioInicial.Count == 0)
                PresentacionesInventarioInicial.Add(presentacionBase);
            else
                PresentacionesInventarioInicial[0] = presentacionBase;
            OnPropertyChanged(nameof(ResumenInventarioInicial));
        }

        if (_preciosPorPresentacion.ContainsKey("BASE"))
            ActualizarResumenPrecioPresentacion("BASE");
    }

    [RelayCommand]
    private async Task VerKardexAsync()
    {
        CerrarCapturaInventarioInicialSiActiva();
        if (!_productoId.HasValue || ObtenerEmpresaId() <= 0)
            return;
        IsKardexOpen = true;
        IsKardexLoading = true;
        MensajeError = null;
        try
        {
            var items = await _inventoryService.ObtenerKardexAsync(
                new KardexFiltro
                {
                    EmpresaId = ObtenerEmpresaId(),
                    ProductoId = _productoId.Value,
                    EstablecimientoId = _currentSession.EstablecimientoId
                });
            KardexItems.Clear();
            foreach (var item in items)
                KardexItems.Add(item);
        }
        catch
        {
            MensajeError = "No fue posible consultar el Kardex del producto.";
            IsKardexOpen = false;
        }
        finally
        {
            IsKardexLoading = false;
        }
    }

    [RelayCommand]
    private void CerrarKardex() => IsKardexOpen = false;

    [RelayCommand]
    private async Task VerMovimientoInventarioInicialAsync(
        ProductoInventarioInicialRequest? item)
    {
        if (item?.MovimientoId is null)
            return;
        await VerKardexAsync();
    }

    [RelayCommand]
    private async Task AbrirAjusteAsync()
    {
        CerrarCapturaInventarioInicialSiActiva();
        LimpiarEditoresAjuste();
        _restaurandoContextoAjuste = true;
        AjusteTipo = "ENTRADA";
        AjusteBodegaId = _currentSession.BodegaId;
        AjustePresentacionId = PresentacionesOperacion.FirstOrDefault()?.Id;
        _restaurandoContextoAjuste = false;
        NotificarContextoTipoAjuste();
        AjusteCantidad = 0;
        AjusteCostoPresentacion = 0;
        AjusteMotivo = string.Empty;
        AjusteMotivoId = null;
        AjusteObservacion = null;
        AjusteNumeroLote = null;
        AjusteSeriesTexto = null;
        LimpiarEditoresAjuste();
        MensajeError = null;
        _estadoControlAjuste = _productoId.HasValue
            ? await _inventoryService.ObtenerEstadoControlAsync(
                ObtenerEmpresaId(), _productoId.Value)
            : null;
        AjusteManejaFechaCaducidad =
            _estadoControlAjuste?.ManejaFechaCaducidad == true;
        _seriesExistentesAjuste.Clear();
        if (_estadoControlAjuste is not null)
            foreach (var serie in _estadoControlAjuste.SeriesProducto)
                _seriesExistentesAjuste.Add(
                    serie.Trim().ToUpperInvariant());
        ActualizarOpcionesAjuste();
        await CargarMotivosAsync(
            MotivosAjuste,
            AjusteTipo == "ENTRADA" ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA");
        IsAjusteOpen = true;
    }

    [RelayCommand]
    private void CerrarAjuste() => IsAjusteOpen = false;

    [RelayCommand(CanExecute = nameof(PuedeRegistrarAjuste))]
    private async Task RegistrarAjusteAsync()
    {
        if (AjusteEsEntrada && !PuedeRegistrarAjuste)
        {
            MensajeError = ValidarControlAjuste(AjusteCantidadBase) ??
                "Complete bodega, presentación, cantidad, costo y motivo antes de registrar el ajuste.";
            return;
        }
        if (!_productoId.HasValue || !AjusteBodegaId.HasValue ||
            !AjustePresentacionId.HasValue || AjusteCantidad <= 0 ||
            string.IsNullOrWhiteSpace(AjusteMotivo) ||
            !_currentSession.EstablecimientoId.HasValue)
        {
            MensajeError = "Seleccione bodega, presentación, cantidad y escriba el motivo del ajuste.";
            return;
        }
        var presentacion = PresentacionesOperacion.First(x =>
            x.Id == AjustePresentacionId.Value);
        if (AjusteTipo == "ENTRADA" && AjusteCostoPresentacion < 0)
        {
            MensajeError = "El costo no puede ser negativo.";
            return;
        }
        var cantidadBase = AjusteCantidadBase;
        var errorControl = ValidarControlAjuste(cantidadBase);
        if (errorControl is not null)
        {
            MensajeError = errorControl;
            return;
        }
        var detalle = new IngresoInventarioDetalleRequest
        {
            ProductoId = _productoId.Value,
            ProductoPresentacionId = presentacion.Id!.Value,
            Cantidad = AjusteCantidad,
            CostoTotal = AjusteCostoPresentacion * AjusteCantidad,
            Observacion = AjusteObservacion,
            Series = ConstruirSeriesAjuste()
        };
        detalle.Lotes = ConstruirLotesAjuste();

        long? productoIdParaRecargar = null;
        IsLoading = true;
        try
        {
            var result = await _inventoryService.RegistrarAjusteAsync(
                new AjusteInventarioRequest
                {
                    EmpresaId = ObtenerEmpresaId(),
                    EstablecimientoId = _currentSession.EstablecimientoId.Value,
                    BodegaId = AjusteBodegaId.Value,
                    UsuarioId = _currentSession.UsuarioId,
                    TipoAjuste = AjusteTipo,
                    Fecha = DateTime.UtcNow,
                    MotivoOperacionInventarioId = AjusteMotivoId!.Value,
                    Motivo = AjusteMotivo,
                    Observacion = AjusteObservacion,
                    Detalles = [detalle]
                });
            MensajeError = result.Success ? null : result.Message;
            if (!result.Success)
                return;
            IsAjusteOpen = false;
            productoIdParaRecargar = _productoId.Value;
        }
        finally
        {
            IsLoading = false;
        }
        if (productoIdParaRecargar.HasValue)
            await EditarAsync(productoIdParaRecargar.Value);
    }

    partial void OnAjusteTipoChanged(string? oldValue, string newValue)
    {
        if (_restaurandoContextoAjuste) return;
        if (!ConfirmarPerdidaDatosAjuste("el tipo de ajuste"))
        {
            _restaurandoContextoAjuste = true;
            AjusteTipo = oldValue ?? "ENTRADA";
            _restaurandoContextoAjuste = false;
            NotificarContextoTipoAjuste();
            return;
        }
        NotificarContextoTipoAjuste();
        AjusteMotivoId = null;
        AjusteMotivo = string.Empty;
        AjusteNumeroLote = null;
        AjusteSeriesTexto = null;
        LimpiarEditoresAjuste();
        ActualizarOpcionesAjuste();
        _ = CargarMotivosAsync(MotivosAjuste,
            newValue == "ENTRADA" ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA");
    }

    private void NotificarContextoTipoAjuste()
    {
        OnPropertyChanged(nameof(AjusteEsEntrada));
        OnPropertyChanged(nameof(AjusteEsSalida));
        OnPropertyChanged(nameof(AjusteEtiquetaCosto));
        OnPropertyChanged(nameof(AjusteAsteriscoCosto));
        OnPropertyChanged(nameof(MostrarAjusteLotes));
        OnPropertyChanged(nameof(MostrarAjusteSeriesNuevas));
        OnPropertyChanged(nameof(MostrarAjusteSeriesDisponibles));
        NotificarEstadoBotonesAjuste();
    }

    partial void OnAjusteBodegaIdChanged(long? oldValue, long? newValue)
    {
        if (_restaurandoContextoAjuste) return;
        if (!ConfirmarPerdidaDatosAjuste("la bodega"))
        {
            _restaurandoContextoAjuste = true;
            AjusteBodegaId = oldValue;
            _restaurandoContextoAjuste = false;
            return;
        }
        AjusteNumeroLote = null;
        LimpiarEditoresAjuste();
        ActualizarOpcionesAjuste();
        NotificarEstadoBotonesAjuste();
    }

    partial void OnAjustePresentacionIdChanged(long? oldValue, long? newValue)
    {
        if (_restaurandoContextoAjuste) return;
        if (!ConfirmarPerdidaDatosAjuste("la presentación"))
        {
            _restaurandoContextoAjuste = true;
            AjustePresentacionId = oldValue;
            _restaurandoContextoAjuste = false;
            return;
        }
        LimpiarEditoresAjuste();
        ActualizarOpcionesAjuste();
        NotificarResumenAjuste();
        ActualizarCostoReferenciaAjuste();
    }

    partial void OnAjusteCantidadChanged(decimal value) =>
        NotificarResumenAjuste();

    partial void OnAjusteCostoPresentacionChanged(decimal value) =>
        NotificarEstadoBotonesAjuste();

    partial void OnAjusteMotivoChanged(string value) =>
        NotificarEstadoBotonesAjuste();

    partial void OnAjusteMotivoIdChanged(long? value)
    {
        AjusteMotivo = MotivosAjuste.FirstOrDefault(x => x.Id == value)?.Nombre
            ?? string.Empty;
        NotificarEstadoBotonesAjuste();
    }

    partial void OnConversionMotivoIdChanged(long? value)
    {
        ConversionMotivo = MotivosConversion.FirstOrDefault(x => x.Id == value)
            ?.Nombre ?? string.Empty;
        NotificarEstadoConversion();
    }

    partial void OnCorreccionMotivoIdChanged(long? value) =>
        CorreccionMotivo = MotivosCorreccion.FirstOrDefault(x => x.Id == value)
            ?.Nombre ?? string.Empty;

    partial void OnAjusteManejaFechaCaducidadChanged(bool value) =>
        NotificarEstadoBotonesAjuste();

    partial void OnAjusteNumeroLoteChanged(string? value)
    {
        FiltrarSeriesAjuste();
        ActualizarCostoReferenciaAjuste();
    }

    private void ActualizarOpcionesAjuste()
    {
        AjusteLotesDisponibles.Clear();
        var bodega = _estadoControlAjuste?.Bodegas
            .FirstOrDefault(x => x.BodegaId == AjusteBodegaId);
        if (AjusteEsEntrada && _estadoControlAjuste is not null)
        {
            foreach (var grupo in _estadoControlAjuste.Bodegas
                         .SelectMany(x => x.Lotes.Select(l => new
                         {
                             x.BodegaId,
                             Lote = l
                         }))
                         .GroupBy(x => x.Lote.LoteId)
                         .OrderBy(x => x.First().Lote.NumeroLote))
            {
                var referencia = grupo.First().Lote;
                var existenciaLocal = grupo.FirstOrDefault(x =>
                    x.BodegaId == AjusteBodegaId)?.Lote;
                AjusteLotesDisponibles.Add(new EstadoControlLoteDto
                {
                    LoteId = referencia.LoteId,
                    NumeroLote = referencia.NumeroLote,
                    StockActual = existenciaLocal?.StockActual ?? 0,
                    StockReservado = existenciaLocal?.StockReservado ?? 0,
                    FechaElaboracion = referencia.FechaElaboracion,
                    FechaCaducidad = referencia.FechaCaducidad,
                    UltimoCostoUnitarioBase = referencia.UltimoCostoUnitarioBase
                });
            }
        }
        else if (bodega is not null)
            foreach (var lote in bodega.Lotes.Where(x => x.Disponible > 0)
                         .OrderBy(x => x.NumeroLote))
                AjusteLotesDisponibles.Add(lote);
        CargarLotesSalidaDisponibles();
        FiltrarSeriesAjuste();
        ActualizarCostoReferenciaAjuste();
    }

    private void CargarLotesSalidaDisponibles()
    {
        if (!AjusteEsSalida || TipoControlInventario != "LOTE")
            return;

        foreach (var lote in AjusteLotes)
            lote.PropertyChanged -= AjusteLotePropertyChanged;
        AjusteLotes.Clear();
        foreach (var disponible in AjusteLotesDisponibles)
        {
            var lote = new AjusteLoteEditorViewModel();
            AplicarLoteExistente(lote, disponible);
            lote.CantidadBase = 0;
            lote.PropertyChanged += AjusteLotePropertyChanged;
            AjusteLotes.Add(lote);
        }
        NotificarResumenAjuste();
    }

    private void FiltrarSeriesAjuste()
    {
        AjusteSeriesDisponibles.Clear();
        var bodega = _estadoControlAjuste?.Bodegas
            .FirstOrDefault(x => x.BodegaId == AjusteBodegaId);
        if (bodega is null || !AjusteEsSalida) return;
        foreach (var serie in bodega.Series.OrderBy(x => x.NumeroSerie))
        {
            var editor = new AjusteSerieDisponibleViewModel
            {
                SerieId = serie.SerieId,
                NumeroSerie = serie.NumeroSerie,
                NumeroLote = serie.NumeroLote,
                Ubicacion = serie.Ubicacion
            };
            editor.PropertyChanged += AjusteSerieDisponiblePropertyChanged;
            AjusteSeriesDisponibles.Add(editor);
        }
    }

    private void ActualizarCostoReferenciaAjuste()
    {
        var factor = PresentacionesOperacion
            .FirstOrDefault(x => x.Id == AjustePresentacionId)
            ?.FactorConversion ?? 1;
        var lote = AjusteLotes.FirstOrDefault(x => x.LoteId.HasValue);
        var costoLote = lote is null ? null : AjusteLotesDisponibles
            .FirstOrDefault(x => x.LoteId == lote.LoteId)
            ?.UltimoCostoUnitarioBase;
        var costoBase = AjusteEsSalida
            ? Costo.CostoPromedio
            : costoLote ?? (Costo.UltimoCostoEfectivo > 0
                ? Costo.UltimoCostoEfectivo
                : Costo.CostoPromedio);
        AjusteCostoPresentacion = costoBase * factor;
        AjusteOrigenCosto = AjusteEsSalida
            ? "Costo aplicado: costo promedio vigente del producto."
            : costoLote.HasValue
                ? $"Referencia: último costo registrado para el lote {lote!.NumeroLote}."
                : Costo.UltimoCostoEfectivo > 0
                    ? "Referencia: último costo efectivo del producto."
                    : "Referencia: costo promedio actual del producto.";
        OnPropertyChanged(nameof(AjusteOrigenCosto));
    }

    [RelayCommand]
    private void AgregarLoteAjuste()
    {
        if (!PuedeAgregarLoteAjuste) return;
        var lote = new AjusteLoteEditorViewModel();
        foreach (var sugerencia in AjusteLotesDisponibles)
            lote.Sugerencias.Add(sugerencia);
        lote.PropertyChanged += AjusteLotePropertyChanged;
        AjusteLotes.Add(lote);
        NotificarResumenAjuste();
    }

    [RelayCommand]
    private void QuitarLoteAjuste(AjusteLoteEditorViewModel? lote)
    {
        if (lote is null) return;
        lote.PropertyChanged -= AjusteLotePropertyChanged;
        AjusteLotes.Remove(lote);
        SincronizarLotesSeriesNuevas();
        NotificarResumenAjuste();
    }

    [RelayCommand]
    private void UsarLoteSimilar(AjusteLoteEditorViewModel? lote)
    {
        if (lote?.LoteSimilar is null) return;
        AplicarLoteExistente(lote, lote.LoteSimilar);
    }

    [RelayCommand]
    private async Task CrearLoteSimilarAsync(AjusteLoteEditorViewModel? lote)
    {
        if (lote?.LoteSimilar is null) return;
        var mensaje = $"Existe el lote '{lote.LoteSimilar.NumeroLote}', posiblemente equivalente a '{lote.NumeroLote}'. ¿Confirma crear uno distinto usando el motivo general del ajuste?";
        var confirmar = await _messageDialogService.ConfirmWarningAsync(
            "Confirmar lote diferente",
            mensaje,
            "Crear lote diferente",
            "Volver al ajuste");
        if (!confirmar) return;
        lote.PermitirCrearLoteSimilar = true;
        lote.LoteSimilar = null;
    }

    [RelayCommand]
    private void AgregarSerieNuevaAjuste()
    {
        if (!PuedeAgregarSerieNuevaAjuste) return;
        var serie = new AjusteSerieNuevaEditorViewModel();
        serie.PropertyChanged += AjusteSerieNuevaPropertyChanged;
        AjusteSeriesNuevas.Add(serie);
        NotificarResumenAjuste();
    }

    [RelayCommand]
    private void QuitarSerieNuevaAjuste(AjusteSerieNuevaEditorViewModel? serie)
    {
        if (serie is null) return;
        serie.PropertyChanged -= AjusteSerieNuevaPropertyChanged;
        AjusteSeriesNuevas.Remove(serie);
        NotificarResumenAjuste();
    }

    private void AjusteLotePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not AjusteLoteEditorViewModel lote) return;
        if (e.PropertyName == nameof(
                AjusteLoteEditorViewModel.LoteSugeridoSeleccionado) &&
            lote.LoteSugeridoSeleccionado is { } loteSeleccionado)
        {
            AplicarLoteExistente(lote, loteSeleccionado);
            lote.MostrarSugerencias = false;
            lote.LoteSugeridoSeleccionado = null;
            ActualizarCostoReferenciaAjuste();
        }
        else if (e.PropertyName == nameof(AjusteLoteEditorViewModel.NumeroLote))
        {
            ResolverLoteEscrito(lote);
            ActualizarCostoReferenciaAjuste();
        }
        SincronizarLotesSeriesNuevas();
        NotificarResumenAjuste();
    }

    private void AjusteSerieNuevaPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotificarResumenAjuste();

    private void AjusteSerieDisponiblePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AjusteSerieDisponibleViewModel.IsSelected))
            NotificarResumenAjuste();
    }

    private void ResolverLoteEscrito(AjusteLoteEditorViewModel lote)
    {
        var numero = lote.NumeroLote.Trim();
        lote.Sugerencias.Clear();
        foreach (var sugerencia in OrdenarSugerenciasLote(numero))
            lote.Sugerencias.Add(sugerencia);
        lote.MostrarSugerencias = AjusteEsEntrada && numero.Length > 0 &&
                                  lote.Sugerencias.Count > 0;
        lote.PermitirCrearLoteSimilar = false;
        if (numero.Length == 0)
        {
            lote.LoteId = null;
            lote.LoteSimilar = null;
            return;
        }
        var repetido = AjusteLotes.FirstOrDefault(x =>
            !ReferenceEquals(x, lote) &&
            AjusteInventarioRules.NormalizarLoteExacto(x.NumeroLote) ==
            AjusteInventarioRules.NormalizarLoteExacto(numero));
        if (repetido is not null)
        {
            lote.LoteId = null;
            lote.LoteSimilar = null;
            MensajeError =
                $"El lote {numero.ToUpperInvariant()} ya está incluido en este ajuste. Modifique la cantidad de la fila existente.";
            return;
        }
        if (MensajeError?.Contains("ya está incluido en este ajuste",
                StringComparison.Ordinal) == true)
            MensajeError = null;
        var exacto = AjusteLotesDisponibles.FirstOrDefault(x =>
            string.Equals(x.NumeroLote.Trim(), numero,
                StringComparison.OrdinalIgnoreCase));
        if (exacto is not null)
        {
            AplicarLoteExistente(lote, exacto);
            return;
        }
        lote.LoteId = null;
        lote.StockActual = 0;
        lote.FechaElaboracion = null;
        lote.FechaCaducidad = null;
        var clave = NormalizarLoteComparable(numero);
        lote.LoteSimilar = AjusteLotesDisponibles.FirstOrDefault(x =>
            NormalizarLoteComparable(x.NumeroLote) == clave);
    }

    private IEnumerable<EstadoControlLoteDto> OrdenarSugerenciasLote(
        string consulta)
    {
        if (string.IsNullOrWhiteSpace(consulta))
            return AjusteLotesDisponibles.OrderBy(x => x.NumeroLote);
        return AjusteLotesDisponibles
            .Where(x => AjusteInventarioRules.CoincideBusquedaLote(
                x.NumeroLote, consulta))
            .OrderBy(x => AjusteInventarioRules.PrioridadBusquedaLote(
                x.NumeroLote, consulta))
            .ThenBy(x => x.NumeroLote.Length)
            .ThenBy(x => x.NumeroLote);
    }

    private static string NormalizarLoteComparable(string value) =>
        AjusteInventarioRules.NormalizarLoteComparable(value);

    private static void AplicarLoteExistente(
        AjusteLoteEditorViewModel editor, EstadoControlLoteDto lote)
    {
        editor.LoteId = lote.LoteId;
        editor.NumeroLote = lote.NumeroLote;
        editor.StockActual = lote.Disponible;
        editor.FechaElaboracion = lote.FechaElaboracion;
        editor.FechaCaducidad = lote.FechaCaducidad;
        editor.LoteSimilar = null;
        editor.PermitirCrearLoteSimilar = false;
    }

    private void LimpiarEditoresAjuste()
    {
        foreach (var lote in AjusteLotes)
            lote.PropertyChanged -= AjusteLotePropertyChanged;
        foreach (var serie in AjusteSeriesNuevas)
            serie.PropertyChanged -= AjusteSerieNuevaPropertyChanged;
        foreach (var serie in AjusteSeriesDisponibles)
            serie.PropertyChanged -= AjusteSerieDisponiblePropertyChanged;
        AjusteLotes.Clear();
        AjusteSeriesNuevas.Clear();
        AjusteSeriesDisponibles.Clear();
    }

    private void SincronizarLotesSeriesNuevas()
    {
        var filas = AjusteLotes.Select(x => x.FilaId).ToHashSet();
        foreach (var serie in AjusteSeriesNuevas.Where(x =>
                     x.LoteFilaId.HasValue &&
                     !filas.Contains(x.LoteFilaId.Value)))
            serie.LoteFilaId = null;
        OnPropertyChanged(nameof(AjusteLotes));
    }

    private void NotificarResumenAjuste()
    {
        OnPropertyChanged(nameof(AjusteCantidadBase));
        OnPropertyChanged(nameof(AjusteLotesAsignado));
        OnPropertyChanged(nameof(AjusteLotesPendiente));
        OnPropertyChanged(nameof(AjusteSeriesSeleccionadas));
        OnPropertyChanged(nameof(AjusteSeriesNuevasRegistradas));
        OnPropertyChanged(nameof(AjusteSeriesPendientes));
        OnPropertyChanged(nameof(AjusteResumenDistribucionSeries));
        NotificarEstadoBotonesAjuste();
    }

    private void NotificarEstadoBotonesAjuste()
    {
        OnPropertyChanged(nameof(PuedeAgregarLoteAjuste));
        OnPropertyChanged(nameof(PuedeAgregarSerieNuevaAjuste));
        OnPropertyChanged(nameof(PuedeRegistrarAjuste));
        RegistrarAjusteCommand.NotifyCanExecuteChanged();
    }

    private bool ConfirmarPerdidaDatosAjuste(string cambio)
    {
        if (!TieneDatosTemporalesAjuste()) return true;
        return _messageDialogService.Confirm(
            "Cambiar datos del ajuste",
            $"Al cambiar {cambio} se limpiarán los lotes y series ingresados. ¿Desea continuar?",
            "Cambiar y limpiar",
            "Conservar datos");
    }

    private bool TieneDatosTemporalesAjuste() =>
        AjusteLotes.Count > 0 || AjusteSeriesNuevas.Count > 0 ||
        AjusteSeriesDisponibles.Any(x => x.IsSelected);

    private string? ValidarControlAjuste(decimal cantidadBase)
    {
        if (AjusteEsEntrada && TipoControlInventario == "LOTE_Y_SERIE")
            return ObtenerRazonesEntradaLoteYSerieNoValida().FirstOrDefault();

        if (MostrarAjusteLotes)
        {
            var errorLotes = AjusteInventarioRules.ValidarLotes(
                cantidadBase,
                AjusteLotes.Select(x => new LoteAjusteSnapshot(
                    x.NumeroLote, x.CantidadBase, x.StockActual,
                    x.LoteId.HasValue)).ToList(),
                AjusteEsSalida);
            if (errorLotes is not null) return errorLotes;
            if (AjusteLotes.Any(x => x.LoteSimilar is not null &&
                                     !x.PermitirCrearLoteSimilar))
                return "Resuelva la advertencia de lote posiblemente equivalente antes de continuar.";
            if (AjusteEsEntrada && AjusteLotes.Any(x => !x.LoteId.HasValue &&
                    AjusteManejaFechaCaducidad && !x.FechaCaducidad.HasValue))
                return "Ingrese la caducidad de cada lote nuevo.";
            if (AjusteLotes.Any(x => !x.LoteId.HasValue &&
                    x.FechaElaboracion.HasValue && x.FechaCaducidad.HasValue &&
                    x.FechaElaboracion.Value.Date > x.FechaCaducidad.Value.Date))
                return "La elaboración no puede ser posterior a la caducidad.";
        }

        if (AjusteRequiereSeries)
        {
            var seriesRegla = AjusteEsEntrada
                ? AjusteSeriesNuevas.Select(x => new SerieAjusteSnapshot(
                    x.NumeroSerie, ObtenerNumeroLoteSerieAjuste(x))).ToList()
                : AjusteSeriesDisponibles.Where(x => x.IsSelected)
                    .Select(x => new SerieAjusteSnapshot(
                        x.NumeroSerie, x.NumeroLote)).ToList();
            var errorSeries = AjusteEsEntrada
                ? AjusteInventarioRules.ValidarSeriesNuevas(
                    cantidadBase, seriesRegla, _seriesExistentesAjuste)
                : AjusteInventarioRules.ValidarSeries(
                    cantidadBase, seriesRegla);
            if (errorSeries is not null) return errorSeries;
            if (AjusteEsEntrada)
            {
                var validas = AjusteSeriesNuevas.Where(x =>
                    !string.IsNullOrWhiteSpace(x.NumeroSerie)).ToList();
                if (TipoControlInventario == "LOTE_Y_SERIE")
                {
                    if (validas.Any(x => !x.LoteFilaId.HasValue ||
                                         AjusteLotes.All(lote =>
                                             lote.FilaId != x.LoteFilaId.Value)))
                        return "Asocie cada serie nueva a uno de los lotes del ajuste.";
                    foreach (var lote in AjusteLotes)
                        if (validas.Count(x => x.LoteFilaId == lote.FilaId) !=
                            lote.CantidadBase)
                            return $"Las series asociadas al lote '{lote.NumeroLote}' deben coincidir con su cantidad.";
                }
            }
        }
        return null;
    }

    private List<IngresoInventarioLoteRequest> ConstruirLotesAjuste()
    {
        if (TipoControlInventario == "LOTE_Y_SERIE" && AjusteEsSalida)
            return AjusteSeriesDisponibles.Where(x => x.IsSelected)
                .GroupBy(x => x.NumeroLote ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .Select(x => new IngresoInventarioLoteRequest
                {
                    NumeroLote = x.Key,
                    CantidadBase = x.Count()
                }).ToList();
        if (!MostrarAjusteLotes) return [];
        var lotes = AjusteEsSalida
            ? AjusteLotes.Where(x => x.CantidadBase > 0)
            : AjusteLotes;
        return lotes.Select(x => new IngresoInventarioLoteRequest
        {
            NumeroLote = x.NumeroLote.Trim(),
            CantidadBase = x.CantidadBase,
            FechaElaboracion = x.FechaElaboracion.HasValue
                ? DateOnly.FromDateTime(x.FechaElaboracion.Value) : null,
            FechaCaducidad = x.FechaCaducidad.HasValue
                ? DateOnly.FromDateTime(x.FechaCaducidad.Value) : null,
            PermitirCrearLoteSimilar = x.PermitirCrearLoteSimilar
        }).ToList();
    }

    private List<IngresoInventarioSerieRequest> ConstruirSeriesAjuste() =>
        AjusteEsEntrada
            ? AjusteSeriesNuevas.Where(x => !string.IsNullOrWhiteSpace(x.NumeroSerie))
                .Select(x => new IngresoInventarioSerieRequest
                {
                    NumeroSerie = x.NumeroSerie.Trim(),
                    NumeroLote = ObtenerNumeroLoteSerieAjuste(x)?.Trim()
                }).ToList()
            : AjusteSeriesDisponibles.Where(x => x.IsSelected)
                .Select(x => new IngresoInventarioSerieRequest
                {
                    NumeroSerie = x.NumeroSerie,
                    NumeroLote = x.NumeroLote
                }).ToList();

    private string? ObtenerNumeroLoteSerieAjuste(
        AjusteSerieNuevaEditorViewModel serie) =>
        serie.LoteFilaId.HasValue
            ? AjusteLotes.FirstOrDefault(lote =>
                lote.FilaId == serie.LoteFilaId.Value)?.NumeroLote
            : null;

    private IReadOnlyList<string> ObtenerRazonesEntradaLoteYSerieNoValida()
    {
        var factor = PresentacionesOperacion.FirstOrDefault(x =>
            x.Id == AjustePresentacionId)?.FactorConversion ?? 0;
        return AjusteInventarioRules.ObtenerRazonesEntradaLoteYSerieNoValida(
            new EntradaLoteYSerieAjusteSnapshot(
                IsLoading,
                AjusteBodegaId,
                AjustePresentacionId,
                AjusteCantidad,
                factor,
                AjusteCostoPresentacion,
                AjusteMotivo?.Trim() ?? string.Empty,
                AjusteManejaFechaCaducidad,
                AjusteLotes.Select(x => new LoteEntradaAjusteSnapshot(
                    x.FilaId,
                    x.LoteId,
                    x.NumeroLote,
                    x.CantidadBase,
                    x.FechaElaboracion,
                    x.FechaCaducidad,
                    x.LoteSimilar is not null &&
                    !x.PermitirCrearLoteSimilar)).ToList(),
                AjusteSeriesNuevas.Select(x =>
                    new SerieEntradaAjusteSnapshot(
                        x.NumeroSerie, x.LoteFilaId)).ToList(),
                _seriesExistentesAjuste));
    }

    [RelayCommand]
    private async Task AbrirConversionAsync()
    {
        CerrarCapturaInventarioInicialSiActiva();
        if (!_productoId.HasValue)
            return;
        MensajeError = null;
        IsLoading = true;
        try
        {
            var estado = await _inventoryService.ObtenerEstadoControlAsync(
                ObtenerEmpresaId(), _productoId.Value);
            if (estado is null)
            {
                MensajeError = "No fue posible cargar el estado de control.";
                return;
            }
            ConversionTipoAnterior = estado.TipoControl;
            TiposControlDestino.Clear();
            foreach (var tipo in ObtenerControlesDestino(estado.TipoControl))
                TiposControlDestino.Add(tipo);
            ConversionTipoNuevo = TiposControlDestino.FirstOrDefault();
            ConversionControlCaducidad =
                ConversionDestinoLotes &&
                (estado.TipoControl is "LOTE" or "LOTE_Y_SERIE"
                    ? AlertaCaducidad
                    : true);
            ConversionDiasAnticipacionCaducidad =
                ConversionControlCaducidad && DiasAlertaCaducidad > 0
                    ? DiasAlertaCaducidad
                    : 30;
            ConversionMotivo = string.Empty;
            ConversionMotivoId = null;
            ConversionBodegas.Clear();
            foreach (var bodega in estado.Bodegas)
            {
                var editor = new ConversionBodegaEditorViewModel
                {
                    BodegaId = bodega.BodegaId,
                    BodegaCodigo = bodega.BodegaCodigo,
                    BodegaNombre = bodega.BodegaNombre,
                    StockActual = bodega.StockActual,
                    StockReservado = bodega.StockReservado
                };
                editor.DistribucionChanged += OnConversionDistribucionChanged;
                foreach (var lote in bodega.Lotes)
                    editor.Lotes.Add(new ConversionLoteEditorViewModel
                    {
                        ProductoLoteId = lote.LoteId,
                        NumeroLote = lote.NumeroLote,
                        CantidadBase = lote.StockActual,
                        FechaElaboracion = lote.FechaElaboracion,
                        FechaCaducidad = lote.FechaCaducidad
                    });
                foreach (var serie in bodega.Series)
                    editor.Series.Add(new ConversionSerieEditorViewModel
                    {
                        ProductoSerieId = serie.SerieId,
                        NumeroSerie = serie.NumeroSerie,
                        NumeroLote = serie.NumeroLote
                    });
                ConversionBodegas.Add(editor);
            }
            await CargarMotivosAsync(MotivosConversion, "CONVERSION_CONTROL");
            NotificarEstadoConversion();
            IsConversionOpen = true;
        }
        catch (Exception ex)
        {
            IsConversionOpen = false;
            MensajeError =
                $"No fue posible abrir la conversión de control: {ex.GetBaseException().Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CerrarConversion() => IsConversionOpen = false;

    [RelayCommand]
    private void AgregarLoteConversion(ConversionBodegaEditorViewModel? bodega) =>
        bodega?.Lotes.Add(new ConversionLoteEditorViewModel());

    [RelayCommand]
    private void QuitarLoteConversion(ConversionLoteEditorViewModel? lote)
    {
        if (lote is null) return;
        foreach (var bodega in ConversionBodegas)
            if (bodega.Lotes.Remove(lote)) return;
    }

    [RelayCommand]
    private void AgregarSerieConversion(ConversionBodegaEditorViewModel? bodega) =>
        bodega?.Series.Add(new ConversionSerieEditorViewModel());

    [RelayCommand]
    private void QuitarSerieConversion(ConversionSerieEditorViewModel? serie)
    {
        if (serie is null) return;
        foreach (var bodega in ConversionBodegas)
            if (bodega.Series.Remove(serie)) return;
    }

    private void OnConversionDistribucionChanged() =>
        NotificarEstadoConversion();

    private void NotificarEstadoConversion()
    {
        OnPropertyChanged(nameof(PuedeConfirmarConversion));
    }

    partial void OnConversionTipoNuevoChanged(string? value)
    {
        var requiereLotes = value is "LOTE" or "LOTE_Y_SERIE";
        var requiereSeries = value is "SERIE" or "LOTE_Y_SERIE";
        foreach (var bodega in ConversionBodegas)
        {
            if (!requiereLotes)
                bodega.Lotes.Clear();
            if (!requiereSeries)
                bodega.Series.Clear();
        }
        if (!requiereLotes)
            ConversionControlCaducidad = false;
        else if (!ConversionControlCaducidad)
        {
            ConversionControlCaducidad = true;
            ConversionDiasAnticipacionCaducidad = 30;
        }
        OnPropertyChanged(nameof(MostrarConversionCaducidad));
        OnPropertyChanged(nameof(MostrarColumnasConversionFecha));
        MensajeError = null;
        NotificarEstadoConversion();
    }

    partial void OnConversionControlCaducidadChanged(bool value)
    {
        ConversionDiasAnticipacionCaducidad = 30;
        if (!value)
            foreach (var lote in ConversionBodegas.SelectMany(x => x.Lotes))
            {
                lote.FechaElaboracion = null;
                lote.FechaCaducidad = null;
            }
        OnPropertyChanged(nameof(MostrarColumnasConversionFecha));
        NotificarEstadoConversion();
    }

    partial void OnConversionDiasAnticipacionCaducidadChanged(int value) =>
        NotificarEstadoConversion();

    partial void OnConversionMotivoChanged(string value) =>
        NotificarEstadoConversion();

    partial void OnIsLoadingChanged(bool value)
    {
        NotificarEstadoConversion();
        NotificarEstadoBotonesAjuste();
    }

    [RelayCommand]
    private async Task ConfirmarConversionAsync()
    {
        if (!_productoId.HasValue || string.IsNullOrWhiteSpace(ConversionTipoNuevo) ||
            !ConversionMotivoId.HasValue)
        {
            MensajeError = "Seleccione el nuevo control e ingrese el motivo.";
            return;
        }
        var errorDistribucion = ValidarDistribucionConversion();
        if (errorDistribucion is not null)
        {
            MensajeError = errorDistribucion;
            return;
        }
        long? productoIdParaRecargar = null;
        IsLoading = true;
        try
        {
            var result = await _inventoryService.ConvertirControlAsync(
                new ConversionControlInventarioRequest
                {
                    EmpresaId = ObtenerEmpresaId(),
                    ProductoId = _productoId.Value,
                    UsuarioId = _currentSession.UsuarioId,
                    MotivoOperacionInventarioId = ConversionMotivoId.Value,
                    TipoControlAnterior = ConversionTipoAnterior,
                    TipoControlNuevo = ConversionTipoNuevo,
                    ControlCaducidad = MostrarConversionCaducidad &&
                        ConversionControlCaducidad,
                    DiasAnticipacionCaducidad =
                        ConversionDiasAnticipacionCaducidad,
                    Motivo = ConversionMotivo,
                    Bodegas = ConversionBodegas.Select(b =>
                        new ConversionControlBodegaRequest
                        {
                            BodegaId = b.BodegaId,
                            StockEsperado = b.StockActual,
                            Lotes = b.Lotes.Select(l => new ConversionControlLoteRequest
                            {
                                ProductoLoteId = l.ProductoLoteId,
                                NumeroLote = l.NumeroLote,
                                CantidadBase = l.CantidadBase,
                                FechaElaboracion = ConversionControlCaducidad
                                    ? l.FechaElaboracion : null,
                                FechaCaducidad = ConversionControlCaducidad
                                    ? l.FechaCaducidad : null,
                                EsRegularizacion = l.EsRegularizacion
                            }).ToList(),
                            Series = b.Series.Select(s => new ConversionControlSerieRequest
                            {
                                ProductoSerieId = s.ProductoSerieId,
                                NumeroSerie = s.NumeroSerie,
                                NumeroLote = s.NumeroLote
                            }).ToList()
                        }).ToList()
                });
            MensajeError = result.Success ? null : result.Message;
            if (!result.Success) return;
            IsConversionOpen = false;
            productoIdParaRecargar = _productoId.Value;
        }
        finally
        {
            IsLoading = false;
        }

        if (productoIdParaRecargar.HasValue)
        {
            await EditarAsync(productoIdParaRecargar.Value);
        }
    }

    private string? ValidarDistribucionConversion()
    {
        var hoy = DateTime.Today;
        var seriesGlobales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var bodega in ConversionBodegas)
        {
            if (ConversionDestinoLotes)
            {
                if (bodega.Lotes.Any(x =>
                        string.IsNullOrWhiteSpace(x.NumeroLote) ||
                        x.CantidadBase <= 0))
                    return $"Complete número y cantidad mayor a cero para todos los lotes de {bodega.BodegaDisplay}.";
                if (bodega.Lotes.Select(x => x.NumeroLote.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
                    bodega.Lotes.Count)
                    return $"No repita números de lote en {bodega.BodegaDisplay}.";
                if (bodega.CantidadLotesPendiente != 0)
                    return $"Distribuya exactamente el stock requerido entre los lotes de {bodega.BodegaDisplay}.";
                if (ConversionControlCaducidad &&
                    ConversionDiasAnticipacionCaducidad <= 0)
                    return "Los días de anticipación deben ser mayores que cero.";
                if (ConversionControlCaducidad &&
                    bodega.Lotes.Any(x => x.FechaElaboracion.HasValue &&
                                           x.FechaElaboracion.Value.Date > hoy))
                    return "La fecha de elaboración no puede ser posterior a la fecha actual.";
                if (ConversionControlCaducidad && bodega.Lotes.Any(x =>
                        !x.FechaCaducidad.HasValue))
                    return "Ingrese la caducidad de todos los lotes en formato DD/MM/AAAA.";
                if (ConversionControlCaducidad &&
                    bodega.Lotes.Any(x => x.FechaCaducidad.HasValue &&
                                           x.FechaCaducidad.Value.Date <= hoy))
                    return "La fecha de caducidad debe ser posterior a la fecha actual.";
                if (ConversionControlCaducidad &&
                    bodega.Lotes.Any(x => x.FechaElaboracion.HasValue &&
                                           x.FechaCaducidad.HasValue &&
                                          x.FechaElaboracion.Value.Date >
                                          x.FechaCaducidad.Value.Date))
                    return "La elaboración no puede ser posterior a la caducidad.";
            }

            if (!ConversionDestinoSeries)
                continue;
            if (bodega.StockActual != decimal.Truncate(bodega.StockActual))
                return $"El stock de {bodega.BodegaDisplay} debe ser entero para control por series.";
            if (bodega.CantidadSeriesPendiente != 0)
                return $"Registre exactamente {bodega.StockActual:0} series en {bodega.BodegaDisplay}.";
            foreach (var serie in bodega.Series)
            {
                if (string.IsNullOrWhiteSpace(serie.NumeroSerie) ||
                    !seriesGlobales.Add(serie.NumeroSerie.Trim()))
                    return "Las series deben estar completas y no repetirse.";
                if (ConversionDestinoLotes &&
                    (string.IsNullOrWhiteSpace(serie.NumeroLote) ||
                     !bodega.Lotes.Any(x => string.Equals(
                         x.NumeroLote.Trim(), serie.NumeroLote.Trim(),
                         StringComparison.OrdinalIgnoreCase))))
                    return $"Asocie cada serie de {bodega.BodegaDisplay} a un lote válido.";
            }
            if (ConversionDestinoLotes && bodega.Lotes.Any(lote =>
                    lote.CantidadBase != decimal.Truncate(lote.CantidadBase) ||
                    bodega.Series.Count(serie => string.Equals(
                        serie.NumeroLote?.Trim(), lote.NumeroLote.Trim(),
                        StringComparison.OrdinalIgnoreCase)) !=
                    (int)lote.CantidadBase))
                return $"Las series asociadas a cada lote deben coincidir con su cantidad en {bodega.BodegaDisplay}.";
        }
        return null;
    }

    [RelayCommand]
    private async Task AbrirCorreccionControlAsync()
    {
        CerrarCapturaInventarioInicialSiActiva();
        if (!_productoId.HasValue) return;
        MensajeError = null;
        CorreccionMensajeExito = null;
        CorreccionMotivo = string.Empty;
        CorreccionMotivoId = null;
        await CargarCorreccionesAsync();
        await CargarMotivosAsync(MotivosCorreccion, "CORRECCION_LOTE_SERIE");
        IsCorreccionControlOpen = true;
    }

    [RelayCommand]
    private void CerrarCorreccionControl()
    {
        CancelarMensajeCorreccion();
        CorreccionMensajeExito = null;
        IsCorreccionControlOpen = false;
    }

    [RelayCommand]
    private async Task GuardarCorreccionLoteAsync(
        CorreccionLoteEditorViewModel? lote)
    {
        if (lote is null || !_productoId.HasValue ||
            !CorreccionMotivoId.HasValue)
        {
            MensajeError = "Ingrese el motivo obligatorio de la corrección.";
            return;
        }
        var hoy = DateTime.Today;
        if (MostrarColumnaCaducidad && lote.FechaElaboracion.HasValue &&
            lote.FechaElaboracion.Value.Date > hoy)
        {
            MensajeError =
                "La fecha de elaboración no puede ser posterior a la fecha actual.";
            return;
        }
        if (MostrarColumnaCaducidad && !lote.FechaCaducidad.HasValue)
        {
            MensajeError =
                "Ingrese la caducidad del lote en formato DD/MM/AAAA.";
            return;
        }
        if (MostrarColumnaCaducidad && lote.FechaCaducidad!.Value.Date <= hoy)
        {
            MensajeError =
                "La fecha de caducidad debe ser posterior a la fecha actual.";
            return;
        }
        if (MostrarColumnaCaducidad && lote.FechaElaboracion.HasValue &&
            lote.FechaElaboracion.Value.Date > lote.FechaCaducidad!.Value.Date)
        {
            MensajeError =
                "La fecha de elaboración no puede ser posterior a la fecha de caducidad.";
            return;
        }
        IsCorreccionSaving = true;
        try
        {
            var result = await _inventoryService.CorregirLoteAsync(
                new CorregirLoteRequest
                {
                    EmpresaId = ObtenerEmpresaId(),
                    ProductoId = _productoId.Value,
                    LoteId = lote.LoteId,
                    UsuarioId = _currentSession.UsuarioId,
                    MotivoOperacionInventarioId = CorreccionMotivoId.Value,
                    NumeroLote = lote.NumeroLote,
                    FechaElaboracion = MostrarColumnaCaducidad
                        ? lote.FechaElaboracion : null,
                    FechaCaducidad = MostrarColumnaCaducidad
                        ? lote.FechaCaducidad : null,
                    Motivo = CorreccionMotivo
                });
            MensajeError = result.Success ? null : result.Message;
            if (result.Success)
            {
                await CargarCorreccionesAsync();
                await RefrescarIngresosHistoricosAsync();
                MostrarMensajeCorreccionExitosa(
                    "Los cambios del lote se guardaron correctamente.");
            }
        }
        finally
        {
            IsCorreccionSaving = false;
        }
    }

    [RelayCommand]
    private async Task GuardarCorreccionSerieAsync(
        CorreccionSerieEditorViewModel? serie)
    {
        if (serie is null || !_productoId.HasValue ||
            !CorreccionMotivoId.HasValue)
        {
            MensajeError = "Ingrese el motivo obligatorio de la corrección.";
            return;
        }
        IsCorreccionSaving = true;
        try
        {
            var result = await _inventoryService.CorregirSerieAsync(
                new CorregirSerieRequest
                {
                    EmpresaId = ObtenerEmpresaId(),
                    ProductoId = _productoId.Value,
                    SerieId = serie.SerieId,
                    UsuarioId = _currentSession.UsuarioId,
                    MotivoOperacionInventarioId = CorreccionMotivoId.Value,
                    NumeroSerie = serie.NumeroSerie,
                    Motivo = CorreccionMotivo
                });
            MensajeError = result.Success ? null : result.Message;
            if (result.Success)
            {
                await CargarCorreccionesAsync();
                await RefrescarIngresosHistoricosAsync();
                MostrarMensajeCorreccionExitosa(
                    "Los cambios de la serie se guardaron correctamente.");
            }
        }
        finally
        {
            IsCorreccionSaving = false;
        }
    }

    private async Task CargarCorreccionesAsync()
    {
        if (!_productoId.HasValue) return;
        var estado = await _inventoryService.ObtenerEstadoControlAsync(
            ObtenerEmpresaId(), _productoId.Value);
        LotesCorreccion.Clear();
        SeriesCorreccion.Clear();
        if (estado is null) return;
        foreach (var lote in estado.Bodegas.SelectMany(x => x.Lotes)
                     .GroupBy(x => x.LoteId).Select(x => x.First()))
            LotesCorreccion.Add(new CorreccionLoteEditorViewModel
            {
                LoteId = lote.LoteId,
                NumeroLote = lote.NumeroLote,
                FechaElaboracion = lote.FechaElaboracion,
                FechaCaducidad = lote.FechaCaducidad
            });
        foreach (var serie in estado.Bodegas.SelectMany(x => x.Series))
            SeriesCorreccion.Add(new CorreccionSerieEditorViewModel
            {
                SerieId = serie.SerieId,
                NumeroSerie = serie.NumeroSerie,
                NumeroLote = serie.NumeroLote
            });
    }

    private async Task CargarMotivosAsync(
        ObservableCollection<MotivoOperacionInventarioDto> destino,
        string tipoOperacion)
    {
        var seleccionActual = destino == MotivosAjuste
            ? AjusteMotivoId
            : destino == MotivosConversion ? ConversionMotivoId : CorreccionMotivoId;
        var items = await _inventoryService.ObtenerMotivosOperacionAsync(
            ObtenerEmpresaId(), tipoOperacion);
        destino.Clear();
        foreach (var item in items)
            destino.Add(item);
        if (seleccionActual.HasValue && destino.All(x => x.Id != seleccionActual))
        {
            if (destino == MotivosAjuste) AjusteMotivoId = null;
            else if (destino == MotivosConversion) ConversionMotivoId = null;
            else CorreccionMotivoId = null;
        }
    }

    [RelayCommand]
    private void AbrirNuevoMotivo(string? destino)
    {
        if (!PuedeCrearMotivo || string.IsNullOrWhiteSpace(destino)) return;
        _nuevoMotivoDestino = destino;
        _nuevoMotivoTipo = destino switch
        {
            "AJUSTE" => AjusteTipo == "ENTRADA"
                ? "AJUSTE_ENTRADA" : "AJUSTE_SALIDA",
            "CONVERSION" => "CONVERSION_CONTROL",
            "CORRECCION" => "CORRECCION_LOTE_SERIE",
            _ => string.Empty
        };
        if (_nuevoMotivoTipo.Length == 0) return;
        NuevoMotivoNombre = string.Empty;
        NuevoMotivoDescripcion = null;
        NuevoMotivoError = null;
        IsNuevoMotivoOpen = true;
    }

    [RelayCommand]
    private void CerrarNuevoMotivo() => IsNuevoMotivoOpen = false;

    [RelayCommand]
    private async Task GuardarNuevoMotivoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoMotivoNombre))
        {
            NuevoMotivoError = "Ingrese el nombre obligatorio del motivo.";
            return;
        }
        IsNuevoMotivoSaving = true;
        try
        {
            var result = await _inventoryService.CrearMotivoOperacionAsync(
                new CrearMotivoOperacionInventarioRequest
                {
                    EmpresaId = ObtenerEmpresaId(),
                    UsuarioId = _currentSession.UsuarioId,
                    TipoOperacion = _nuevoMotivoTipo,
                    Nombre = NuevoMotivoNombre,
                    Descripcion = NuevoMotivoDescripcion
                });
            if (!result.Success)
            {
                NuevoMotivoError = result.Message;
                return;
            }
            var destino = _nuevoMotivoDestino == "AJUSTE" ? MotivosAjuste
                : _nuevoMotivoDestino == "CONVERSION" ? MotivosConversion
                : MotivosCorreccion;
            await CargarMotivosAsync(destino, _nuevoMotivoTipo);
            if (_nuevoMotivoDestino == "AJUSTE")
                AjusteMotivoId = result.MovimientoInventarioId;
            else if (_nuevoMotivoDestino == "CONVERSION")
                ConversionMotivoId = result.MovimientoInventarioId;
            else
                CorreccionMotivoId = result.MovimientoInventarioId;
            IsNuevoMotivoOpen = false;
        }
        finally
        {
            IsNuevoMotivoSaving = false;
        }
    }

    private void MostrarMensajeCorreccionExitosa(string mensaje)
    {
        CancelarMensajeCorreccion();
        var cancellationTokenSource = new CancellationTokenSource();
        _correccionMensajeCancellationTokenSource = cancellationTokenSource;
        CorreccionMensajeExito = mensaje;
        _ = OcultarMensajeCorreccionExitosaAsync(cancellationTokenSource);
    }

    private void CancelarMensajeCorreccion()
    {
        var cancellationTokenSource =
            _correccionMensajeCancellationTokenSource;
        _correccionMensajeCancellationTokenSource = null;
        cancellationTokenSource?.Cancel();
    }

    private async Task OcultarMensajeCorreccionExitosaAsync(
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(3),
                cancellationTokenSource.Token);
            if (_correccionMensajeCancellationTokenSource ==
                cancellationTokenSource)
                CorreccionMensajeExito = null;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(
                    _correccionMensajeCancellationTokenSource,
                    cancellationTokenSource))
                _correccionMensajeCancellationTokenSource = null;
            cancellationTokenSource.Dispose();
        }
    }

    private async Task RefrescarIngresosHistoricosAsync()
    {
        if (!_productoId.HasValue)
            return;

        var producto = await _productService.ObtenerProductoAsync(
            _productoId.Value, ObtenerEmpresaId());
        if (producto is null)
            return;

        var pendientes = InventariosIniciales
            .Where(x => !x.EsHistorico)
            .ToList();
        InventariosIniciales.Clear();
        foreach (var historico in producto.InventariosIniciales
                     .Where(x => x.EsHistorico))
            InventariosIniciales.Add(historico);
        foreach (var pendiente in pendientes)
            InventariosIniciales.Add(pendiente);
    }

    private static IEnumerable<string> ObtenerControlesDestino(string actual) =>
        actual switch
        {
            "NORMAL" => ["LOTE", "SERIE", "LOTE_Y_SERIE"],
            "LOTE" => ["NORMAL", "LOTE_Y_SERIE"],
            "SERIE" => ["NORMAL", "LOTE_Y_SERIE"],
            "LOTE_Y_SERIE" => ["NORMAL", "LOTE", "SERIE"],
            _ => []
        };

    [RelayCommand]
    private void AgregarEntradaInicialExistente()
    {
        if (!PuedeCompletarInventarioInicial)
        {
            MensajeError = MotivoNoPuedeCompletarInventarioInicial;
            return;
        }
        MostrarEntradaInicialExistente = true;
        RegistrarInventarioInicial = true;
        MensajeError = null;
    }

    [RelayCommand]
    private void CancelarEntradaInicialExistente()
    {
        MostrarEntradaInicialExistente = false;
        foreach (var item in InventariosIniciales.Where(x => !x.EsHistorico).ToList())
            InventariosIniciales.Remove(item);
        RegistrarInventarioInicial =
            InventariosIniciales.Any(x => x.EsHistorico);
        CantidadPresentacionesInicial = 0;
        CostoPresentacionInicial = 0;
        InventarioBodegaId = null;
        InventarioUbicacion = null;
        InventarioStockMinimo = 0;
        NumeroLoteInicial = null;
        FechaElaboracionInicial = null;
        FechaCaducidadInicial = null;
        SeriesInicialesTexto = null;
        NumeroSerieInicial = null;
        LoteSerieInicial = null;
        LotesInventarioInicial.Clear();
        SeriesInventarioInicial.Clear();
        MensajeError = null;
    }

    private void CerrarCapturaInventarioInicialSiActiva()
    {
        if (MostrarEntradaInicialExistente)
            CancelarEntradaInicialExistente();
    }

    [RelayCommand]
    private void AgregarLoteInicial()
    {
        LotesInventarioInicial.Add(
            new LoteInventarioInicialEditorViewModel());
        MensajeError = null;
    }

    [RelayCommand]
    private void QuitarLoteInicial(object? item)
    {
        if (item is LoteInventarioInicialEditorViewModel lote)
            LotesInventarioInicial.Remove(lote);
    }

    [RelayCommand]
    private void AgregarSerieInicial()
    {
        SeriesInventarioInicial.Add(
            new SerieInventarioInicialEditorViewModel());
        MensajeError = null;
    }

    [RelayCommand]
    private void QuitarSerieInicial(object? item)
    {
        if (item is SerieInventarioInicialEditorViewModel serie)
            SeriesInventarioInicial.Remove(serie);
        NotificarDistribucionInventario();
    }

    private void LoteInicialPropertyChanged(
        object? sender,
        System.ComponentModel.PropertyChangedEventArgs e) =>
        NotificarDistribucionInventario();

    private void NotificarDistribucionInventario()
    {
        OnPropertyChanged(nameof(TotalLotesInicial));
        OnPropertyChanged(nameof(CantidadLotesPendiente));
        OnPropertyChanged(nameof(TotalSeriesInicial));
        OnPropertyChanged(nameof(CantidadSeriesPendiente));
    }

    [RelayCommand]
    private void AgregarInventarioInicial()
    {
        if (!InventarioBodegaId.HasValue)
        {
            MensajeError = "Seleccione la bodega.";
            return;
        }
        if (string.IsNullOrWhiteSpace(InventarioPresentacionCodigo) ||
            !PresentacionesInventarioInicial.Any(x =>
                x.Codigo == InventarioPresentacionCodigo))
        {
            MensajeError = "Seleccione la presentación de ingreso.";
            return;
        }
        if (!IsEditing && InventariosIniciales.Any(x =>
                x.BodegaId == InventarioBodegaId.Value &&
                string.Equals(
                    x.PresentacionCodigo,
                    InventarioPresentacionCodigo,
                    StringComparison.OrdinalIgnoreCase)))
        {
            MensajeError =
                "La presentación seleccionada ya tiene una entrada inicial " +
                "en esta bodega.";
            return;
        }
        if (CantidadPresentacionesInicial <= 0)
        {
            MensajeError = "Ingrese una cantidad mayor a 0.";
            return;
        }
        if (CostoPresentacionInicial <= 0)
        {
            MensajeError = "Ingrese un costo de presentación mayor a 0.";
            return;
        }
        if (InventarioUbicacion?.Trim().Length > 255)
        {
            MensajeError = "La ubicación admite hasta 255 caracteres.";
            return;
        }
        if (InventarioStockMinimo < 0 ||
            decimal.Round(InventarioStockMinimo, 6) !=
            InventarioStockMinimo)
        {
            MensajeError =
                "El stock mínimo debe ser positivo y admite hasta 6 decimales.";
            return;
        }
        var ubicacionNormalizada =
            InventarioUbicacion?.Trim().ToUpperInvariant();
        var configuracionBodega = InventariosIniciales
            .FirstOrDefault(x => x.BodegaId == InventarioBodegaId.Value);
        if (configuracionBodega is not null &&
            (configuracionBodega.StockMinimo != InventarioStockMinimo ||
             !string.Equals(
                 configuracionBodega.Ubicacion?.Trim().ToUpperInvariant(),
                 ubicacionNormalizada,
                 StringComparison.Ordinal)))
        {
            MensajeError =
                "La ubicación y el stock mínimo deben coincidir con la configuración ya agregada para esta bodega.";
            return;
        }

        if (decimal.Round(CantidadPresentacionesInicial, 2) !=
            CantidadPresentacionesInicial ||
            decimal.Round(CostoPresentacionInicial, 6) !=
            CostoPresentacionInicial)
        {
            MensajeError =
                "La cantidad admite 2 decimales y el costo 6 decimales.";
            return;
        }

        var controlaLotes =
            TipoControlInventario is "LOTE" or "LOTE_Y_SERIE";
        var controlaSeries =
            TipoControlInventario is "SERIE" or "LOTE_Y_SERIE";
        if (controlaLotes && LotesInventarioInicial.Count == 0)
        {
            MensajeError = "Agregue al menos un lote.";
            return;
        }
        if (controlaLotes &&
            (LotesInventarioInicial.Any(x =>
                 string.IsNullOrWhiteSpace(x.NumeroLote) ||
                 x.CantidadBase <= 0) ||
             LotesInventarioInicial.Select(x => x.NumeroLote.Trim())
                 .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
             LotesInventarioInicial.Count))
        {
            MensajeError =
                "Complete número y cantidad de cada lote, sin duplicados.";
            return;
        }
        if (controlaLotes &&
            TotalLotesInicial != CantidadBaseInicial)
        {
            MensajeError =
                "La suma de los lotes debe coincidir con la cantidad base.";
            return;
        }
        var hoy = DateTime.Today;
        if (controlaLotes &&
            LotesInventarioInicial.Any(x =>
                x.FechaElaboracion.HasValue &&
                x.FechaElaboracion.Value.Date > hoy))
        {
            MensajeError =
                "La fecha de elaboración no puede ser posterior a la fecha actual.";
            return;
        }
        if (controlaLotes &&
            LotesInventarioInicial.Any(x =>
                x.FechaCaducidad.HasValue &&
                x.FechaCaducidad.Value.Date <= hoy))
        {
            MensajeError =
                "La fecha de caducidad debe ser posterior a la fecha actual.";
            return;
        }
        if (controlaLotes &&
            LotesInventarioInicial.Any(x =>
                x.FechaElaboracion.HasValue &&
                x.FechaCaducidad.HasValue &&
                x.FechaElaboracion.Value.Date >
                x.FechaCaducidad.Value.Date))
        {
            MensajeError =
                "La fecha de elaboración no puede ser posterior a la fecha de caducidad.";
            return;
        }
        if (controlaLotes && AlertaCaducidad &&
            LotesInventarioInicial.Any(x => !x.FechaCaducidad.HasValue))
        {
            MensajeError =
                "Ingrese la caducidad de todos los lotes en formato DD/MM/AAAA.";
            return;
        }
        if (controlaSeries && SeriesInventarioInicial.Count == 0)
        {
            MensajeError = "Agregue al menos una serie.";
            return;
        }
        if (controlaSeries &&
            (SeriesInventarioInicial.Any(x =>
                 string.IsNullOrWhiteSpace(x.NumeroSerie)) ||
             SeriesInventarioInicial.Select(x => x.NumeroSerie.Trim())
                 .Distinct(StringComparer.OrdinalIgnoreCase).Count() !=
             SeriesInventarioInicial.Count))
        {
            MensajeError =
                "Complete cada número de serie y no ingrese duplicados.";
            return;
        }
        if (controlaSeries &&
            (CantidadBaseInicial != decimal.Truncate(CantidadBaseInicial) ||
             TotalSeriesInicial != (int)CantidadBaseInicial))
        {
            MensajeError =
                "Debe agregar una serie por cada unidad base.";
            return;
        }
        if (TipoControlInventario == "LOTE_Y_SERIE" &&
            SeriesInventarioInicial.Any(serie =>
                !LotesInventarioInicial.Any(lote =>
                    string.Equals(lote.NumeroLote, serie.NumeroLote,
                        StringComparison.OrdinalIgnoreCase))))
        {
            MensajeError =
                "Cada serie debe estar asociada a uno de los lotes agregados.";
            return;
        }
        if (TipoControlInventario == "LOTE_Y_SERIE" &&
            LotesInventarioInicial.Any(lote =>
                lote.CantidadBase != decimal.Truncate(lote.CantidadBase) ||
                SeriesInventarioInicial.Count(serie =>
                    string.Equals(serie.NumeroLote, lote.NumeroLote,
                        StringComparison.OrdinalIgnoreCase)) !=
                (int)lote.CantidadBase))
        {
            MensajeError =
                "La cantidad de series asociadas a cada lote debe coincidir con la cantidad del lote.";
            return;
        }

        MensajeError = null;
        InventariosIniciales.Add(new ProductoInventarioInicialRequest
        {
            BodegaId = InventarioBodegaId ?? 0,
            UsuarioId = _currentSession.UsuarioId,
            PresentacionCodigo = InventarioPresentacionCodigo,
            CantidadPresentaciones = CantidadPresentacionesInicial,
            CostoUnitarioPresentacion =
                CostoPresentacionInicial,
            Ubicacion = ubicacionNormalizada,
            StockMinimo = InventarioStockMinimo,
            NumeroLote = NumeroLoteInicial,
            FechaElaboracion = FechaElaboracionInicial.HasValue
                ? DateOnly.FromDateTime(FechaElaboracionInicial.Value)
                : null,
            FechaCaducidad = FechaCaducidadInicial.HasValue
                ? DateOnly.FromDateTime(FechaCaducidadInicial.Value)
                : null,
            NumerosSerie = SeriesInventarioInicial.Select(x =>
                    string.IsNullOrWhiteSpace(x.NumeroLote)
                        ? x.NumeroSerie
                        : $"{x.NumeroLote}|{x.NumeroSerie}")
                .ToList(),
            Lotes = LotesInventarioInicial.Select(x =>
                new ProductoInventarioInicialLoteRequest
                {
                    NumeroLote = x.NumeroLote,
                    CantidadBase = x.CantidadBase,
                    FechaElaboracion = x.FechaElaboracion,
                    FechaCaducidad = x.FechaCaducidad
                }).ToList()
            ,
            BodegaNombre = Existencias.FirstOrDefault(x =>
                x.BodegaId == InventarioBodegaId)?.BodegaNombre ??
                string.Empty,
            BodegaCodigo = Existencias.FirstOrDefault(x =>
                x.BodegaId == InventarioBodegaId)?.BodegaCodigo ??
                string.Empty,
            PresentacionNombre = PresentacionesInventarioInicial
                .FirstOrDefault(x =>
                    x.Codigo == InventarioPresentacionCodigo)?.Nombre ??
                InventarioPresentacionCodigo
        });
        var indiceExistencia = Existencias
            .Select((item, index) => new { item, index })
            .FirstOrDefault(x =>
                x.item.BodegaId == InventarioBodegaId)?.index ?? -1;
        if (indiceExistencia >= 0)
        {
            var existencia = Existencias[indiceExistencia];
            existencia.StockMinimo = InventarioStockMinimo;
            existencia.Ubicacion = ubicacionNormalizada;
            Existencias[indiceExistencia] = existencia;
        }
        OnPropertyChanged(nameof(CostoBaseInventarioInicial));
        ActualizarReferenciasPrecios();
        CantidadPresentacionesInicial = 0;
        CostoPresentacionInicial = 0;
        InventarioBodegaId = null;
        InventarioPresentacionCodigo = string.Empty;
        InventarioUbicacion = null;
        InventarioStockMinimo = 0;
        NumeroLoteInicial = null;
        FechaElaboracionInicial = null;
        FechaCaducidadInicial = null;
        SeriesInicialesTexto = null;
        NumeroSerieInicial = null;
        LoteSerieInicial = null;
        LotesInventarioInicial.Clear();
        SeriesInventarioInicial.Clear();
        NotificarDistribucionInventario();
    }

    [RelayCommand]
    private void QuitarInventarioInicial(ProductoInventarioInicialRequest? item)
    {
        if (item is null)
            return;
        if (item.EsHistorico)
        {
            MensajeError =
                "El inventario inicial confirmado es histórico; corríjalo mediante un ajuste de inventario.";
            return;
        }
        if (item.PresentacionCodigo == "BASE" &&
            InventariosIniciales.Count > 1)
        {
            MensajeError =
                "Quite primero las presentaciones adicionales antes de retirar la BASE.";
            return;
        }
        InventariosIniciales.Remove(item);
        if (!InventariosIniciales.Any(x => x.BodegaId == item.BodegaId))
        {
            var indice = Existencias
                .Select((existencia, index) => new { existencia, index })
                .FirstOrDefault(x =>
                    x.existencia.BodegaId == item.BodegaId)?.index ?? -1;
            if (indice >= 0)
            {
                var existencia = Existencias[indice];
                existencia.StockMinimo = 0;
                existencia.Ubicacion = null;
                Existencias[indice] = existencia;
            }
        }
        OnPropertyChanged(nameof(CostoBaseInventarioInicial));
        ActualizarReferenciasPrecios();
    }

    private void NotificarPropiedadesCalculadas()
    {
        OnPropertyChanged(
            nameof(MostrarConfiguracionInventario));

        OnPropertyChanged(
            nameof(MostrarConfiguracionCaducidad));
        OnPropertyChanged(nameof(MostrarLoteAsociadoSeries));
        OnPropertyChanged(nameof(MostrarCorreccionLotesSeries));
        OnPropertyChanged(nameof(AjusteRequiereLotes));
        OnPropertyChanged(nameof(AjusteRequiereSeries));
        OnPropertyChanged(nameof(MostrarAjusteLotes));
        OnPropertyChanged(nameof(MostrarAjusteSeriesNuevas));
        OnPropertyChanged(nameof(MostrarAjusteSeriesDisponibles));
        OnPropertyChanged(nameof(MostrarColumnaCaducidad));
    }

    private long ObtenerEmpresaId()
    {
        return _currentSession.EmpresaId ?? 0;
    }
}

public partial class ConversionBodegaEditorViewModel : ObservableObject
{
    public ConversionBodegaEditorViewModel()
    {
        Lotes.CollectionChanged += (_, args) =>
        {
            if (args.OldItems is not null)
                foreach (ConversionLoteEditorViewModel item in args.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
            if (args.NewItems is not null)
                foreach (ConversionLoteEditorViewModel item in args.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            NotificarDistribucion();
        };
        Series.CollectionChanged += (_, args) =>
        {
            if (args.OldItems is not null)
                foreach (ConversionSerieEditorViewModel item in args.OldItems)
                    item.PropertyChanged -= OnItemPropertyChanged;
            if (args.NewItems is not null)
                foreach (ConversionSerieEditorViewModel item in args.NewItems)
                    item.PropertyChanged += OnItemPropertyChanged;
            NotificarDistribucion();
        };
    }

    public event Action? DistribucionChanged;
    public long BodegaId { get; set; }
    public string BodegaCodigo { get; set; } = string.Empty;
    public string BodegaNombre { get; set; } = string.Empty;
    public string BodegaDisplay => BodegaDisplayFormatter.Format(
        BodegaCodigo, BodegaNombre);
    public decimal StockActual { get; set; }
    public decimal StockReservado { get; set; }
    public ObservableCollection<ConversionLoteEditorViewModel> Lotes { get; } = new();
    public ObservableCollection<ConversionSerieEditorViewModel> Series { get; } = new();
    public decimal CantidadLotesAsignada => Lotes.Sum(x => x.CantidadBase);
    public decimal CantidadLotesPendiente => StockActual - CantidadLotesAsignada;
    public int CantidadSeriesAsignada => Series.Count;
    public decimal CantidadSeriesPendiente => StockActual - CantidadSeriesAsignada;

    public bool DistribucionCompleta(bool requiereLotes, bool requiereSeries) =>
        StockReservado == 0 &&
        (!requiereLotes || CantidadLotesPendiente == 0) &&
        (!requiereSeries || CantidadSeriesPendiente == 0);

    private void OnItemPropertyChanged(
        object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        NotificarDistribucion();

    private void NotificarDistribucion()
    {
        OnPropertyChanged(nameof(CantidadLotesAsignada));
        OnPropertyChanged(nameof(CantidadLotesPendiente));
        OnPropertyChanged(nameof(CantidadSeriesAsignada));
        OnPropertyChanged(nameof(CantidadSeriesPendiente));
        DistribucionChanged?.Invoke();
    }
}

public partial class ConversionLoteEditorViewModel : ObservableObject
{
    public long? ProductoLoteId { get; set; }
    [ObservableProperty] private string numeroLote = string.Empty;
    [ObservableProperty] private decimal cantidadBase;
    [ObservableProperty] private DateTime? fechaElaboracion;
    [ObservableProperty] private DateTime? fechaCaducidad;
    [ObservableProperty] private bool esRegularizacion;
}

public partial class ConversionSerieEditorViewModel : ObservableObject
{
    public long? ProductoSerieId { get; set; }
    [ObservableProperty] private string numeroSerie = string.Empty;
    [ObservableProperty] private string? numeroLote;
}

public partial class CorreccionLoteEditorViewModel : ObservableObject
{
    public long LoteId { get; set; }
    [ObservableProperty] private string numeroLote = string.Empty;
    [ObservableProperty] private DateTime? fechaElaboracion;
    [ObservableProperty] private DateTime? fechaCaducidad;
}

public partial class CorreccionSerieEditorViewModel : ObservableObject
{
    public long SerieId { get; set; }
    [ObservableProperty] private string numeroSerie = string.Empty;
    public string? NumeroLote { get; set; }
}

public partial class AjusteSerieDisponibleViewModel : ObservableObject
{
    public long SerieId { get; set; }
    public string NumeroSerie { get; set; } = string.Empty;
    public string? NumeroLote { get; set; }
    public string? Ubicacion { get; set; }
    [ObservableProperty] private bool isSelected;
}

public partial class AjusteLoteEditorViewModel : ObservableObject
{
    public Guid FilaId { get; } = Guid.NewGuid();
    public ObservableCollection<EstadoControlLoteDto> Sugerencias { get; } = new();
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsExistente))]
    [NotifyPropertyChangedFor(nameof(EsNuevo))]
    private long? loteId;
    private string numeroLote = string.Empty;
    public string NumeroLote
    {
        get => numeroLote;
        set
        {
            if (!SetProperty(ref numeroLote, value ?? string.Empty)) return;
            OnPropertyChanged(nameof(EsNuevo));
        }
    }
    [ObservableProperty] private decimal cantidadBase;
    [ObservableProperty] private decimal stockActual;
    [ObservableProperty] private DateTime? fechaElaboracion;
    [ObservableProperty] private DateTime? fechaCaducidad;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TieneLoteSimilar))]
    private EstadoControlLoteDto? loteSimilar;
    [ObservableProperty] private bool permitirCrearLoteSimilar;
    [ObservableProperty] private bool mostrarSugerencias;
    [ObservableProperty]
    private EstadoControlLoteDto? loteSugeridoSeleccionado;
    public bool EsExistente => LoteId.HasValue;
    public bool EsNuevo => !EsExistente && !string.IsNullOrWhiteSpace(NumeroLote);
    public bool TieneLoteSimilar => LoteSimilar is not null;
}

public partial class AjusteSerieNuevaEditorViewModel : ObservableObject
{
    [ObservableProperty] private string numeroSerie = string.Empty;
    [ObservableProperty] private Guid? loteFilaId;
}

public partial class PrecioListaEditorViewModel : ObservableObject
{
    public long ListaPrecioId { get; }
    public string ListaCodigo { get; }
    public string ListaNombre { get; }
    public bool EsListaBase { get; }
    public decimal? DescuentoPredeterminado { get; }
    public IReadOnlyList<string> Metodos { get; }
    public string EtiquetaPorcentaje =>
        EsListaBase ? "% utilidad" : "% descuento";
    public string AyudaMetodo => EsListaBase
        ? "Calcula el precio sobre el costo promedio."
        : "Aplica el descuento sobre la Lista A.";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UsaPorcentaje))]
    private string metodoCalculo;

    [ObservableProperty]
    private decimal? porcentaje;

    [ObservableProperty]
    private decimal? precio;

    private decimal costoEquivalente;
    private decimal precioA;

    public decimal CostoEquivalente => costoEquivalente;
    public decimal PrecioListaA => precioA;
    public decimal PrecioResultante => MetodoCalculo switch
    {
        "PORCENTAJE_COSTO" =>
            ProductoNuevoRules.CalcularPrecioPorcentajeCosto(
                costoEquivalente,
                Porcentaje ?? 0),
        "DESCUENTO_PORCENTAJE" =>
            ProductoNuevoRules.CalcularPrecioConDescuento(
                precioA,
                Porcentaje ?? 0),
        _ => Precio ?? 0
    };
    public decimal Utilidad => PrecioResultante - costoEquivalente;
    public decimal MargenSobreCosto => costoEquivalente == 0
        ? 0
        : Utilidad / costoEquivalente * 100m;

    public bool UsaPorcentaje => MetodoCalculo != "PRECIO_FIJO";

    public PrecioListaEditorViewModel(
        ListaPrecioEditorDto lista,
        ProductoPrecioDto? existente)
    {
        ListaPrecioId = lista.Id;
        ListaCodigo = lista.Codigo;
        ListaNombre = lista.Nombre;
        EsListaBase = lista.EsListaBase;
        DescuentoPredeterminado =
            lista.PorcentajeDescuentoPredeterminado;
        Metodos = lista.EsListaBase
            ? ["Margen sobre costo", "Precio fijo"]
            : ["Descuento sobre Lista A", "Precio fijo"];
        metodoCalculo = existente?.MetodoCalculo ??
            (lista.EsListaBase ? "PORCENTAJE_COSTO" : "DESCUENTO_PORCENTAJE");
        Porcentaje = existente?.Porcentaje ??
            (lista.EsListaBase ? 0m : lista.PorcentajeDescuentoPredeterminado);
        Precio = existente?.Precio;
    }

    public string MetodoSeleccionado
    {
        get => MetodoCalculo == "PRECIO_FIJO"
            ? "Precio fijo"
            : Metodos[0];
        set => MetodoCalculo = value == "Precio fijo"
            ? "PRECIO_FIJO"
            : EsListaBase ? "PORCENTAJE_COSTO" : "DESCUENTO_PORCENTAJE";
    }

    partial void OnMetodoCalculoChanged(string value)
    {
        OnPropertyChanged(nameof(MetodoSeleccionado));
        NotificarCalculos();
    }

    partial void OnPorcentajeChanged(decimal? value) => NotificarCalculos();
    partial void OnPrecioChanged(decimal? value) => NotificarCalculos();

    public void ActualizarReferencias(decimal costo, decimal precioBase)
    {
        costoEquivalente = costo;
        precioA = precioBase;
        NotificarCalculos();
    }

    private void NotificarCalculos()
    {
        OnPropertyChanged(nameof(CostoEquivalente));
        OnPropertyChanged(nameof(PrecioListaA));
        OnPropertyChanged(nameof(PrecioResultante));
        OnPropertyChanged(nameof(Utilidad));
        OnPropertyChanged(nameof(MargenSobreCosto));
    }

    public ProductoPrecioDto ToDto() => new()
    {
        ListaPrecioId = ListaPrecioId,
        ListaPrecioNombre = ListaNombre,
        MetodoCalculo = MetodoCalculo,
        Porcentaje = UsaPorcentaje ? Porcentaje : null,
        Precio = UsaPorcentaje ? null : Precio,
        Estado = 1
    };
}

public sealed class PrecioPresentacionConfiguradaViewModel
{
    public string PresentacionCodigo { get; init; } = string.Empty;
    public string PresentacionNombre { get; init; } = string.Empty;
    public IReadOnlyList<PrecioListaResumenViewModel> Detalles { get; init; } = [];
}

public sealed class PrecioListaResumenViewModel
{
    public string ListaNombre { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public decimal Utilidad { get; init; }
    public decimal Margen { get; init; }
}

public partial class LoteInventarioInicialEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string numeroLote = string.Empty;

    [ObservableProperty]
    private decimal cantidadBase;

    [ObservableProperty]
    private DateTime? fechaElaboracion;

    [ObservableProperty]
    private DateTime? fechaCaducidad;
}

public partial class SerieInventarioInicialEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string numeroSerie = string.Empty;

    [ObservableProperty]
    private string? numeroLote;
}
