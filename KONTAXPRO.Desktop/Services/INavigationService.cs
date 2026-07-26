using CommunityToolkit.Mvvm.ComponentModel;

namespace KONTAXPRO.Desktop.Services
{
    public interface INavigationService
    {
        ObservableObject? CurrentViewModel { get; }

        event Action? CurrentViewModelChanged;

        void NavigateTo(string route);
    }
}