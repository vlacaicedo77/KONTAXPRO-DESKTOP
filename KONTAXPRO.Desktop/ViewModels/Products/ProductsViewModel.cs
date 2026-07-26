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

    private CancellationTokenSource? _searchCancellationTokenSource;

    public ProductFormViewModel ProductForm { get; }
    
    [ObservableProperty]
    private bool isProductFormOpen;

    public ObservableCollection<ProductoListadoDto> Productos { get; }
        = new();

    public bool HayProductoCodigoEncontrado =>
    ProductoCodigoEncontrado is not null;

    [ObservableProperty]
    private ProductoListadoDto? productoSeleccionado;

    [ObservableProperty]
    private string textoBusqueda = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool mostrarSoloActivos = true;

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
    private string codigoBarrasNuevoProducto =
        string.Empty;

    [ObservableProperty]
    private bool isCheckingBarcode;

    [ObservableProperty]
    private string? mensajeVerificacionBarcode;

    [ObservableProperty]
    private ProductoCodigoBarrasDto?
        productoCodigoEncontrado;

    partial void OnProductoCodigoEncontradoChanged(
    ProductoCodigoBarrasDto? value)
    {
        OnPropertyChanged(
            nameof(HayProductoCodigoEncontrado));
    }

    partial void OnCodigoBarrasNuevoProductoChanged(
    string value)
    {
        ProductoCodigoEncontrado =
            null;

        MensajeVerificacionBarcode =
            null;
    }

    public ProductsViewModel(
    IProductService productService,
    CurrentSession currentSession,
    ProductFormViewModel productForm)
    {
        _productService = productService;
        _currentSession = currentSession;

        ProductForm = productForm;

        ProductForm.CloseRequested +=
            OnProductFormCloseRequested;

        ProductForm.ProductSaved +=
            OnProductSaved;

        ProductForm.ExistingProductRequested +=
            OnExistingProductRequested;

    }

    private async void OnExistingProductRequested(
    long productoId)
    {
        IsProductFormOpen =
            false;

        await ProductForm.EditarAsync(
            productoId);

        IsProductFormOpen =
            true;
    }

    public async Task InitializeAsync()
    {
        await CargarProductosAsync();
    }

    [RelayCommand]
    private async Task RecargarAsync()
    {
        await CargarProductosAsync();
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        _ = BuscarConRetardoAsync();
    }

    partial void OnMostrarSoloActivosChanged(bool value)
    {
        _ = CargarProductosAsync();
    }

    [RelayCommand]
    private async Task LimpiarBusquedaAsync()
    {
        TextoBusqueda = string.Empty;
        await CargarProductosAsync();
    }

    [RelayCommand]
    private async Task CambiarEstadoAsync(ProductoListadoDto? producto)
    {
        if (producto is null)
        {
            return;
        }

        var empresaId = ObtenerEmpresaId();

        if (empresaId <= 0)
        {
            MensajeEstado =
                "No existe una empresa activa en la sesión.";
            return;
        }

        var nuevoEstado = producto.Estado == 1
            ? (short)0
            : (short)1;

        IsLoading = true;

        try
        {
            var result = await _productService.CambiarEstadoAsync(
                producto.Id,
                empresaId,
                nuevoEstado);

            MensajeEstado = result.Message;

            if (result.Success)
            {
                await CargarProductosAsync();
            }
        }
        catch
        {
            MensajeEstado =
                "Ocurrió un error al cambiar el estado del producto.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuscarConRetardoAsync()
    {
        _searchCancellationTokenSource?.Cancel();
        _searchCancellationTokenSource?.Dispose();

        _searchCancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            _searchCancellationTokenSource.Token;

        try
        {
            await Task.Delay(
                TimeSpan.FromMilliseconds(300),
                cancellationToken);

            await CargarProductosAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Se escribió otro carácter antes de terminar el retardo.
        }
    }

    private async Task CargarProductosAsync(
        CancellationToken cancellationToken = default)
    {
        var empresaId = ObtenerEmpresaId();

        if (empresaId <= 0)
        {
            MensajeEstado =
                "No existe una empresa activa en la sesión.";
            return;
        }

        IsLoading = true;
        MensajeEstado = null;

        try
        {
            short? estado = MostrarSoloActivos
                ? (short)1
                : null;

            var productos =
                await _productService.ObtenerProductosAsync(
                    empresaId,
                    TextoBusqueda,
                    null,
                    estado,
                    cancellationToken);

            Productos.Clear();

            foreach (var producto in productos)
            {
                Productos.Add(producto);
            }

            ActualizarIndicadores();
        }
        catch (OperationCanceledException)
        {
            // Una búsqueda anterior fue cancelada.
        }
        catch
        {
            MensajeEstado =
                "No fue posible cargar los productos.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ActualizarIndicadores()
    {
        TotalProductos =
            Productos.Count;

        TotalSinStock =
            Productos.Count(x => x.SinStock);

        TotalStockBajo =
            Productos.Count(x => x.TieneStockBajo);

        TotalPorCaducar =
            Productos.Count(x => x.PorCaducar);
    }

    [RelayCommand]
    private void NuevoProducto()
    {
        CodigoBarrasNuevoProducto =
            string.Empty;

        MensajeVerificacionBarcode =
            null;

        ProductoCodigoEncontrado =
            null;

        IsCheckingBarcode =
            false;

        IsNewProductCheckOpen =
            true;
    }

    [RelayCommand]
    private async Task EditarProductoAsync(
    ProductoListadoDto? producto)
    {
        if (producto is null)
        {
            producto = ProductoSeleccionado;
        }

        if (producto is null)
        {
            return;
        }

        await ProductForm.EditarAsync(producto.Id);

        IsProductFormOpen = true;
    }

    private void OnProductFormCloseRequested()
    {
        IsProductFormOpen = false;
    }

    private async void OnProductSaved(long productoId)
    {
        IsProductFormOpen = false;

        MensajeEstado =
            "Producto guardado correctamente.";

        await CargarProductosAsync();

        ProductoSeleccionado =
            Productos.FirstOrDefault(
                x => x.Id == productoId);
    }

    [RelayCommand]
    private async Task AbrirProductoEncontradoAsync()
    {
        if (ProductoCodigoEncontrado is null)
        {
            return;
        }

        var productoId =
            ProductoCodigoEncontrado.ProductoId;

        IsNewProductCheckOpen =
            false;

        await ProductForm.EditarAsync(
            productoId);

        IsProductFormOpen =
            true;
    }

    [RelayCommand]
    private async Task VerificarCodigoBarrasAsync()
    {
        MensajeVerificacionBarcode = null;
        ProductoCodigoEncontrado = null;

        if (string.IsNullOrWhiteSpace(
            CodigoBarrasNuevoProducto))
        {
            MensajeVerificacionBarcode =
                "Ingresa o escanea el código de barras.";
            return;
        }

        IsCheckingBarcode = true;

        try
        {
            var resultado =
                await _productService
                    .BuscarPorCodigoBarrasAsync(
                        CodigoBarrasNuevoProducto);

            if (resultado != null)
            {
                ProductoCodigoEncontrado =
                    resultado;

                MensajeVerificacionBarcode =
                    "Este código de barras ya está registrado.";

                return;
            }

            var codigo =
                CodigoBarrasNuevoProducto.Trim();

            IsNewProductCheckOpen =
                false;

            await ProductForm.NuevoAsync(
                codigo,
                false);

            IsProductFormOpen =
                true;
        }
        catch (Exception ex)
        {
            MensajeVerificacionBarcode =
                $"No fue posible verificar el código de barras: {ex.Message}";
        }
        finally
        {
            IsCheckingBarcode = false;
        }
    }

    [RelayCommand]
    private async Task ContinuarSinCodigoBarrasAsync()
    {
        IsNewProductCheckOpen =
            false;

        await ProductForm.NuevoAsync(
            null,
            true);

        IsProductFormOpen =
            true;
    }

    [RelayCommand]
    private void CancelarVerificacionNuevoProducto()
    {
        IsNewProductCheckOpen =
            false;

        CodigoBarrasNuevoProducto =
            string.Empty;

        MensajeVerificacionBarcode =
            null;

        ProductoCodigoEncontrado =
            null;
    }

    private long ObtenerEmpresaId()
    {
        return _currentSession.EmpresaId ?? 0;
    }
}