using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Security;

public class UsuarioEmpresaService : IUsuarioEmpresaService
{
    private readonly IDbContextFactory<KontaxDbContext> _dbContextFactory;
    private readonly CurrentSession _currentSession;

    public UsuarioEmpresaService(
        IDbContextFactory<KontaxDbContext> dbContextFactory,
        CurrentSession currentSession)
    {
        _dbContextFactory = dbContextFactory;
        _currentSession = currentSession;
    }

    public async Task<List<EmpresaDisponible>> ObtenerEmpresasUsuarioAsync(
        long usuarioId)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync();

        return await context.UsuariosEmpresas
            .AsNoTracking()
            .Where(x =>
                x.UsuarioId == usuarioId &&
                x.Estado == 1 &&
                x.Empresa != null &&
                x.Empresa.Estado == 1)
            .OrderBy(x => x.Empresa!.RazonSocial)
            .Select(x => new EmpresaDisponible
            {
                UsuarioEmpresaId = x.Id,
                EmpresaId = x.EmpresaId,
                NumeroIdentificacion =
                    x.Empresa!.NumeroIdentificacion,
                RazonSocial =
                    x.Empresa.RazonSocial,
                NombreComercial =
                x.Empresa.NombreComercial ?? x.Empresa.RazonSocial
            })
            .ToListAsync();
    }

    public async Task SeleccionarEmpresaAsync(
    long usuarioEmpresaId)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync();

        var usuarioEmpresa = await context.UsuariosEmpresas
            .AsNoTracking()
            .Include(x => x.Empresa)
            .Include(x => x.UsuariosEmpresasRoles)
                .ThenInclude(x => x.Rol)
                    .ThenInclude(x => x!.RolesPermisos)
                        .ThenInclude(x => x.Permiso)
            .FirstOrDefaultAsync(x =>
                x.Id == usuarioEmpresaId &&
                x.UsuarioId == _currentSession.UsuarioId &&
                x.Estado == 1);

        if (usuarioEmpresa is null)
        {
            throw new InvalidOperationException(
                "La empresa seleccionada no está disponible para el usuario.");
        }

        // Empresa activa
        _currentSession.EmpresaId =
            usuarioEmpresa.EmpresaId;

        _currentSession.RazonSocial =
            usuarioEmpresa.Empresa?.NombreComercial
            ?? usuarioEmpresa.Empresa?.RazonSocial;

        // Roles
        _currentSession.Roles =
            usuarioEmpresa.UsuariosEmpresasRoles
                .Where(x =>
                    x.Rol != null &&
                    x.Rol.Estado == 1)
                .Select(x => x.Rol!.Codigo)
                .Distinct()
                .ToList();

        // Permisos efectivos
        _currentSession.Permisos =
            usuarioEmpresa.UsuariosEmpresasRoles
                .Where(x =>
                    x.Rol != null &&
                    x.Rol.Estado == 1)
                .SelectMany(x =>
                    x.Rol!.RolesPermisos)
                .Where(x =>
                    x.Permiso != null &&
                    x.Permiso.Estado == 1)
                .Select(x =>
                    x.Permiso!.Codigo)
                .Distinct()
                .ToList();

        // Limpiar contexto operativo anterior
        _currentSession.EstablecimientoId = null;
        _currentSession.EstablecimientoCodigo = null;
        _currentSession.EstablecimientoNombre = null;

        _currentSession.PuntoEmisionId = null;
        _currentSession.PuntoEmisionCodigo = null;
        _currentSession.PuntoEmisionNombre = null;
        _currentSession.BodegaId = null;
        _currentSession.BodegaNombre = null;

        // Cargar configuración predeterminada del usuario para la empresa
        var configuracion = await context.UsuariosConfiguracionesEmpresa
            .AsNoTracking()
            .Include(x => x.Establecimiento)
            .Include(x => x.PuntoEmision)
            .Include(x => x.Bodega)
            .FirstOrDefaultAsync(x =>
                x.UsuarioId == _currentSession.UsuarioId &&
                x.EmpresaId == usuarioEmpresa.EmpresaId &&
                context.UsuariosEmpresasEstablecimientos.Any(
                    acceso =>
                        acceso.UsuarioEmpresaId == usuarioEmpresa.Id &&
                        acceso.EstablecimientoId == x.EstablecimientoId));

        if (configuracion?.Establecimiento is not null)
        {
            var establecimiento = configuracion.Establecimiento;

            _currentSession.EstablecimientoId =
                establecimiento.Id;

            _currentSession.EstablecimientoCodigo =
                establecimiento.Codigo;

            _currentSession.EstablecimientoNombre =
                establecimiento.Nombre;

            if (configuracion.PuntoEmision is not null)
            {
                var puntoEmision = configuracion.PuntoEmision;

                _currentSession.PuntoEmisionId =
                    puntoEmision.Id;

                _currentSession.PuntoEmisionCodigo =
                    puntoEmision.Codigo;

                _currentSession.PuntoEmisionNombre =
                    puntoEmision.Nombre;
            }

            if (configuracion.Bodega is not null)
            {
                _currentSession.BodegaId = configuracion.Bodega.Id;
                _currentSession.BodegaNombre = configuracion.Bodega.Nombre;
            }
        }
    }
}
