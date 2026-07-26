using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace KONTAXPRO.Desktop.Services;

public class SessionFlowService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUsuarioEmpresaService _usuarioEmpresaService;
    private readonly CurrentSession _currentSession;

    public SessionFlowService(
        IServiceProvider serviceProvider,
        IUsuarioEmpresaService usuarioEmpresaService,
        CurrentSession currentSession)
    {
        _serviceProvider = serviceProvider;
        _usuarioEmpresaService = usuarioEmpresaService;
        _currentSession = currentSession;
    }

    public async Task<bool> IniciarSesionAsync(
        Window? owner = null)
    {
        // Mostrar login
        var loginWindow =
            _serviceProvider.GetRequiredService<LoginWindow>();

        if (owner != null)
        {
            loginWindow.Owner = owner;
        }

        var loginResultado = loginWindow.ShowDialog();

        if (loginResultado != true)
        {
            return false;
        }

        // Consultar empresas del usuario autenticado
        var empresas =
            await _usuarioEmpresaService.ObtenerEmpresasUsuarioAsync(
                _currentSession.UsuarioId);

        _currentSession.CantidadEmpresasDisponibles =
            empresas.Count;

        if (empresas.Count == 0)
        {
            MessageBox.Show(
                "Tu cuenta no tiene empresas habilitadas para trabajar.\n\n" +
                "Solicita al administrador que revise la configuración de acceso.",
                "Acceso empresarial no disponible",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            _currentSession.Clear();
            return false;
        }

        // Si solamente tiene una empresa, seleccionarla automáticamente
        if (empresas.Count == 1)
        {
            await _usuarioEmpresaService.SeleccionarEmpresaAsync(
                empresas[0].UsuarioEmpresaId);

            return true;
        }

        // Si tiene varias empresas, mostrar ventana de selección
        var seleccionarEmpresaWindow =
            _serviceProvider.GetRequiredService<
                SeleccionarEmpresaWindow>();

        if (owner != null)
        {
            seleccionarEmpresaWindow.Owner = owner;
        }

        var empresaResultado =
            seleccionarEmpresaWindow.ShowDialog();

        return empresaResultado == true;
    }
}