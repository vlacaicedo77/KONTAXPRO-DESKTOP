using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Common;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels.Products;

public partial class ProductFormViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IProductCatalogService _catalogService;
    private readonly CurrentSession _currentSession;

    private CancellationTokenSource?
    _suggestionsCancellationTokenSource;

    private long? _productoId;
    private string? _tipoCatalogoRapido;
    private readonly HashSet<long> _tarifasImpuestoIds = [];

    public event Action? CloseRequested;
    public event Action<long>? ProductSaved;
    public event Action<long>? ExistingProductRequested;

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

    public ObservableCollection<CatalogItemDto> ListasPrecio { get; }
        = new();

    public ObservableCollection<ProductoPresentacionDto>
        PresentacionesAdicionales { get; } = new();

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
    private string nombre = string.Empty;

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
    private string tipoProducto = "PRODUCTO";

    [ObservableProperty]
    private string tipoControlInventario = "NORMAL";

    [ObservableProperty]
    private bool manejaInventario = true;

    [ObservableProperty]
    private bool permiteVentaSinStock;

    [ObservableProperty]
    private bool alertaStockMinimo = true;

    [ObservableProperty]
    private decimal stockMinimo;

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
    private string presentacionNombre = string.Empty;

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

    public bool MostrarConfiguracionCaducidad =>
        MostrarConfiguracionInventario &&
        TipoControlInventario is "LOTE" or "LOTE_Y_SERIE";

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
        CurrentSession currentSession)
    {
        _productService = productService;
        _catalogService = catalogService;
        _currentSession = currentSession;
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

            TarifaImpuestoId =
                producto.Impuestos
                    .FirstOrDefault(x => x.Estado == 1)
                    ?.TarifaImpuestoId;
            _tarifasImpuestoIds.Clear();
            foreach (var impuesto in producto.Impuestos.Where(x => x.Estado == 1))
                _tarifasImpuestoIds.Add(impuesto.TarifaImpuestoId);

            var precioBase = producto.Presentaciones
                .FirstOrDefault(x => x.EsPresentacionBase)
                ?.Precios.FirstOrDefault();
            ListaPrecioBaseId = precioBase?.ListaPrecioId;
            PrecioBase = precioBase?.Precio ?? 0;

            foreach (var presentacion in producto.Presentaciones
                         .Where(x => !x.EsPresentacionBase))
            {
                PresentacionesAdicionales.Add(presentacion);
            }

            TipoProducto =
                producto.TipoProducto;

            TipoControlInventario =
                producto.TipoControlInventario;

            ManejaInventario =
                producto.ManejaInventario;

            StockMinimo =
                producto.Existencias
                    .FirstOrDefault(x =>
                        x.BodegaId == _currentSession.BodegaId)
                    ?.StockMinimo
                ?? 0;

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

        if (!SinCodigoBarras &&
            string.IsNullOrWhiteSpace(
            CodigoBarras))
        {
            MensajeError =
                "Debe ingresar el código de barras o indicar que el producto no posee uno.";
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

            var request =
                new ProductoGuardarRequest
                {
                    Id = _productoId,

                    EmpresaId =
                        empresaId,

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

                    StockMinimo =
                        manejaInventarioReal
                            ? StockMinimo
                            : 0,

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
                            Precios = ListaPrecioBaseId.HasValue
                                ?
                                [
                                    new ProductoPrecioDto
                                    {
                                        ListaPrecioId =
                                            ListaPrecioBaseId.Value,
                                        MetodoCalculo = "PRECIO_FIJO",
                                        Precio = PrecioBase,
                                        Estado = 1
                                    }
                                ]
                                : []
                        },
                        .. PresentacionesAdicionales
                    ],

                    Existencias =
                        manejaInventarioReal &&
                        _currentSession.BodegaId.HasValue
                            ?
                            [
                                new ProductoExistenciaDto
                                {
                                    BodegaId =
                                        _currentSession.BodegaId.Value,
                                    StockMinimo = StockMinimo
                                }
                            ]
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
    private void Cancelar()
    {
        if (IsQuickCatalogOpen)
        {
            CerrarCatalogoRapido();
            return;
        }

        CloseRequested?.Invoke();
    }

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
        ManejaInventario =
            value == "PRODUCTO";

        if (!ManejaInventario)
        {
            TipoControlInventario =
                "NORMAL";
        }

        NotificarPropiedadesCalculadas();
    }

    partial void OnTipoControlInventarioChanged(
        string value)
    {
        NotificarPropiedadesCalculadas();
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

        /*
         * Para producto nuevo podemos seleccionar automáticamente
         * Unidad si existe y la primera tarifa disponible.
         */

        if (!_productoId.HasValue)
        {
            if (!UnidadMedidaBaseId.HasValue)
            {
                UnidadMedidaBaseId =
                    unidades.FirstOrDefault(
                        x => x.Nombre.Equals(
                            "Unidad",
                            StringComparison.OrdinalIgnoreCase))
                    ?.Id
                    ?? unidades.FirstOrDefault()?.Id;
            }

            if (!TarifaImpuestoId.HasValue)
            {
                TarifaImpuestoId =
                    tarifas.FirstOrDefault()?.Id;
            }

            ListaPrecioBaseId ??= listas.FirstOrDefault()?.Id;
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

    partial void OnSinCodigoBarrasChanged(bool value)
    {
        OnPropertyChanged(
            nameof(TextoAyudaCodigoBarras));
    }

    private void LimpiarFormulario()
    {
        _productoId = null;

        Nombre = string.Empty;
        Modelo = null;
        PresentacionNombre = string.Empty;
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

        TipoProducto = "PRODUCTO";
        TipoControlInventario = "NORMAL";

        ManejaInventario = true;
        PermiteVentaSinStock = false;

        AlertaStockMinimo = true;
        StockMinimo = 0;

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
        PresentacionesAdicionales.Add(new ProductoPresentacionDto
        {
            Codigo = $"P{PresentacionesAdicionales.Count + 1:00}",
            Nombre = "Nueva presentación",
            FactorConversion = 1,
            PermiteCompra = true,
            PermiteVenta = true,
            Estado = 1
        });
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
    }

    private void NotificarPropiedadesCalculadas()
    {
        OnPropertyChanged(
            nameof(MostrarConfiguracionInventario));

        OnPropertyChanged(
            nameof(MostrarConfiguracionCaducidad));
    }

    private long ObtenerEmpresaId()
    {
        return _currentSession.EmpresaId ?? 0;
    }
}
