using CommunityToolkit.Mvvm.ComponentModel;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.ViewModels.Clientes;
using KONTAXPRO.Desktop.ViewModels.Proveedores;
using KONTAXPRO.Desktop.ViewModels.Compras;
using KONTAXPRO.Desktop.ViewModels.Tesoreria;
using KONTAXPRO.Desktop.ViewModels.Inventory;

namespace KONTAXPRO.Desktop.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SemaphoreSlim _navigationGate = new(1, 1);

        public ObservableObject? CurrentViewModel { get; private set; }

        public string? CurrentRoute { get; private set; }

        public event Action? CurrentViewModelChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo(string route)
        {
            if (string.Equals(CurrentRoute, route,
                    StringComparison.OrdinalIgnoreCase))
                return;

            var nextViewModel = Resolve(route);
            if (nextViewModel is null)
                return;

            Activate(route, nextViewModel);
        }

        public async Task<bool> NavigateToAsync(
            string route,
            CancellationToken cancellationToken = default)
        {
            if (string.Equals(CurrentRoute, route,
                    StringComparison.OrdinalIgnoreCase))
                return false;

            if (!await _navigationGate.WaitAsync(0, cancellationToken))
                return false;

            try
            {
                if (string.Equals(CurrentRoute, route,
                        StringComparison.OrdinalIgnoreCase))
                    return false;

                if (CurrentViewModel is IRouteNavigationTarget currentTarget &&
                    IsSameModule(CurrentRoute, route))
                {
                    await currentTarget.NavigateToRouteAsync(
                        route, cancellationToken);
                    CurrentRoute = route;
                    return true;
                }

                var nextViewModel = Resolve(route);
                if (nextViewModel is null)
                    return false;

                Activate(route, nextViewModel);
                try
                {
                    if (nextViewModel is IAsyncNavigationTarget asyncTarget)
                        await asyncTarget.InitializeAsync(cancellationToken);
                    if (nextViewModel is IRouteNavigationTarget routeTarget)
                        await routeTarget.NavigateToRouteAsync(
                            route, cancellationToken);
                }
                catch
                {
                    // Permite reintentar la misma opción si su apertura falló.
                    CurrentRoute = null;
                    throw;
                }

                return true;
            }
            finally
            {
                _navigationGate.Release();
            }
        }

        private ObservableObject? Resolve(string route) =>
            route switch
            {
                "Inicio" => _serviceProvider.GetService(
                                typeof(DashboardViewModel))
                            as ObservableObject,

                "Ventas" => _serviceProvider.GetService(
                                typeof(VentasViewModel))
                            as ObservableObject,

                "Productos" => _serviceProvider.GetService(
                                   typeof(ProductsViewModel))
                               as ObservableObject,

                "Clientes" => _serviceProvider.GetService(
                                   typeof(ClientesViewModel))
                               as ObservableObject,

                "Proveedores" => _serviceProvider.GetService(
                                      typeof(ProveedoresViewModel))
                                  as ObservableObject,

                _ when route.StartsWith("Compras",
                    StringComparison.OrdinalIgnoreCase) =>
                              _serviceProvider.GetService(
                                  typeof(ComprasViewModel))
                              as ObservableObject,

                "OperacionesSinComprobante" => _serviceProvider.GetService(
                                  typeof(OperacionesSinSustentoViewModel))
                              as ObservableObject,

                "Inventario" => _serviceProvider.GetService(
                                  typeof(InventoryViewModel))
                              as ObservableObject,

                _ => null
            };

        private static bool IsSameModule(string? currentRoute, string nextRoute) =>
            currentRoute is not null &&
            currentRoute.StartsWith("Compras",
                StringComparison.OrdinalIgnoreCase) &&
            nextRoute.StartsWith("Compras",
                StringComparison.OrdinalIgnoreCase);

        private void Activate(string route, ObservableObject nextViewModel)
        {
            if (!ReferenceEquals(CurrentViewModel, nextViewModel) &&
                CurrentViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            CurrentViewModel = nextViewModel;
            CurrentRoute = route;

            CurrentViewModelChanged?.Invoke();
        }
    }
}
