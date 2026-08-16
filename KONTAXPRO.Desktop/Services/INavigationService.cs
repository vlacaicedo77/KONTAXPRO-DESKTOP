using CommunityToolkit.Mvvm.ComponentModel;

namespace KONTAXPRO.Desktop.Services
{
    public interface INavigationService
    {
        ObservableObject? CurrentViewModel { get; }

        string? CurrentRoute { get; }

        event Action? CurrentViewModelChanged;

        void NavigateTo(string route);

        Task<bool> NavigateToAsync(
            string route,
            CancellationToken cancellationToken = default);
    }

    public interface IAsyncNavigationTarget
    {
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }

    public interface IRouteNavigationTarget
    {
        Task NavigateToRouteAsync(
            string route,
            CancellationToken cancellationToken = default);
    }
}
