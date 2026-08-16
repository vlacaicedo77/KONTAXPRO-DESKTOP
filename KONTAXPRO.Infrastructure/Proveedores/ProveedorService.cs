using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Proveedores;

public sealed class ProveedorService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    CurrentSession currentSession,
    IConstanciaVerificacionIdentificacionStore proofStore,
    ILogger<ProveedorService> logger) : IProveedorService
{
    private const string RucCode = IdentificacionEcuadorValidator.RucCodigo;
    private const string IdentificationUniqueConstraint =
        "ux_terceros_identificaciones_tipo_numero";

    public async Task<ProveedorCatalogoResultadoDto> ObtenerProveedoresAsync(
        ProveedorCatalogoQuery request,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(request.Pagina, 1);
        var pageSize = request.TamanoPagina is 25 or 50 or 100
            ? request.TamanoPagina
            : 25;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureActiveCompanyAccessAsync(context, cancellationToken);
        var rucTypeId = await GetRucTypeIdAsync(context, cancellationToken);
        var query = BuildSupplierQuery(context, rucTypeId);

        var text = string.IsNullOrWhiteSpace(request.Busqueda)
            ? null
            : request.Busqueda.Trim();
        if (text is not null)
        {
            var pattern = $"%{text}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Ruc, pattern) ||
                EF.Functions.ILike(x.RazonSocial, pattern) ||
                (x.Correo != null && EF.Functions.ILike(x.Correo, pattern)) ||
                (x.Telefono != null && EF.Functions.ILike(x.Telefono, pattern)));
        }

        var kpis = await query.GroupBy(_ => 1)
            .Select(group => new ProveedorCatalogoKpisDto
            {
                Activos = group.Count(x => x.Estado == 1),
                PendientesVerificar = group.Count(x => !x.Verificado),
                SinCorreo = group.Count(x =>
                    x.Correo == null ||
                    x.Correo == string.Empty ||
                    x.Correo.ToLower() == ContactoClienteNormalizer.DefaultEmail),
                SinContactoDigital = group.Count(x =>
                    (x.Correo == null ||
                     x.Correo == string.Empty ||
                     x.Correo.ToLower() == ContactoClienteNormalizer.DefaultEmail) &&
                    (x.Telefono == null || x.Telefono == string.Empty))
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ProveedorCatalogoKpisDto();

        query = request.Estado switch
        {
            ProveedorEstadoFiltro.Activos => query.Where(x => x.Estado == 1),
            ProveedorEstadoFiltro.Inactivos => query.Where(x => x.Estado == 0),
            _ => query
        };
        query = request.Verificacion switch
        {
            ProveedorVerificacionFiltro.Verificados =>
                query.Where(x => x.Verificado),
            ProveedorVerificacionFiltro.NoVerificados =>
                query.Where(x => !x.Verificado),
            _ => query
        };
        query = request.Kpi switch
        {
            ProveedorCatalogoKpi.Activos => query.Where(x => x.Estado == 1),
            ProveedorCatalogoKpi.PendientesVerificar =>
                query.Where(x => !x.Verificado),
            ProveedorCatalogoKpi.SinCorreo => query.Where(x =>
                x.Correo == null ||
                x.Correo == string.Empty ||
                x.Correo.ToLower() == ContactoClienteNormalizer.DefaultEmail),
            ProveedorCatalogoKpi.SinContactoDigital => query.Where(x =>
                (x.Correo == null ||
                 x.Correo == string.Empty ||
                 x.Correo.ToLower() == ContactoClienteNormalizer.DefaultEmail) &&
                (x.Telefono == null || x.Telefono == string.Empty)),
            _ => query
        };

        var total = await query.CountAsync(cancellationToken);
        var orderedQuery = (request.Orden, request.OrdenDescendente) switch
        {
            (ProveedorCatalogoOrden.Ruc, false) => query.OrderBy(x => x.Ruc),
            (ProveedorCatalogoOrden.Ruc, true) => query
                .OrderByDescending(x => x.Ruc),
            (ProveedorCatalogoOrden.Estado, false) => query
                .OrderBy(x => x.Estado),
            (ProveedorCatalogoOrden.Estado, true) => query
                .OrderByDescending(x => x.Estado),
            (ProveedorCatalogoOrden.RazonSocial, true) => query
                .OrderByDescending(x => x.RazonSocial),
            _ => query.OrderBy(x => x.RazonSocial)
        };
        var rows = await orderedQuery.ThenBy(x => x.Ruc)
            .ThenBy(x => x.TerceroId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new ProveedorCatalogoResultadoDto
        {
            Kpis = kpis,
            Total = total,
            Pagina = page,
            TamanoPagina = pageSize,
            Items = rows.Select(ToItem).ToList()
        };
    }

    public async Task<ProveedorDetalleDto?> ObtenerProveedorAsync(
        long terceroId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureActiveCompanyAccessAsync(context, cancellationToken);
        var rucTypeId = await GetRucTypeIdAsync(context, cancellationToken);
        var row = await BuildSupplierQuery(context, rucTypeId)
            .SingleOrDefaultAsync(x => x.TerceroId == terceroId, cancellationToken);
        return row is null ? null : ToDetail(row);
    }

    public async Task<ProveedorDetalleDto?> BuscarPorRucAsync(
        string ruc,
        CancellationToken cancellationToken = default)
    {
        var validation = IdentificacionEcuadorValidator.Validate(RucCode, ruc);
        if (!validation.IsValid)
            return null;

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureActiveCompanyAccessAsync(context, cancellationToken);
        var rucTypeId = await GetRucTypeIdAsync(context, cancellationToken);
        var canonicalKey = ClaveIdentidadTercero.Crear(RucCode, validation.Normalized);
        var thirdPartyId = await context.Terceros.AsNoTracking()
            .Where(x =>
                x.ClaveIdentidad == canonicalKey ||
                x.Identificaciones.Any(i =>
                    i.TipoIdentificacionId == rucTypeId &&
                    i.NumeroNormalizado == validation.Normalized &&
                    i.Estado == 1))
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (thirdPartyId is null)
            return null;

        var row = await BuildThirdPartyQuery(context, rucTypeId)
            .SingleOrDefaultAsync(
                x => x.TerceroId == thirdPartyId.Value,
                cancellationToken);
        return row is null ? null : ToDetail(row, validation.Normalized);
    }

    public async Task<ProveedorOperationResult> GuardarAsync(
        ProveedorGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Estado is not (0 or 1))
            return ProveedorOperationResult.Fail("El estado seleccionado no es válido.");
        var validation = IdentificacionEcuadorValidator.Validate(RucCode, request.Ruc);
        if (!validation.IsValid)
            return ProveedorOperationResult.Fail(validation.Error!);
        var name = NormalizeRequired(request.RazonSocial, 256);
        if (name is null)
            return ProveedorOperationResult.Fail("Ingresa la razón social del proveedor.");
        var email = ContactoClienteNormalizer.NormalizeOptionalEmail(request.Correo);
        if (!email.IsValid)
            return ProveedorOperationResult.Fail(email.Error!);
        var phone = ContactoClienteNormalizer.NormalizePhone(request.Telefono);
        if (!phone.IsValid)
            return ProveedorOperationResult.Fail(phone.Error!);

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var companyId = await EnsureActiveCompanyAccessAsync(
                context,
                cancellationToken);
            await EnsureCanManageThirdPartiesAsync(
                context,
                companyId,
                cancellationToken);
            var rucTypeId = await GetRucTypeIdAsync(context, cancellationToken);
            var canonicalKey = ClaveIdentidadTercero.Crear(
                RucCode,
                validation.Normalized);
            var thirdParty = await context.Terceros
                .Include(x => x.Identificaciones)
                .SingleOrDefaultAsync(
                    x => x.ClaveIdentidad == canonicalKey ||
                         x.Identificaciones.Any(i =>
                             i.TipoIdentificacionId == rucTypeId &&
                             i.NumeroNormalizado == validation.Normalized),
                    cancellationToken);
            if (request.TerceroId.HasValue &&
                (thirdParty is null || thirdParty.Id != request.TerceroId.Value))
                return ProveedorOperationResult.Fail(
                    "El RUC pertenece a otro tercero registrado.");
            if (thirdParty is not null)
            {
                if (!request.Version.HasValue)
                    return ProveedorOperationResult.Conflict(
                        TercerosConcurrency.UserMessage);
                context.Entry(thirdParty)
                    .Property(x => x.Version)
                    .OriginalValue = request.Version.Value;
            }

            var thirdPartyExisted = thirdParty is not null;
            var alreadySupplier = thirdParty?.EsProveedor == true;
            var now = DateTime.UtcNow;
            var isNewSupplier = thirdParty is null || !thirdParty.EsProveedor;
            if (thirdParty is null)
            {
                thirdParty = new Tercero
                {
                    TipoIdentificacionId = rucTypeId,
                    NumeroIdentificacion = validation.Normalized,
                    ClaveIdentidad = canonicalKey,
                    CreatedAt = now
                };
                context.Terceros.Add(thirdParty);
            }
            if (thirdParty.EsConsumidorFinal)
                return ProveedorOperationResult.Fail(
                    "El consumidor final es un registro protegido.");

            var identification = thirdParty.Identificaciones.SingleOrDefault(i =>
                i.TipoIdentificacionId == rucTypeId &&
                i.NumeroNormalizado == validation.Normalized);
            ConstanciaVerificacionIdentificacion? proof = null;
            if (request.ConstanciaVerificacionId.HasValue &&
                !proofStore.TryTake(
                    request.ConstanciaVerificacionId.Value,
                    currentSession.UsuarioId,
                    RucCode,
                    validation.Normalized,
                    PropositoConsultaIdentificacion.Proveedor,
                    out proof))
            {
                return ProveedorOperationResult.Fail(
                    "La constancia de verificación expiró o no corresponde al RUC. Vuelve a verificarlo.");
            }
            if (identification is null && proof is null)
            {
                return ProveedorOperationResult.Fail(
                    "Verifica el RUC antes de guardar. El registro manual solo se habilita después de tres fallos oficiales.");
            }
            if (identification is null)
            {
                identification = new TerceroIdentificacion
                {
                    Tercero = thirdParty,
                    TipoIdentificacionId = rucTypeId,
                    NumeroIdentificacion = validation.Normalized,
                    NumeroNormalizado = validation.Normalized,
                    EsPrincipal = thirdParty.Id == 0 ||
                        (thirdParty.TipoIdentificacionId == rucTypeId &&
                         thirdParty.NumeroIdentificacion == validation.Normalized),
                    CreatedAt = now
                };
                context.TercerosIdentificaciones.Add(identification);
            }

            var verification = proof?.Tipo switch
            {
                TipoConstanciaVerificacion.Verificada => "VERIFICADO",
                TipoConstanciaVerificacion.OfflineAutorizada => "PENDIENTE",
                _ => identification.EstadoVerificacion
            };
            var source = proof?.Tipo switch
            {
                TipoConstanciaVerificacion.Verificada =>
                    NormalizeOptional(proof.Fuente, 255),
                TipoConstanciaVerificacion.OfflineAutorizada => "MANUAL",
                _ => identification.FuenteVerificacion
            };
            DateTime? verifiedAt = verification == "VERIFICADO"
                ? identification.VerificadoAt ?? now
                : null;
            identification.EstadoVerificacion = verification;
            identification.FuenteVerificacion = source;
            identification.VerificadoAt = verifiedAt;
            identification.Estado = 1;
            identification.UpdatedAt = identification.Id == 0 ? null : now;

            thirdParty.RazonSocial = proof?.Tipo ==
                TipoConstanciaVerificacion.Verificada
                    ? NormalizeRequired(proof.RazonSocial, 256) ?? name
                    : verification == "VERIFICADO" && proof is null
                        ? thirdParty.RazonSocial
                        : name;
            var officialTradeName = proof?.Tipo ==
                TipoConstanciaVerificacion.Verificada
                    ? NormalizeOptional(proof.NombreComercial, 256)
                    : null;
            if (officialTradeName is not null)
                thirdParty.NombreComercial = officialTradeName;
            thirdParty.Direccion = NormalizeOptional(request.Direccion, 500);
            thirdParty.Correo = email.Value;
            thirdParty.Telefono = phone.Value;
            thirdParty.EsProveedor = true;
            thirdParty.EstadoProveedor = isNewSupplier ? 1 : request.Estado;
            if (identification.EsPrincipal)
            {
                thirdParty.EstadoVerificacion = verification;
                thirdParty.FuenteVerificacion = source;
                thirdParty.VerificadoAt = verifiedAt;
                thirdParty.OrigenRegistro = verification == "VERIFICADO"
                    ? "OFICIAL"
                    : "OFFLINE";
            }
            thirdParty.UpdatedAt = thirdParty.Id == 0 ? null : now;

            EnsureActiveCompanyHasNotChanged(companyId);
            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(CreateAudit(
                companyId,
                thirdParty.Id,
                !thirdPartyExisted
                    ? TercerosAuditActions.ProveedorCreado
                    : !alreadySupplier
                        ? TercerosAuditActions.ProveedorRolAsignado
                        : TercerosAuditActions.ProveedorActualizado,
                !thirdPartyExisted
                    ? "Se creó el proveedor global."
                    : !alreadySupplier
                        ? "Se asignó el rol Proveedor a un tercero existente."
                        : "Se actualizó el proveedor global.",
                now));
            if (proof?.Tipo == TipoConstanciaVerificacion.Verificada)
            {
                context.Auditorias.Add(CreateAudit(
                    companyId,
                    thirdParty.Id,
                    TercerosAuditActions.ProveedorVerificado,
                    "Se confirmó la verificación oficial del proveedor.",
                    now));
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProveedorOperationResult.Ok(
                isNewSupplier
                    ? "Proveedor registrado correctamente."
                    : "Proveedor actualizado correctamente.",
                thirdParty.Id);
        }
        catch (DbUpdateException exception) when (IsExpectedIdentityConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Fail(
                "Ya existe un tercero asociado a ese RUC.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Conflict(
                TercerosConcurrency.UserMessage);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning("Operación global de proveedor rechazada por sesión no válida.");
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Fail(exception.Message);
        }
        catch (InvalidOperationException exception) when (
            exception is EmpresaAccessDeniedException or
                TercerosAccessDeniedException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Fail(exception.Message);
        }
    }

    public async Task<ProveedorOperationResult> CambiarEstadoAsync(
        long terceroId,
        int estado,
        uint version,
        CancellationToken cancellationToken = default)
    {
        if (estado is not (0 or 1))
            return ProveedorOperationResult.Fail("El estado solicitado no es válido.");
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var companyId = await EnsureActiveCompanyAccessAsync(
                context,
                cancellationToken);
            await EnsureCanManageThirdPartiesAsync(
                context,
                companyId,
                cancellationToken);
            var thirdParty = await context.Terceros.SingleOrDefaultAsync(
                x => x.Id == terceroId && x.EsProveedor,
                cancellationToken);
            if (thirdParty is null)
                return ProveedorOperationResult.Fail("El proveedor no fue encontrado.");
            context.Entry(thirdParty)
                .Property(x => x.Version)
                .OriginalValue = version;
            var now = DateTime.UtcNow;
            thirdParty.EstadoProveedor = estado;
            thirdParty.UpdatedAt = now;
            EnsureActiveCompanyHasNotChanged(companyId);
            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(CreateAudit(
                companyId,
                thirdParty.Id,
                estado == 1
                    ? TercerosAuditActions.ProveedorActivado
                    : TercerosAuditActions.ProveedorInactivado,
                estado == 1
                    ? "Se activó el proveedor global."
                    : "Se inactivó el proveedor global.",
                now));
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProveedorOperationResult.Ok(
                estado == 1
                    ? "Proveedor activado correctamente."
                    : "Proveedor inactivado correctamente.",
                thirdParty.Id);
        }
        catch (UnauthorizedAccessException exception)
        {
            logger.LogWarning("Cambio de estado global de proveedor rechazado por sesión no válida.");
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Fail(exception.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Conflict(
                TercerosConcurrency.UserMessage);
        }
        catch (InvalidOperationException exception) when (
            exception is EmpresaAccessDeniedException or
                TercerosAccessDeniedException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ProveedorOperationResult.Fail(exception.Message);
        }
    }

    private Auditoria CreateAudit(
        long companyId,
        long thirdPartyId,
        string action,
        string description,
        DateTime createdAt) => new()
    {
        UsuarioId = currentSession.UsuarioId,
        EmpresaId = companyId,
        EstablecimientoId = currentSession.EstablecimientoId,
        Accion = action,
        Entidad = "terceros",
        EntidadId = thirdPartyId,
        Descripcion = description,
        CreatedAt = createdAt
    };

    private static IQueryable<ProveedorRow> BuildSupplierQuery(
        KontaxDbContext context,
        long rucTypeId) =>
        BuildThirdPartyQuery(context, rucTypeId)
            .Where(x => x.EsProveedor && x.Ruc != string.Empty);

    private static IQueryable<ProveedorRow> BuildThirdPartyQuery(
        KontaxDbContext context,
        long rucTypeId) =>
        context.Terceros.AsNoTracking()
            .Where(x => x.NumeroIdentificacion !=
                TerceroEstructural.ConsumidorFinalIdentificacion)
            .Select(x => new ProveedorRow
            {
                TerceroId = x.Id,
                Ruc = x.Identificaciones
                    .Where(i => i.TipoIdentificacionId == rucTypeId && i.Estado == 1)
                    .OrderByDescending(i => i.EsPrincipal)
                    .Select(i => i.NumeroNormalizado)
                    .FirstOrDefault() ??
                    (x.TipoIdentificacionId == rucTypeId
                        ? x.NumeroIdentificacion
                        : string.Empty),
                RazonSocial = x.RazonSocial,
                Direccion = x.Direccion,
                Correo = x.Correo,
                Telefono = x.Telefono,
                Verificado = x.Identificaciones
                    .Where(i => i.TipoIdentificacionId == rucTypeId && i.Estado == 1)
                    .Select(i => i.EstadoVerificacion == "VERIFICADO")
                    .FirstOrDefault(),
                FuenteVerificacion = x.Identificaciones
                    .Where(i => i.TipoIdentificacionId == rucTypeId && i.Estado == 1)
                    .Select(i => i.FuenteVerificacion)
                    .FirstOrDefault(),
                VerificadoAt = x.Identificaciones
                    .Where(i => i.TipoIdentificacionId == rucTypeId && i.Estado == 1)
                    .Select(i => i.VerificadoAt)
                    .FirstOrDefault(),
                EsProveedor = x.EsProveedor,
                Estado = x.EstadoProveedor,
                Version = x.Version
            });

    private static ProveedorCatalogoItemDto ToItem(ProveedorRow row) => new()
    {
        TerceroId = row.TerceroId,
        Ruc = row.Ruc,
        RazonSocial = row.RazonSocial,
        Direccion = row.Direccion,
        Correo = row.Correo,
        Telefono = row.Telefono,
        Verificado = row.Verificado,
        FuenteVerificacion = row.FuenteVerificacion,
        Estado = row.Estado,
        Version = row.Version
    };

    private static ProveedorDetalleDto ToDetail(
        ProveedorRow row,
        string? queriedRuc = null) => new()
    {
        TerceroId = row.TerceroId,
        Ruc = queriedRuc ?? row.Ruc,
        RazonSocial = row.RazonSocial,
        Direccion = row.Direccion,
        Correo = row.Correo,
        Telefono = row.Telefono,
        Verificado = row.Verificado,
        FuenteVerificacion = row.FuenteVerificacion,
        VerificadoAt = row.VerificadoAt,
        EsProveedor = row.EsProveedor,
        Estado = row.Estado,
        Version = row.Version
    };

    private static async Task<long> GetRucTypeIdAsync(
        KontaxDbContext context,
        CancellationToken cancellationToken) =>
        await context.TiposIdentificacion.AsNoTracking()
            .Where(x => x.Codigo == RucCode)
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);

    private async Task<long> EnsureActiveCompanyAccessAsync(
        KontaxDbContext context,
        CancellationToken cancellationToken)
    {
        var companyId = currentSession.EmpresaId.GetValueOrDefault();
        if (!currentSession.IsAuthenticated || companyId <= 0)
            throw new EmpresaAccessDeniedException();

        var authorized = await context.UsuariosEmpresas.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioId == currentSession.UsuarioId &&
                x.EmpresaId == companyId &&
                x.Estado == 1 &&
                x.Usuario != null &&
                x.Usuario.Estado == 1 &&
                x.Empresa != null &&
                x.Empresa.Estado == 1,
                cancellationToken);
        if (!authorized)
            throw new EmpresaAccessDeniedException();

        return companyId;
    }

    private async Task EnsureCanManageThirdPartiesAsync(
        KontaxDbContext context,
        long companyId,
        CancellationToken cancellationToken)
    {
        var authorized = await context.UsuariosEmpresasRoles.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == currentSession.UsuarioId &&
                x.UsuarioEmpresa.EmpresaId == companyId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.Rol != null &&
                x.Rol.Estado == 1 &&
                (x.Rol.Codigo == "ADMINISTRADOR" ||
                 x.Rol.RolesPermisos.Any(rp =>
                     rp.Permiso != null &&
                     rp.Permiso.Codigo == TercerosPermissions.Gestionar &&
                     rp.Permiso.Estado == 1)),
                cancellationToken);
        if (!authorized)
            throw new TercerosAccessDeniedException();
    }

    private void EnsureActiveCompanyHasNotChanged(long companyId)
    {
        if (!currentSession.IsAuthenticated ||
            currentSession.EmpresaId != companyId)
            throw new EmpresaAccessDeniedException();
    }

    private static bool IsExpectedIdentityConflict(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgres &&
        postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
        postgres.ConstraintName is
            IdentificationUniqueConstraint or
            ClaveIdentidadTercero.UniqueConstraintName or
            "ux_terceros_tipo_identificacion_numero";

    private static string? NormalizeRequired(string? value, int maxLength)
    {
        var normalized = NormalizeOptional(value, maxLength);
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return normalized is not null && normalized.Length > maxLength
            ? normalized[..maxLength]
            : normalized;
    }

    private sealed class ProveedorRow
    {
        public long TerceroId { get; init; }
        public string Ruc { get; init; } = string.Empty;
        public string RazonSocial { get; init; } = string.Empty;
        public string? Direccion { get; init; }
        public string? Correo { get; init; }
        public string? Telefono { get; init; }
        public bool Verificado { get; init; }
        public string? FuenteVerificacion { get; init; }
        public DateTime? VerificadoAt { get; init; }
        public bool EsProveedor { get; init; }
        public int Estado { get; init; }
        public uint Version { get; init; }
    }
}
