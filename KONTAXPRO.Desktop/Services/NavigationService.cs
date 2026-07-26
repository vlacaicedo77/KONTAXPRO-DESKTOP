using CommunityToolkit.Mvvm.ComponentModel;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.ViewModels.Products;

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
            CurrentViewModel = route switch
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

                _ => CurrentViewModel
            };

            CurrentViewModelChanged?.Invoke();
        }
    }
}