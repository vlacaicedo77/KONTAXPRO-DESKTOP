using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models;
using KONTAXPRO.Application.Session;

namespace KONTAXPRO.Desktop.ViewModels;

public partial class SeleccionarEstablecimientoViewModel(
    IUsuarioEmpresaService usuarioEmpresaService,
    CurrentSession currentSession) : ObservableObject
{
    public event Action? EstablecimientoSeleccionado;

    public ObservableCollection<EstablecimientoDisponible> Establecimientos
        { get; } = [];

    [ObservableProperty]
    private EstablecimientoDisponible? establecimientoActual;

    [ObservableProperty]
    private string mensaje = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    public string Empresa => currentSession.RazonSocial ?? "Empresa activa";

    public async Task CargarAsync()
    {
        if (!currentSession.EmpresaId.HasValue) return;
        try
        {
            IsLoading = true;
            Mensaje = string.Empty;
            Establecimientos.Clear();
            var disponibles = await usuarioEmpresaService
                .ObtenerEstablecimientosUsuarioAsync(
                    currentSession.UsuarioId,
                    currentSession.EmpresaId.Value);
            foreach (var item in disponibles) Establecimientos.Add(item);
            currentSession.CantidadEstablecimientosDisponibles =
                Establecimientos.Count;
            EstablecimientoActual = Establecimientos.FirstOrDefault(x =>
                x.EstablecimientoId == currentSession.EstablecimientoId);
            if (Establecimientos.Count == 0)
                Mensaje = "No tienes establecimientos activos asignados.";
        }
        catch (Exception ex)
        {
            Mensaje = $"No fue posible cargar los establecimientos: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SeleccionarAsync()
    {
        if (EstablecimientoActual is null)
        {
            Mensaje = "Selecciona un establecimiento para continuar.";
            return;
        }
        try
        {
            IsLoading = true;
            Mensaje = string.Empty;
            await usuarioEmpresaService.SeleccionarEstablecimientoAsync(
                EstablecimientoActual.EstablecimientoId);
            EstablecimientoSeleccionado?.Invoke();
        }
        catch (Exception ex)
        {
            Mensaje = $"No fue posible cambiar el establecimiento: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
