using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models;
using KONTAXPRO.Application.Session;
using System.Collections.ObjectModel;

namespace KONTAXPRO.Desktop.ViewModels;

public partial class SeleccionarEmpresaViewModel : ObservableObject
{
    private readonly IUsuarioEmpresaService _usuarioEmpresaService;
    private readonly CurrentSession _currentSession;

    public event Action? EmpresaSeleccionada;

    public ObservableCollection<EmpresaDisponible> Empresas { get; }
        = new();

    [ObservableProperty]
    private EmpresaDisponible? empresaActual;

    [ObservableProperty]
    private string mensaje = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public SeleccionarEmpresaViewModel(
        IUsuarioEmpresaService usuarioEmpresaService,
        CurrentSession currentSession)
    {
        _usuarioEmpresaService = usuarioEmpresaService;
        _currentSession = currentSession;
    }

    public async Task CargarEmpresasAsync()
    {
        try
        {
            IsLoading = true;
            Mensaje = string.Empty;

            Empresas.Clear();

            var empresas =
                await _usuarioEmpresaService.ObtenerEmpresasUsuarioAsync(
                    _currentSession.UsuarioId);

            foreach (var empresa in empresas)
            {
                Empresas.Add(empresa);
            }

            _currentSession.CantidadEmpresasDisponibles =
                Empresas.Count;

            if (Empresas.Count == 0)
            {
                Mensaje =
                    "El usuario no tiene empresas habilitadas.";
                return;
            }

            EmpresaActual = null;
        }
        catch (Exception ex)
        {
            Mensaje =
                $"Error al cargar las empresas: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeleccionarEmpresaAsync()
    {
        if (EmpresaActual is null)
        {
            Mensaje =
                "Debe seleccionar una empresa.";
            return;
        }

        try
        {
            IsLoading = true;
            Mensaje = string.Empty;

            await _usuarioEmpresaService.SeleccionarEmpresaAsync(
                EmpresaActual.UsuarioEmpresaId);

            EmpresaSeleccionada?.Invoke();
        }
        catch (Exception ex)
        {
            Mensaje =
                $"No fue posible seleccionar la empresa: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}