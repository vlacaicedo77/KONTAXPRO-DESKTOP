using CommunityToolkit.Mvvm.ComponentModel;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.ViewModels.Products;
using KONTAXPRO.Desktop.ViewModels.Clientes;
using KONTAXPRO.Desktop.ViewModels.Proveedores;
using KONTAXPRO.Desktop.ViewModels.Compras;
using KONTAXPRO.Desktop.ViewModels.Tesoreria;

namespace KONTAXPRO.Desktop.Services
{
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;

        public ObservableObject? CurrentViewModel { get; private set; }

        public event Action? CurrentViewModelChanged;

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void NavigateTo(string route)
        {
            var nextViewModel = route switch
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

                "Compras" => _serviceProvider.GetService(
                                  typeof(ComprasViewModel))
                              as ObservableObject,

                "OperacionesSinComprobante" => _serviceProvider.GetService(
                                  typeof(OperacionesSinSustentoViewModel))
                              as ObservableObject,

                _ => CurrentViewModel
            };

            if (!ReferenceEquals(CurrentViewModel, nextViewModel) &&
                CurrentViewModel is IDisposable disposable)
            {
                disposable.Dispose();
            }

            CurrentViewModel = nextViewModel;

            CurrentViewModelChanged?.Invoke();
        }
    }
}
