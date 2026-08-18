using KONTAXPRO.Application.Models;

namespace KONTAXPRO.Application.Interfaces;

public interface IUsuarioEmpresaService
{
    Task<List<EmpresaDisponible>> ObtenerEmpresasUsuarioAsync(
        long usuarioId);

    Task SeleccionarEmpresaAsync(
        long usuarioEmpresaId);

    Task<List<EstablecimientoDisponible>>
        ObtenerEstablecimientosUsuarioAsync(
            long usuarioId,
            long empresaId);

    Task SeleccionarEstablecimientoAsync(
        long establecimientoId);
}
