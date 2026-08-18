using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
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
        var empresaAnteriorId = _currentSession.EmpresaId;
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
                x.Estado == 1 &&
                x.Empresa != null &&
                x.Empresa.Estado == 1);

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

        var establecimientos = await context
            .UsuariosEmpresasEstablecimientos
            .AsNoTracking()
            .Where(x =>
                x.UsuarioEmpresaId == usuarioEmpresa.Id &&
                x.Establecimiento != null &&
                x.Establecimiento.Estado == 1)
            .Select(x => x.Establecimiento!)
            .OrderByDescending(x => x.EsMatriz)
            .ThenBy(x => x.Codigo)
            .ToListAsync();
        _currentSession.CantidadEstablecimientosDisponibles =
            establecimientos.Count;

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
                        acceso.EstablecimientoId == x.EstablecimientoId) &&
                x.Establecimiento != null &&
                x.Establecimiento.Estado == 1);

        var establecimiento = configuracion?.Establecimiento ??
            establecimientos.FirstOrDefault();
        PuntoEmision? punto = null;
        Bodega? bodega = null;
        if (establecimiento is not null)
        {
            punto = configuracion?.PuntoEmision is { Estado: 1 } configuredPoint &&
                configuredPoint.EstablecimientoId == establecimiento.Id
                ? configuredPoint
                : await context.PuntosEmision.AsNoTracking()
                    .Where(x => x.EstablecimientoId == establecimiento.Id &&
                        x.Estado == 1)
                    .OrderBy(x => x.Codigo)
                    .FirstOrDefaultAsync();
            bodega = configuracion?.Bodega is { Estado: 1 } configuredWarehouse &&
                configuredWarehouse.EstablecimientoId == establecimiento.Id
                ? configuredWarehouse
                : await context.Bodegas.AsNoTracking()
                    .Where(x => x.EstablecimientoId == establecimiento.Id &&
                        x.Estado == 1)
                    .OrderBy(x => x.Codigo)
                    .FirstOrDefaultAsync();
        }

        AplicarContextoOperativo(establecimiento, punto, bodega);

        _currentSession.NotifyEmpresaActivaChanged(empresaAnteriorId);
    }

    public async Task<List<EstablecimientoDisponible>>
        ObtenerEstablecimientosUsuarioAsync(
            long usuarioId,
            long empresaId)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync();

        return await context.UsuariosEmpresasEstablecimientos
            .AsNoTracking()
            .Where(x =>
                x.UsuarioEmpresa != null &&
                x.UsuarioEmpresa.UsuarioId == usuarioId &&
                x.UsuarioEmpresa.EmpresaId == empresaId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.Establecimiento != null &&
                x.Establecimiento.EmpresaId == empresaId &&
                x.Establecimiento.Estado == 1)
            .OrderByDescending(x => x.Establecimiento!.EsMatriz)
            .ThenBy(x => x.Establecimiento!.Codigo)
            .Select(x => new EstablecimientoDisponible
            {
                EstablecimientoId = x.EstablecimientoId,
                Codigo = x.Establecimiento!.Codigo,
                Nombre = x.Establecimiento.Nombre,
                NombreComercial = x.Establecimiento.NombreComercial ??
                    x.Establecimiento.Nombre,
                Direccion = x.Establecimiento.Direccion,
                EsMatriz = x.Establecimiento.EsMatriz
            })
            .ToListAsync();
    }

    public async Task SeleccionarEstablecimientoAsync(
        long establecimientoId)
    {
        if (!_currentSession.EmpresaId.HasValue ||
            _currentSession.UsuarioId <= 0)
            throw new InvalidOperationException(
                "Seleccione una empresa antes de cambiar de establecimiento.");

        var empresaId = _currentSession.EmpresaId.Value;
        var anteriorId = _currentSession.EstablecimientoId;
        await using var context =
            await _dbContextFactory.CreateDbContextAsync();

        var acceso = await context.UsuariosEmpresasEstablecimientos
            .AsNoTracking()
            .Include(x => x.UsuarioEmpresa)
            .Include(x => x.Establecimiento)
            .SingleOrDefaultAsync(x =>
                x.EstablecimientoId == establecimientoId &&
                x.UsuarioEmpresa != null &&
                x.UsuarioEmpresa.UsuarioId == _currentSession.UsuarioId &&
                x.UsuarioEmpresa.EmpresaId == empresaId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.Establecimiento != null &&
                x.Establecimiento.EmpresaId == empresaId &&
                x.Establecimiento.Estado == 1)
            ?? throw new InvalidOperationException(
                "El establecimiento no está disponible para el usuario.");

        var configuracion = await context.UsuariosConfiguracionesEmpresa
            .SingleOrDefaultAsync(x =>
                x.UsuarioId == _currentSession.UsuarioId &&
                x.EmpresaId == empresaId);

        PuntoEmision? punto = null;
        if (configuracion?.EstablecimientoId == establecimientoId &&
            configuracion.PuntoEmisionId.HasValue)
            punto = await context.PuntosEmision.AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.Id == configuracion.PuntoEmisionId.Value &&
                    x.EstablecimientoId == establecimientoId &&
                    x.Estado == 1);
        punto ??= await context.PuntosEmision.AsNoTracking()
            .Where(x => x.EstablecimientoId == establecimientoId &&
                x.Estado == 1)
            .OrderBy(x => x.Codigo)
            .FirstOrDefaultAsync();

        Bodega? bodega = null;
        if (configuracion?.EstablecimientoId == establecimientoId &&
            configuracion.BodegaId.HasValue)
            bodega = await context.Bodegas.AsNoTracking()
                .SingleOrDefaultAsync(x =>
                    x.Id == configuracion.BodegaId.Value &&
                    x.EstablecimientoId == establecimientoId &&
                    x.Estado == 1);
        bodega ??= await context.Bodegas.AsNoTracking()
            .Where(x => x.EstablecimientoId == establecimientoId &&
                x.Estado == 1)
            .OrderBy(x => x.Codigo)
            .FirstOrDefaultAsync();

        var now = DateTime.UtcNow;
        if (configuracion is null)
        {
            configuracion = new UsuarioConfiguracionEmpresa
            {
                UsuarioId = _currentSession.UsuarioId,
                EmpresaId = empresaId,
                CreatedAt = now
            };
            context.UsuariosConfiguracionesEmpresa.Add(configuracion);
        }
        configuracion.EstablecimientoId = establecimientoId;
        configuracion.PuntoEmisionId = punto?.Id;
        configuracion.BodegaId = bodega?.Id;
        configuracion.UpdatedAt = now;
        await context.SaveChangesAsync();

        _currentSession.CantidadEstablecimientosDisponibles =
            await context.UsuariosEmpresasEstablecimientos.AsNoTracking()
                .CountAsync(x =>
                    x.UsuarioEmpresaId == acceso.UsuarioEmpresaId &&
                    x.Establecimiento != null &&
                    x.Establecimiento.Estado == 1);
        AplicarContextoOperativo(acceso.Establecimiento, punto, bodega);
        _currentSession.NotifyEstablecimientoActivoChanged(anteriorId);
    }

    private void AplicarContextoOperativo(
        Establecimiento? establecimiento,
        PuntoEmision? punto,
        Bodega? bodega)
    {
        _currentSession.EstablecimientoId = establecimiento?.Id;
        _currentSession.EstablecimientoCodigo = establecimiento?.Codigo;
        _currentSession.EstablecimientoNombre = establecimiento is null
            ? null
            : establecimiento.NombreComercial ?? establecimiento.Nombre;
        _currentSession.PuntoEmisionId = punto?.Id;
        _currentSession.PuntoEmisionCodigo = punto?.Codigo;
        _currentSession.PuntoEmisionNombre = punto?.Nombre;
        _currentSession.BodegaId = bodega?.Id;
        _currentSession.BodegaNombre = bodega?.Nombre;
        _currentSession.CajaId = null;
        _currentSession.CajaNombre = null;
        _currentSession.CajaSesionId = null;
    }
}
