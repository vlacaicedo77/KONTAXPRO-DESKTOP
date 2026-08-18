using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Services;
using KONTAXPRO.Desktop.ViewModels;
using KONTAXPRO.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;

namespace KONTAXPRO.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly CurrentSession _currentSession;
        private readonly SessionFlowService _sessionFlowService;

        public MainWindow(
            MainViewModel viewModel,
            IServiceProvider serviceProvider,
            CurrentSession currentSession,
            SessionFlowService sessionFlowService)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _serviceProvider = serviceProvider;
            _currentSession = currentSession;
            _sessionFlowService = sessionFlowService;

            DataContext = _viewModel;

            _viewModel.CambioEmpresaRequested +=
                OnCambioEmpresaRequested;

            _viewModel.CambioEstablecimientoRequested +=
                OnCambioEstablecimientoRequested;

            _viewModel.CerrarSesionRequested +=
                OnCerrarSesionRequested;
        }

        private async void OnCambioEmpresaRequested()
        {
            var seleccionarEmpresaWindow =
                _serviceProvider.GetRequiredService<
                    SeleccionarEmpresaWindow>();

            seleccionarEmpresaWindow.Owner = this;

            var resultado =
                seleccionarEmpresaWindow.ShowDialog();

            if (resultado == true)
            {
                await _viewModel.RestablecerNavegacionAsync();
            }
        }

        private async void OnCambioEstablecimientoRequested()
        {
            var window = _serviceProvider.GetRequiredService<
                SeleccionarEstablecimientoWindow>();
            window.Owner = this;
            if (window.ShowDialog() == true)
                await _viewModel.RestablecerNavegacionAsync();
        }

        private void OnCerrarSesionRequested()
        {
            var nombreUsuario =
                string.IsNullOrWhiteSpace(_currentSession.NombreCompleto)
                    ? "Usuario actual"
                    : _currentSession.NombreCompleto;

            var empresa =
                string.IsNullOrWhiteSpace(_currentSession.RazonSocial)
                    ? "Empresa activa"
                    : _currentSession.RazonSocial;

            var dialogo = new ConfirmacionWindow(
                nombreUsuario,
                empresa)
            {
                Owner = this
            };

            var confirmacion = dialogo.ShowDialog();

            if (confirmacion != true)
            {
                return;
            }

            IniciarNuevoFlujoDeSesion();
        }

        private async void IniciarNuevoFlujoDeSesion()
        {
            System.Windows.Application.Current.ShutdownMode =
                ShutdownMode.OnExplicitShutdown;

            Hide();

            _currentSession.Clear();

            var accesoCorrecto =
                await _sessionFlowService.IniciarSesionAsync();

            if (!accesoCorrecto)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }

            _viewModel.ActualizarContexto();

            System.Windows.Application.Current.MainWindow =
                this;

            System.Windows.Application.Current.ShutdownMode =
                ShutdownMode.OnMainWindowClose;

            Show();

            WindowState =
                WindowState.Maximized;

            Activate();
            Focus();
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.CambioEmpresaRequested -=
                OnCambioEmpresaRequested;

            _viewModel.CambioEstablecimientoRequested -=
                OnCambioEstablecimientoRequested;

            _viewModel.CerrarSesionRequested -=
                OnCerrarSesionRequested;

            base.OnClosed(e);
        }
    }
}
