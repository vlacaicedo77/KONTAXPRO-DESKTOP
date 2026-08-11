using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Productos;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels.Products;

public partial class ProductsViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly CurrentSession _currentSession;
    private readonly INotificationService _notificationService;
    private readonly ILoadingService _loadingService;
    private readonly IMessageDialogService _messageDialogService;

    private CancellationTokenSource? _searchDebounceCancellationTokenSource;
    private CancellationTokenSource? _loadCancellationTokenSource;
    private long _loadSequence;
    private bool _suppressReload;

    public ProductFormViewModel ProductForm { get; }

    public ObservableCollection<ProductoListadoItemViewModel> Productos { get; }
        = [];
    public ObservableCollection<PaginaCatalogoItemViewModel> PaginasVisibles
        { get; } = [];

    public IReadOnlyList<int> TamanosPagina { get; } = [25, 50, 100];
    public IReadOnlyList<EstadoCatalogoItemViewModel> EstadosDisponibles { get; }
        =
        [
            new("ACTIVOS", ProductoCatalogoEstado.Activos),
            new("INACTIVOS", ProductoCatalogoEstado.Inactivos),
            new("TODOS", ProductoCatalogoEstado.Todos)
        ];

    [ObservableProperty]
    private bool isProductFormOpen;

    [ObservableProperty]
    private ProductoListadoItemViewModel? productoSeleccionado;

    [ObservableProperty]
    private string textoBusqueda = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsKpiProductos))]
    [NotifyPropertyChangedFor(nameof(EsKpiStockBajo))]
    [NotifyPropertyChangedFor(nameof(EsKpiSinStock))]
    [NotifyPropertyChangedFor(nameof(EsKpiPorCaducar))]
    private ProductoCatalogoKpi kpiActivo = ProductoCatalogoKpi.Todos;

    [ObservableProperty]
    private ProductoCatalogoEstado estadoSeleccionado =
        ProductoCatalogoEstado.Activos;

    [ObservableProperty]
    private ProductoCatalogoOrden ordenActual = ProductoCatalogoOrden.Producto;

    [ObservableProperty]
    private bool ordenDescendente;

    [ObservableProperty]
    private int paginaActual = 1;

    [ObservableProperty]
    private int tamanoPagina = 25;

    [ObservableProperty]
    private int totalItems;

    [ObservableProperty]
    private int totalPaginas;

    [ObservableProperty]
    private int totalProductos;

    [ObservableProperty]
    private int totalSinStock;

    [ObservableProperty]
    private int totalStockBajo;

    [ObservableProperty]
    private int totalPorCaducar;

    [ObservableProperty]
    private string? mensajeEstado;

    [ObservableProperty]
    private bool isNewProductCheckOpen;

    [ObservableProperty]
    private string codigoBarrasNuevoProducto = string.Empty;

    [ObservableProperty]
    private bool isCheckingBarcode;

    [ObservableProperty]
    private string? mensajeVerificacionBarcode;

    [ObservableProperty]
    private ProductoCodigoBarrasDto? productoCodigoEncontrado;

    public bool HayProductoCodigoEncontrado =>
        ProductoCodigoEncontrado is not null;
    public bool EsKpiProductos => KpiActivo == ProductoCatalogoKpi.Todos;
    public bool EsKpiStockBajo => KpiActivo == ProductoCatalogoKpi.StockBajo;
    public bool EsKpiSinStock => KpiActivo == ProductoCatalogoKpi.SinStock;
    public bool EsKpiPorCaducar => KpiActivo == ProductoCatalogoKpi.PorCaducar;
    public bool PuedeIrPaginaAnterior => PaginaActual > 1;
    public bool PuedeIrPaginaSiguiente =>
        TotalPaginas > 0 && PaginaActual < TotalPaginas;
    public bool HayResultados => Productos.Count > 0;
    public bool MostrarEstadoVacio => !IsLoading && !HayResultados;
    public string TextoPaginacion
    {
        get
        {
            if (TotalItems == 0)
                return "Mostrando 0 de 0 productos";

            var inicio = (PaginaActual - 1) * TamanoPagina + 1;
            var fin = Math.Min(PaginaActual * TamanoPagina, TotalItems);
            return $"Mostrando {inicio:N0}–{fin:N0} de {TotalItems:N0} productos";
        }
    }

    public string IndicadorCodigo => Indicador(ProductoCatalogoOrden.Codigo);
    public string IndicadorProducto => Indicador(ProductoCatalogoOrden.Producto);
    public string IndicadorCategoria => Indicador(ProductoCatalogoOrden.Categoria);
    public string IndicadorUnidad => Indicador(ProductoCatalogoOrden.Unidad);
    public string IndicadorStock => Indicador(ProductoCatalogoOrden.Stock);
    public string IndicadorCosto => Indicador(ProductoCatalogoOrden.CostoPromedio);
    public string IndicadorPrecio => Indicador(ProductoCatalogoOrden.PrecioBase);
    public string IndicadorEstado => Indicador(ProductoCatalogoOrden.Estado);

    public ProductsViewModel(
        IProductService productService,
        CurrentSession currentSession,
        ProductFormViewModel productForm,
        INotificationService notificationService,
        ILoadingService loadingService,
        IMessageDialogService messageDialogService)
    {
        _productService = productService;
        _currentSession = currentSession;
        _notificationService = notificationService;
        _loadingService = loadingService;
        _messageDialogService = messageDialogService;
        ProductForm = productForm;

        ProductForm.CloseRequested += OnProductFormCloseRequested;
        ProductForm.ProductSaved += OnProductSaved;
        ProductForm.ExistingProductRequested += OnExistingProductRequested;
    }

    public Task InitializeAsync() => CargarProductosAsync();

    [RelayCommand]
    private Task RecargarAsync() => CargarProductosAsync();

    [RelayCommand]
    private void LimpiarBusqueda()
    {
        if (!string.IsNullOrEmpty(TextoBusqueda))
            TextoBusqueda = string.Empty;
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        if (_suppressReload)
            return;

        PaginaActual = 1;
        _ = BuscarConRetardoAsync();
        NotificarEstadoVacio();
    }

    partial void OnEstadoSeleccionadoChanged(ProductoCatalogoEstado value)
    {
        if (_suppressReload)
            return;

        PaginaActual = 1;
        _ = CargarProductosAsync();
    }

    partial void OnTamanoPaginaChanged(int value)
    {
        if (_suppressReload)
            return;

        PaginaActual = 1;
        _ = CargarProductosAsync();
    }

    partial void OnIsLoadingChanged(bool value) => NotificarEstadoVacio();

    partial void OnProductoCodigoEncontradoChanged(
        ProductoCodigoBarrasDto? value) =>
        OnPropertyChanged(nameof(HayProductoCodigoEncontrado));

    partial void OnCodigoBarrasNuevoProductoChanged(string value)
    {
        ProductoCodigoEncontrado = null;
        MensajeVerificacionBarcode = null;
    }

    [RelayCommand]
    private void SeleccionarKpi(ProductoCatalogoKpi kpi)
    {
        if (KpiActivo == kpi)
            return;

        KpiActivo = kpi;
        PaginaActual = 1;
        _ = CargarProductosAsync();
    }

    [RelayCommand]
    private void Ordenar(ProductoCatalogoOrden orden)
    {
        if (OrdenActual == orden)
            OrdenDescendente = !OrdenDescendente;
        else
        {
            OrdenActual = orden;
            OrdenDescendente = false;
        }

        PaginaActual = 1;
        NotificarIndicadoresOrden();
        _ = CargarProductosAsync();
    }

    [RelayCommand]
    private void IrPagina(PaginaCatalogoItemViewModel? pagina)
    {
        if (pagina is null || pagina.EsSeparador || pagina.Numero == PaginaActual)
            return;

        PaginaActual = pagina.Numero;
        _ = CargarProductosAsync();
    }

    [RelayCommand]
    private void PaginaAnterior()
    {
        if (!PuedeIrPaginaAnterior)
            return;

        PaginaActual--;
        _ = CargarProductosAsync();
    }

    [RelayCommand]
    private void PaginaSiguiente()
    {
        if (!PuedeIrPaginaSiguiente)
            return;

        PaginaActual++;
        _ = CargarProductosAsync();
    }

    [RelayCommand]
    private async Task LimpiarFiltrosAsync()
    {
        _suppressReload = true;
        TextoBusqueda = string.Empty;
        KpiActivo = ProductoCatalogoKpi.Todos;
        EstadoSeleccionado = ProductoCatalogoEstado.Activos;
        PaginaActual = 1;
        _suppressReload = false;
        await CargarProductosAsync();
    }

    [RelayCommand]
    private async Task CambiarEstadoAsync(ProductoListadoItemViewModel? producto)
    {
        if (producto is null)
            return;

        var empresaId = ObtenerEmpresaId();
        if (empresaId <= 0)
        {
            MensajeEstado = "No existe una empresa activa en la sesión.";
            return;
        }

        var activar = !producto.Activo;
        var confirmar = await _messageDialogService.ConfirmAsync(
            activar ? "Activar producto" : "Inactivar producto",
            activar
                ? $"¿Desea activar {producto.Nombre}?"
                : $"¿Desea inactivar {producto.Nombre}? El producto no " +
                  "aparecerá en las operaciones normales mientras esté inactivo.",
            activar ? "Activar producto" : "Inactivar producto",
            "Cancelar",
            !activar);
        if (!confirmar)
            return;

        IsLoading = true;
        try
        {
            var result = await _productService.CambiarEstadoAsync(
                producto.Id,
                empresaId,
                activar ? (short)1 : (short)0);
            if (!result.Success)
            {
                await _messageDialogService.ShowErrorAsync(
                    "No se pudo cambiar el estado",
                    result.Message);
                return;
            }

            await CargarProductosAsync();
            await _notificationService.ShowSuccessAsync(result.Message);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            await _messageDialogService.ShowErrorAsync(
                "No se pudo cambiar el estado",
                "Ocurrió un problema al actualizar el producto.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void AlternarPresentaciones(ProductoListadoItemViewModel? producto)
    {
        if (producto is null || !producto.TienePresentacionesAdicionales)
            return;

        foreach (var item in Productos.Where(x => !ReferenceEquals(x, producto)))
            item.IsPresentacionesPopupOpen = false;
        producto.IsPresentacionesPopupOpen =
            !producto.IsPresentacionesPopupOpen;
    }

    private async Task BuscarConRetardoAsync()
    {
        _searchDebounceCancellationTokenSource?.Cancel();
        _searchDebounceCancellationTokenSource?.Dispose();
        _searchDebounceCancellationTokenSource = new CancellationTokenSource();
        var cancellationToken =
            _searchDebounceCancellationTokenSource.Token;

        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
            await CargarProductosAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task CargarProductosAsync(
        CancellationToken cancellationToken = default)
    {
        var empresaId = ObtenerEmpresaId();
        if (empresaId <= 0)
        {
            MensajeEstado = "No existe una empresa activa en la sesión.";
            return;
        }

        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _loadCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var loadToken = _loadCancellationTokenSource.Token;
        var sequence = Interlocked.Increment(ref _loadSequence);

        IsLoading = true;
        MensajeEstado = null;
        try
        {
            var resultado = await _productService.ObtenerCatalogoProductosAsync(
                new ProductoCatalogoQuery
                {
                    EmpresaId = empresaId,
                    Busqueda = TextoBusqueda,
                    Kpi = KpiActivo,
                    Estado = EstadoSeleccionado,
                    Orden = OrdenActual,
                    OrdenDescendente = OrdenDescendente,
                    Pagina = PaginaActual,
                    TamanoPagina = TamanoPagina
                },
                loadToken);

            if (sequence != Volatile.Read(ref _loadSequence))
                return;

            Productos.Clear();
            foreach (var producto in resultado.Items)
                Productos.Add(new ProductoListadoItemViewModel(producto));

            PaginaActual = resultado.Pagina;
            TotalItems = resultado.TotalItems;
            TotalPaginas = resultado.TotalPaginas;
            TotalProductos = resultado.Kpis.Productos;
            TotalStockBajo = resultado.Kpis.StockBajo;
            TotalSinStock = resultado.Kpis.SinStock;
            TotalPorCaducar = resultado.Kpis.PorCaducar;
            ConstruirPaginasVisibles();
            NotificarPaginacion();
            NotificarEstadoVacio();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            MensajeEstado = "No fue posible cargar los productos.";
        }
        finally
        {
            if (sequence == Volatile.Read(ref _loadSequence))
                IsLoading = false;
        }
    }

    private void ConstruirPaginasVisibles()
    {
        PaginasVisibles.Clear();
        if (TotalPaginas <= 0)
            return;

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
                PaginasVisibles.Add(PaginaCatalogoItemViewModel.Separador());
            PaginasVisibles.Add(new PaginaCatalogoItemViewModel(
                numero,
                numero == PaginaActual));
            anterior = numero;
        }
    }

    private void NotificarPaginacion()
    {
        OnPropertyChanged(nameof(PuedeIrPaginaAnterior));
        OnPropertyChanged(nameof(PuedeIrPaginaSiguiente));
        OnPropertyChanged(nameof(TextoPaginacion));
    }

    private void NotificarEstadoVacio()
    {
        OnPropertyChanged(nameof(HayResultados));
        OnPropertyChanged(nameof(MostrarEstadoVacio));
    }

    private string Indicador(ProductoCatalogoOrden orden) =>
        OrdenActual != orden ? string.Empty : OrdenDescendente ? "▼" : "▲";

    private void NotificarIndicadoresOrden()
    {
        OnPropertyChanged(nameof(IndicadorCodigo));
        OnPropertyChanged(nameof(IndicadorProducto));
        OnPropertyChanged(nameof(IndicadorCategoria));
        OnPropertyChanged(nameof(IndicadorUnidad));
        OnPropertyChanged(nameof(IndicadorStock));
        OnPropertyChanged(nameof(IndicadorCosto));
        OnPropertyChanged(nameof(IndicadorPrecio));
        OnPropertyChanged(nameof(IndicadorEstado));
    }

    [RelayCommand]
    private void NuevoProducto()
    {
        CodigoBarrasNuevoProducto = string.Empty;
        MensajeVerificacionBarcode = null;
        ProductoCodigoEncontrado = null;
        IsCheckingBarcode = false;
        IsNewProductCheckOpen = true;
    }

    [RelayCommand]
    private async Task EditarProductoAsync(
        ProductoListadoItemViewModel? producto)
    {
        producto ??= ProductoSeleccionado;
        if (producto is null)
            return;

        IsLoading = true;
        try
        {
            await using var loading = await _loadingService.ShowAsync(
                "Cargando producto",
                "Estamos preparando la información para editarla.");
            await ProductForm.EditarAsync(producto.Id);
            IsProductFormOpen = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnExistingProductRequested(long productoId)
    {
        IsProductFormOpen = false;
        await ProductForm.EditarAsync(productoId);
        IsProductFormOpen = true;
    }

    private void OnProductFormCloseRequested() => IsProductFormOpen = false;

    private async void OnProductSaved(long productoId)
    {
        IsProductFormOpen = false;
        await CargarProductosAsync();
        ProductoSeleccionado = Productos.FirstOrDefault(x => x.Id == productoId);
        await _notificationService.ShowSuccessAsync(
            "Producto guardado correctamente.");
    }

    [RelayCommand]
    private async Task AbrirProductoEncontradoAsync()
    {
        if (ProductoCodigoEncontrado is null)
            return;

        var productoId = ProductoCodigoEncontrado.ProductoId;
        IsNewProductCheckOpen = false;
        await ProductForm.EditarAsync(productoId);
        IsProductFormOpen = true;
    }

    [RelayCommand]
    private async Task VerificarCodigoBarrasAsync()
    {
        MensajeVerificacionBarcode = null;
        ProductoCodigoEncontrado = null;
        if (string.IsNullOrWhiteSpace(CodigoBarrasNuevoProducto))
        {
            MensajeVerificacionBarcode =
                "Ingresa o escanea el código de barras.";
            return;
        }

        IsCheckingBarcode = true;
        try
        {
            var resultado = await _productService.BuscarPorCodigoBarrasAsync(
                ObtenerEmpresaId(),
                CodigoBarrasNuevoProducto);
            if (resultado is not null)
            {
                ProductoCodigoEncontrado = resultado;
                MensajeVerificacionBarcode =
                    "Este código de barras ya está registrado.";
                return;
            }

            var codigo = CodigoBarrasNuevoProducto.Trim();
            IsNewProductCheckOpen = false;
            await ProductForm.NuevoAsync(codigo, false);
            IsProductFormOpen = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);
            MensajeVerificacionBarcode =
                "No fue posible verificar el código de barras.";
        }
        finally
        {
            IsCheckingBarcode = false;
        }
    }

    [RelayCommand]
    private async Task ContinuarSinCodigoBarrasAsync()
    {
        IsNewProductCheckOpen = false;
        await ProductForm.NuevoAsync(null, true);
        IsProductFormOpen = true;
    }

    [RelayCommand]
    private void CancelarVerificacionNuevoProducto()
    {
        IsNewProductCheckOpen = false;
        CodigoBarrasNuevoProducto = string.Empty;
        MensajeVerificacionBarcode = null;
        ProductoCodigoEncontrado = null;
    }

    private long ObtenerEmpresaId() => _currentSession.EmpresaId ?? 0;
}

public partial class ProductoListadoItemViewModel(
    ProductoListadoDto item) : ObservableObject
{
    public ProductoListadoDto Item { get; } = item;
    public long Id => Item.Id;
    public string Codigo => Item.Codigo;
    public string Nombre => Item.Nombre;
    public string NombreConMarca => Item.NombreConMarca;
    public string? Modelo => Item.Modelo;
    public string? Categoria => Item.Categoria;
    public string UnidadBase => Item.UnidadBase;
    public string TarifaImpuesto => Item.TarifaImpuesto;
    public decimal StockDisponible => Item.StockDisponible;
    public bool TieneStockBajo => Item.TieneStockBajo;
    public bool SinStock => Item.SinStock;
    public bool MostrarStockBajo => TieneStockBajo && !SinStock;
    public decimal CantidadPorCaducar => Item.CantidadPorCaducar;
    public bool PorCaducar => Item.PorCaducar;
    public bool MostrarPorCaducar => PorCaducar && !SinStock;
    public decimal CostoPromedio => Item.CostoPromedio;
    public decimal? PrecioBase => Item.PrecioBase;
    public string ListaPrecioBaseCodigo => Item.ListaPrecioBaseCodigo;
    public IReadOnlyList<ProductoPrecioBaseListaDto> PreciosBasePorLista =>
        Item.PreciosBasePorLista;
    public int CantidadPresentaciones => Item.CantidadPresentaciones;
    public IReadOnlyList<string> PresentacionesComerciales =>
        Item.PresentacionesComerciales;
    public IReadOnlyList<string> PresentacionesVisibles =>
        Item.PresentacionesVisibles;
    public string PresentacionesVisiblesTexto =>
        string.Join(" · ", Item.PresentacionesVisibles);
    public string PresentacionesCompletasTexto =>
        string.Join(" · ", Item.PresentacionesComerciales);
    public int CantidadPresentacionesAdicionales =>
        Item.CantidadPresentacionesAdicionales;
    public string TextoPresentacionesAdicionales =>
        $"+{CantidadPresentacionesAdicionales} MÁS";
    public string PresentacionesRestantesTexto =>
        string.Join(
            Environment.NewLine,
            Item.PresentacionesComerciales.Skip(3));
    public bool TienePresentacionesAdicionales =>
        Item.TienePresentacionesAdicionales;
    public short Estado => Item.Estado;
    public bool Activo => Item.Activo;
    public string EstadoTexto => Item.EstadoTexto;

    [ObservableProperty]
    private bool isPresentacionesPopupOpen;
}

public sealed record EstadoCatalogoItemViewModel(
    string Texto,
    ProductoCatalogoEstado Valor);

public sealed class PaginaCatalogoItemViewModel
{
    public int Numero { get; }
    public string Texto { get; }
    public bool EsActual { get; }
    public bool EsSeparador { get; }

    public PaginaCatalogoItemViewModel(int numero, bool esActual)
    {
        Numero = numero;
        Texto = numero.ToString();
        EsActual = esActual;
    }

    private PaginaCatalogoItemViewModel()
    {
        Texto = "…";
        EsSeparador = true;
    }

    public static PaginaCatalogoItemViewModel Separador() => new();
}
