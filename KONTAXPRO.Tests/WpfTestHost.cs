using System.Threading;
using System.Windows;
using System.Windows.Threading;
using KONTAXPRO.Desktop;

namespace KONTAXPRO.Tests;

internal static class WpfTestHost
{
    private static readonly ManualResetEventSlim Ready = new(false);
    private static readonly object Sync = new();
    private static Thread? _uiThread;
    private static Dispatcher? _dispatcher;

    private static void EnsureStarted()
    {
        if (_dispatcher is not null) return;
        lock (Sync)
        {
            if (_dispatcher is not null) return;
            _uiThread = new Thread(() =>
            {
                var app = System.Windows.Application.Current as App ??
                    new App();
                app.InitializeComponent();
                _dispatcher = Dispatcher.CurrentDispatcher;
                Ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "KONTAXPRO WPF Test Host"
            };
            _uiThread.SetApartmentState(ApartmentState.STA);
            _uiThread.Start();
        }
        Ready.Wait();
    }

    internal static Exception? Run(Action action)
    {
        EnsureStarted();
        try
        {
            _dispatcher!.Invoke(action);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
