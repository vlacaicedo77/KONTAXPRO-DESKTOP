using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Desktop.Views;
using System.Diagnostics;
using System.Threading;
using System.Windows;

namespace KONTAXPRO.Desktop.Services;

public sealed class LoadingService : ILoadingService
{
    private static readonly TimeSpan MinimumDisplayDuration =
        TimeSpan.FromMilliseconds(350);

    private LoadingWindow? _window;
    private Window? _owner;
    private bool _ownerWasEnabled;
    private int _activeScopes;
    private long _openedAt;

    public Task<ILoadingScope> ShowAsync(string title, string message)
    {
        var application = System.Windows.Application.Current;
        if (application is null)
            return Task.FromResult<ILoadingScope>(EmptyLoadingScope.Instance);

        return application.Dispatcher.CheckAccess()
            ? Task.FromResult(ShowCore(title, message))
            : application.Dispatcher.InvokeAsync(
                () => ShowCore(title, message)).Task;
    }

    private ILoadingScope ShowCore(string title, string message)
    {
        _activeScopes++;

        if (_window is null)
        {
            try
            {
                _owner = WindowOwnerResolver.Resolve();
                _ownerWasEnabled = _owner?.IsEnabled ?? false;
                _window = new LoadingWindow(title, message);
                _window.AttachTo(_owner);
                _window.Show();

                if (_owner is not null)
                    _owner.IsEnabled = false;

                _openedAt = Stopwatch.GetTimestamp();
            }
            catch
            {
                _activeScopes--;
                _window = null;
                _owner = null;
                _ownerWasEnabled = false;
                throw;
            }
        }

        return new LoadingScope(this);
    }

    private async ValueTask ReleaseAsync()
    {
        var application = System.Windows.Application.Current;
        if (application is null)
            return;

        var shouldClose = application.Dispatcher.CheckAccess()
            ? ReleaseScopeCore()
            : await application.Dispatcher.InvokeAsync(ReleaseScopeCore);

        if (!shouldClose)
            return;

        var elapsed = Stopwatch.GetElapsedTime(_openedAt);
        var remaining = MinimumDisplayDuration - elapsed;
        if (remaining > TimeSpan.Zero)
            await Task.Delay(remaining);

        if (application.Dispatcher.CheckAccess())
            CloseCore();
        else
            await application.Dispatcher.InvokeAsync(CloseCore);
    }

    private bool ReleaseScopeCore()
    {
        if (_activeScopes > 0)
            _activeScopes--;

        return _activeScopes == 0 && _window is not null;
    }

    private void CloseCore()
    {
        if (_activeScopes != 0 || _window is null)
            return;

        _window.CloseFromService();
        _window = null;

        if (_owner is not null)
            _owner.IsEnabled = _ownerWasEnabled;

        _owner = null;
        _ownerWasEnabled = false;
    }

    private sealed class LoadingScope(LoadingService owner) : ILoadingScope
    {
        private int _disposed;

        public ValueTask DisposeAsync() =>
            Interlocked.Exchange(ref _disposed, 1) == 0
                ? owner.ReleaseAsync()
                : ValueTask.CompletedTask;
    }

    private sealed class EmptyLoadingScope : ILoadingScope
    {
        public static EmptyLoadingScope Instance { get; } = new();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
