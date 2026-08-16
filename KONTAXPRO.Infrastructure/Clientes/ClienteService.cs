using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Models.Clientes;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace KONTAXPRO.Infrastructure.Clientes;

public sealed class ClienteService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    CurrentSession currentSession,
    IConstanciaVerificacionIdentificacionStore proofStore,
    ILogger<ClienteService> logger) : IClienteService
{
    public async Task<ClienteCatalogoResultadoDto> ObtenerClientesAsync(
        ClienteCatalogoQuery request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0)
            return new ClienteCatalogoResultadoDto();

        var pageSize = request.TamanoPagina is 25 or 50 or 100
            ? request.TamanoPagina
            : 25;
        var page = Math.Max(request.Pagina, 1);

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCompanyAccessAsync(
            context,
            request.EmpresaId,
            cancellationToken);
        var query = context.Terceros
            .AsNoTracking()
            .Where(x =>
                x.EsCliente &&
                x.NumeroIdentificacion !=
                    TerceroEstructural.ConsumidorFinalIdentificacion)
            .Select(x => new
            {
                Tercero = x,
                Configuracion = x.EmpresasTerceros
                    .Where(y => y.EmpresaId == request.EmpresaId)
                    .Select(y => new
                    {
                        y.Id,
                        y.ListaPrecioId,
                        y.ListaPrecio,
                        y.CreditoHabilitado,
                        y.Observacion
                    })
                    .SingleOrDefault()
            });

        query = request.Estado switch
        {
            ClienteEstadoFiltro.Activos =>
                query.Where(x => x.Tercero.EstadoCliente == 1),
            ClienteEstadoFiltro.Inactivos =>
                query.Where(x => x.Tercero.EstadoCliente == 0),
            _ => query
        };
        var text = string.IsNullOrWhiteSpace(request.Busqueda)
            ? null
            : request.Busqueda.Trim();
        if (text is not null)
        {
            var pattern = $"%{text}%";
            query = query.Where(x =>
                EF.Functions.ILike(x.Tercero.NumeroIdentificacion, pattern) ||
                EF.Functions.ILike(x.Tercero.RazonSocial, pattern) ||
                (x.Tercero.NombreComercial != null &&
                 EF.Functions.ILike(x.Tercero.NombreComercial, pattern)) ||
                (x.Tercero.Correo != null &&
                 EF.Functions.ILike(x.Tercero.Correo, pattern)) ||
                (x.Tercero.Telefono != null &&
                 EF.Functions.ILike(x.Tercero.Telefono, pattern)));
        }

        var kpis = await query
            .GroupBy(_ => 1)
            .Select(group => new ClienteCatalogoKpisDto
            {
                Clientes = group.Count(),
                PendientesVerificar = group.Count(x =>
                    (x.Tercero.TipoIdentificacion!.Codigo == "CEDULA" ||
                     x.Tercero.TipoIdentificacion.Codigo == "RUC") &&
                    x.Tercero.EstadoVerificacion != "VERIFICADO"),
                SinCredito = group.Count(x =>
                    x.Configuracion != null &&
                    !x.Configuracion.CreditoHabilitado),
                SinContactoDigital = group.Count(x =>
                    (x.Tercero.Correo == null ||
                     x.Tercero.Correo == string.Empty ||
                     x.Tercero.Correo.ToLower() ==
                         ContactoClienteNormalizer.DefaultEmail) &&
                    (x.Tercero.Telefono == null ||
                     x.Tercero.Telefono == string.Empty))
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? new ClienteCatalogoKpisDto();

        query = request.Credito switch
        {
            ClienteCreditoFiltro.ConCredito =>
                query.Where(x =>
                    x.Configuracion == null ||
                    x.Configuracion.CreditoHabilitado),
            ClienteCreditoFiltro.SinCredito =>
                query.Where(x =>
                    x.Configuracion != null &&
                    !x.Configuracion.CreditoHabilitado),
            _ => query
        };
        query = request.Verificacion switch
        {
            ClienteVerificacionFiltro.Verificados =>
                query.Where(x => x.Tercero.EstadoVerificacion == "VERIFICADO"),
            ClienteVerificacionFiltro.NoVerificados =>
                query.Where(x =>
                    (x.Tercero.TipoIdentificacion!.Codigo == "CEDULA" ||
                     x.Tercero.TipoIdentificacion.Codigo == "RUC") &&
                    x.Tercero.EstadoVerificacion != "VERIFICADO"),
            _ => query
        };
        query = request.Kpi switch
        {
            ClienteCatalogoKpi.PendientesVerificar =>
                query.Where(x =>
                    (x.Tercero.TipoIdentificacion!.Codigo == "CEDULA" ||
                     x.Tercero.TipoIdentificacion.Codigo == "RUC") &&
                    x.Tercero.EstadoVerificacion != "VERIFICADO"),
            ClienteCatalogoKpi.SinCredito =>
                query.Where(x =>
                    x.Configuracion != null &&
                    !x.Configuracion.CreditoHabilitado),
            ClienteCatalogoKpi.SinContactoDigital =>
                query.Where(x =>
                    (x.Tercero.Correo == null ||
                     x.Tercero.Correo == string.Empty ||
                     x.Tercero.Correo.ToLower() ==
                         ContactoClienteNormalizer.DefaultEmail) &&
                    (x.Tercero.Telefono == null ||
                     x.Tercero.Telefono == string.Empty)),
            _ => query
        };

        var total = await query.CountAsync(cancellationToken);
        var baseList = await context.ListasPrecio.AsNoTracking()
            .Where(x =>
                x.EmpresaId == request.EmpresaId &&
                x.EsListaBase &&
                x.Estado == 1)
            .Select(x => new { x.Codigo, x.Nombre })
            .SingleOrDefaultAsync(cancellationToken);

        var baseListCode = baseList?.Codigo ?? "A";
        var orderedQuery = (request.Orden, request.OrdenDescendente) switch
        {
            (ClienteCatalogoOrden.Identificacion, false) => query
                .OrderBy(x => x.Tercero.NumeroIdentificacion),
            (ClienteCatalogoOrden.Identificacion, true) => query
                .OrderByDescending(x => x.Tercero.NumeroIdentificacion),
            (ClienteCatalogoOrden.Clasificacion, false) => query.OrderBy(x =>
                x.Configuracion == null || x.Configuracion.ListaPrecio == null
                    ? baseListCode : x.Configuracion.ListaPrecio.Codigo),
            (ClienteCatalogoOrden.Clasificacion, true) => query
                .OrderByDescending(x =>
                    x.Configuracion == null || x.Configuracion.ListaPrecio == null
                        ? baseListCode : x.Configuracion.ListaPrecio.Codigo),
            (ClienteCatalogoOrden.Credito, false) => query.OrderBy(x =>
                x.Configuracion == null || x.Configuracion.CreditoHabilitado),
            (ClienteCatalogoOrden.Credito, true) => query.OrderByDescending(x =>
                x.Configuracion == null || x.Configuracion.CreditoHabilitado),
            (ClienteCatalogoOrden.Estado, false) => query
                .OrderBy(x => x.Tercero.EstadoCliente),
            (ClienteCatalogoOrden.Estado, true) => query
                .OrderByDescending(x => x.Tercero.EstadoCliente),
            (ClienteCatalogoOrden.RazonSocial, true) => query
                .OrderByDescending(x => x.Tercero.RazonSocial),
            _ => query.OrderBy(x => x.Tercero.RazonSocial)
        };
        var rows = await orderedQuery
            .ThenBy(x => x.Tercero.NumeroIdentificacion)
            .ThenBy(x => x.Tercero.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                EmpresaTerceroId = x.Configuracion == null
                    ? (long?)null
                    : x.Configuracion.Id,
                x.Tercero.Id,
                TipoCodigo = x.Tercero.TipoIdentificacion!.Codigo,
                TipoNombre = x.Tercero.TipoIdentificacion.Nombre,
                x.Tercero.NumeroIdentificacion,
                x.Tercero.RazonSocial,
                x.Tercero.NombreComercial,
                x.Tercero.Direccion,
                x.Tercero.Correo,
                x.Tercero.Telefono,
                x.Tercero.EstadoVerificacion,
                x.Tercero.FuenteVerificacion,
                ListaPrecioNombre = x.Configuracion == null ||
                    x.Configuracion.ListaPrecio == null
                    ? null
                    : x.Configuracion.ListaPrecio.Nombre,
                ListaPrecioCodigo = x.Configuracion == null ||
                    x.Configuracion.ListaPrecio == null
                    ? null
                    : x.Configuracion.ListaPrecio.Codigo,
                ListaPrecioId = x.Configuracion == null
                    ? (long?)null
                    : x.Configuracion.ListaPrecioId,
                CreditoHabilitado = x.Configuracion == null ||
                    x.Configuracion.CreditoHabilitado,
                x.Tercero.EstadoCliente,
                x.Tercero.Version
            })
            .ToListAsync(cancellationToken);

        return new ClienteCatalogoResultadoDto
        {
            Kpis = kpis,
            Total = total,
            Pagina = page,
            TamanoPagina = pageSize,
            Items = rows.Select(x => new ClienteCatalogoItemDto
            {
                EmpresaTerceroId = x.EmpresaTerceroId,
                TerceroId = x.Id,
                TipoIdentificacionCodigo = x.TipoCodigo,
                TipoIdentificacionNombre = x.TipoNombre,
                NumeroIdentificacion = x.NumeroIdentificacion,
                RazonSocial = x.RazonSocial,
                NombreComercial = x.NombreComercial,
                Direccion = x.Direccion,
                Correo = x.Correo,
                Telefono = x.Telefono,
                Verificacion = MapVerification(
                    x.TipoCodigo,
                    x.EstadoVerificacion),
                FuenteVerificacion = x.FuenteVerificacion,
                ListaPrecioCodigo = x.ListaPrecioCodigo ?? baseList?.Codigo,
                ListaPrecioNombre = x.ListaPrecioNombre ?? baseList?.Nombre,
                UsaListaBasePredeterminada =
                    x.ListaPrecioId is null && baseList is not null,
                CreditoHabilitado = x.CreditoHabilitado,
                Estado = x.EstadoCliente,
                Version = x.Version
            }).ToList()
        };
    }

    public async Task<ClienteDetalleDto?> ObtenerClienteAsync(
        long terceroId,
        long empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCompanyAccessAsync(context, empresaId, cancellationToken);
        var row = await context.Terceros.AsNoTracking()
            .Where(x =>
                x.Id == terceroId &&
                x.EsCliente &&
                x.NumeroIdentificacion !=
                    TerceroEstructural.ConsumidorFinalIdentificacion)
            .Select(x => new
            {
                x.Id,
                x.TipoIdentificacionId,
                TipoCodigo = x.TipoIdentificacion!.Codigo,
                x.NumeroIdentificacion,
                x.RazonSocial,
                x.NombreComercial,
                x.Direccion,
                x.Correo,
                x.Telefono,
                x.EstadoVerificacion,
                x.FuenteVerificacion,
                x.VerificadoAt,
                x.EstadoCliente,
                x.Version
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (row is null)
            return null;

        var configuration = await context.EmpresasTerceros.AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.TerceroId == terceroId)
            .Select(x => new
            {
                x.Id,
                x.ListaPrecioId,
                x.CreditoHabilitado,
                x.Observacion
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new ClienteDetalleDto
        {
            EmpresaTerceroId = configuration?.Id,
            TerceroId = row.Id,
            TipoIdentificacionId = row.TipoIdentificacionId,
            TipoIdentificacionCodigo = row.TipoCodigo,
            NumeroIdentificacion = row.NumeroIdentificacion,
            RazonSocial = row.RazonSocial,
            NombreComercial = row.NombreComercial,
            Direccion = row.Direccion,
            Correo = row.Correo,
            Telefono = row.Telefono,
            Verificacion = MapVerification(
                row.TipoCodigo,
                row.EstadoVerificacion),
            FuenteVerificacion = row.FuenteVerificacion,
            VerificadoAt = row.VerificadoAt,
            ListaPrecioId = configuration?.ListaPrecioId,
            CreditoHabilitado = configuration?.CreditoHabilitado ?? true,
            Observacion = configuration?.Observacion,
            Estado = row.EstadoCliente,
            Version = row.Version
        };
    }

    public async Task<TerceroIdentificacionDto?> BuscarPorIdentificacionAsync(
        long tipoIdentificacionId,
        string numeroIdentificacion,
        long empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCompanyAccessAsync(context, empresaId, cancellationToken);
        var type = await context.TiposIdentificacion.AsNoTracking()
            .Where(x => x.Id == tipoIdentificacionId && x.Estado == 1)
            .Select(x => new { x.Id, x.Codigo })
            .SingleOrDefaultAsync(cancellationToken);
        if (type is null)
            return null;

        var normalized = IdentificacionEcuadorValidator.Normalize(
            numeroIdentificacion,
            type.Codigo);
        var matches = await FindMatchingThirdPartiesAsync(
            context,
            type.Codigo,
            normalized,
            cancellationToken);
        var match = matches
            .OrderByDescending(x =>
                x.TipoIdentificacionId == type.Id &&
                x.NumeroIdentificacion == normalized)
            .FirstOrDefault();
        if (match is null)
            return null;

        var thirdParty = await context.Terceros.AsNoTracking()
            .Where(x =>
                x.Id == match.Id &&
                x.NumeroIdentificacion !=
                    TerceroEstructural.ConsumidorFinalIdentificacion)
            .Select(x => new
            {
                x.Id,
                x.TipoIdentificacionId,
                x.NumeroIdentificacion,
                x.RazonSocial,
                x.NombreComercial,
                x.Direccion,
                x.Correo,
                x.Telefono,
                x.EstadoVerificacion,
                x.FuenteVerificacion,
                Type = x.TipoIdentificacion!.Codigo,
                x.EsCliente,
                x.EstadoCliente,
                x.Version
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (thirdParty is null)
            return null;

        var relationship = await context.EmpresasTerceros.AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.TerceroId == thirdParty.Id)
            .Select(x => new
            {
                x.Id,
                x.ListaPrecioId,
                x.CreditoHabilitado,
                x.Observacion,
                x.Estado
            })
            .SingleOrDefaultAsync(cancellationToken);

        return new TerceroIdentificacionDto
        {
            TerceroId = thirdParty.Id,
            TipoIdentificacionId = thirdParty.TipoIdentificacionId,
            TipoIdentificacionCodigo = thirdParty.Type,
            NumeroIdentificacion = thirdParty.NumeroIdentificacion,
            EmpresaTerceroId = relationship?.Id,
            EsCliente = thirdParty.EsCliente,
            TieneConfiguracionEmpresaActual = relationship is not null,
            RazonSocial = thirdParty.RazonSocial,
            NombreComercial = thirdParty.NombreComercial,
            Direccion = thirdParty.Direccion,
            Correo = thirdParty.Correo,
            Telefono = thirdParty.Telefono,
            Verificacion = MapVerification(
                thirdParty.Type,
                thirdParty.EstadoVerificacion),
            FuenteVerificacion = thirdParty.FuenteVerificacion,
            ListaPrecioId = relationship?.ListaPrecioId,
            CreditoHabilitado = relationship?.CreditoHabilitado ?? true,
            Observacion = relationship?.Observacion,
            Estado = thirdParty.EstadoCliente,
            Version = thirdParty.Version
        };
    }

    public async Task<IReadOnlyList<TipoIdentificacionClienteDto>>
        ObtenerTiposIdentificacionAsync(
            CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await context.TiposIdentificacion.AsNoTracking()
            .Where(x =>
                x.Estado == 1 &&
                x.Codigo != "CONSUMIDOR_FINAL")
            .OrderBy(x => x.Id)
            .Select(x => new TipoIdentificacionClienteDto
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                LongitudMinima = x.LongitudMinima,
                LongitudMaxima = x.LongitudMaxima
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ListaPrecioClienteDto>>
        ObtenerListasPrecioAsync(
            long empresaId,
            CancellationToken cancellationToken = default)
    {
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureCompanyAccessAsync(context, empresaId, cancellationToken);
        return await context.ListasPrecio.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Estado == 1)
            .OrderByDescending(x => x.EsListaBase)
            .ThenBy(x => x.Orden)
            .ThenBy(x => x.Nombre)
            .Select(x => new ListaPrecioClienteDto
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre = x.Nombre,
                EsListaBase = x.EsListaBase
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClienteOperationResult> GuardarAsync(
        ClienteGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.EmpresaId <= 0)
            return ClienteOperationResult.Fail("No existe una empresa activa.");
        if (request.Estado is not (0 or 1))
            return ClienteOperationResult.Fail("El estado seleccionado no es válido.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsureCompanyAccessAsync(
                context,
                request.EmpresaId,
                cancellationToken);
            await EnsureCanManageThirdPartiesAsync(
                context,
                request.EmpresaId,
                cancellationToken);
            var type = await context.TiposIdentificacion
                .SingleOrDefaultAsync(
                    x => x.Id == request.TipoIdentificacionId && x.Estado == 1,
                    cancellationToken);
            if (type is null || type.Codigo == "CONSUMIDOR_FINAL")
                return ClienteOperationResult.Fail(
                    "El tipo de identificación seleccionado no es válido.");

            var validation = IdentificacionEcuadorValidator.Validate(
                type.Codigo,
                request.NumeroIdentificacion);
            if (!validation.IsValid)
                return ClienteOperationResult.Fail(validation.Error!);

            var name = NormalizeRequired(request.RazonSocial, 256);
            if (name is null)
                return ClienteOperationResult.Fail(
                    "Ingresa los nombres o la razón social del cliente.");
            var address = NormalizeRequired(request.Direccion, 500);
            if (address is null)
                return ClienteOperationResult.Fail(
                    "Ingresa la dirección del cliente para la facturación.");
            var email = ContactoClienteNormalizer.NormalizeEmail(request.Correo);
            if (!email.IsValid)
                return ClienteOperationResult.Fail(email.Error!);
            var phone = ContactoClienteNormalizer.NormalizePhone(request.Telefono);
            if (!phone.IsValid)
                return ClienteOperationResult.Fail(phone.Error!);

            var priceListId = request.ListaPrecioId;
            if (priceListId is null)
            {
                priceListId = await context.ListasPrecio
                    .Where(x =>
                        x.EmpresaId == request.EmpresaId &&
                        x.EsListaBase &&
                        x.Estado == 1)
                    .Select(x => (long?)x.Id)
                    .SingleOrDefaultAsync(cancellationToken);
                if (priceListId is null)
                    return ClienteOperationResult.Fail(
                        "La empresa activa no tiene una lista de precios base configurada.");
            }

            var validList = await context.ListasPrecio.AnyAsync(
                x =>
                    x.Id == priceListId &&
                    x.EmpresaId == request.EmpresaId &&
                    x.Estado == 1,
                cancellationToken);
            if (!validList)
                return ClienteOperationResult.Fail(
                    "La lista de precios no pertenece a la empresa activa.");

            var matchingThirdParties = await FindMatchingThirdPartiesAsync(
                context,
                type.Codigo,
                validation.Normalized,
                cancellationToken);
            if (matchingThirdParties.Count > 1)
                return ClienteOperationResult.Fail(
                    "Existen registros separados para la cédula y el RUC de la misma persona. Deben consolidarse antes de continuar.");

            var thirdParty = matchingThirdParties.SingleOrDefault();
            if (request.TerceroId.HasValue &&
                (thirdParty is null || thirdParty.Id != request.TerceroId.Value))
                return ClienteOperationResult.Fail(
                    "La identificación pertenece a otro tercero registrado.");
            if (thirdParty is not null)
            {
                if (!request.Version.HasValue)
                    return ClienteOperationResult.Conflict(
                        TercerosConcurrency.UserMessage);
                context.Entry(thirdParty)
                    .Property(x => x.Version)
                    .OriginalValue = request.Version.Value;
            }

            var thirdPartyExisted = thirdParty is not null;
            var alreadyClient = thirdParty?.EsCliente == true;
            var now = DateTime.UtcNow;
            if (thirdParty is null)
            {
                thirdParty = new Tercero
                {
                    TipoIdentificacionId = type.Id,
                    NumeroIdentificacion = validation.Normalized,
                    CreatedAt = now
                };
                context.Terceros.Add(thirdParty);
            }
            else if (thirdParty.EsConsumidorFinal)
            {
                return ClienteOperationResult.Fail(
                    "El consumidor final es un registro protegido.");
            }

            var isNewClient = !thirdParty.EsCliente;
            var identification = thirdParty.Identificaciones.SingleOrDefault(x =>
                x.TipoIdentificacionId == type.Id &&
                x.NumeroNormalizado == validation.Normalized);
            ConstanciaVerificacionIdentificacion? proof = null;
            if (request.ConstanciaVerificacionId.HasValue &&
                !proofStore.TryTake(
                    request.ConstanciaVerificacionId.Value,
                    currentSession.UsuarioId,
                    type.Codigo,
                    validation.Normalized,
                    PropositoConsultaIdentificacion.Cliente,
                    out proof))
            {
                return ClienteOperationResult.Fail(
                    "La constancia de verificación expiró o no corresponde a la identificación. Vuelve a verificarla.");
            }

            var nationalIdentification = type.Codigo is "CEDULA" or "RUC";
            if (nationalIdentification && identification is null && proof is null)
            {
                return ClienteOperationResult.Fail(
                    "Verifica la identificación antes de guardar. El registro manual solo se habilita después de tres fallos oficiales.");
            }

            var verification = nationalIdentification
                ? proof?.Tipo switch
                {
                    TipoConstanciaVerificacion.Verificada =>
                        EstadoVerificacionCliente.Verificado,
                    TipoConstanciaVerificacion.OfflineAutorizada =>
                        EstadoVerificacionCliente.NoVerificado,
                    _ => identification?.EstadoVerificacion == "VERIFICADO"
                        ? EstadoVerificacionCliente.Verificado
                        : EstadoVerificacionCliente.NoVerificado
                }
                : EstadoVerificacionCliente.NoAplica;
            var verificationSource = proof?.Tipo switch
            {
                TipoConstanciaVerificacion.Verificada => proof.Fuente,
                TipoConstanciaVerificacion.OfflineAutorizada => "MANUAL",
                _ => nationalIdentification
                    ? identification?.FuenteVerificacion
                    : "MANUAL"
            };

            thirdParty.ClaveIdentidad = ClaveIdentidadTercero.Crear(
                type.Codigo,
                validation.Normalized);
            thirdParty.RazonSocial = proof?.Tipo ==
                TipoConstanciaVerificacion.Verificada
                    ? NormalizeRequired(proof.RazonSocial, 256) ?? name
                    : verification == EstadoVerificacionCliente.Verificado &&
                      proof is null
                        ? thirdParty.RazonSocial
                        : name;
            var requestedTradeName = NormalizeOptional(
                request.NombreComercial,
                256);
            if (proof?.Tipo == TipoConstanciaVerificacion.Verificada)
            {
                var officialTradeName = NormalizeOptional(
                    proof.NombreComercial,
                    256);
                if (officialTradeName is not null)
                    thirdParty.NombreComercial = officialTradeName;
            }
            else if (verification != EstadoVerificacionCliente.Verificado)
            {
                thirdParty.NombreComercial = requestedTradeName;
            }
            thirdParty.Direccion = address;
            thirdParty.Correo = email.Value;
            thirdParty.Telefono = phone.Value;
            thirdParty.EstadoVerificacion =
                verification == EstadoVerificacionCliente.Verificado
                    ? "VERIFICADO"
                    : "PENDIENTE";
            thirdParty.FuenteVerificacion = NormalizeOptional(
                verificationSource,
                255) ?? "MANUAL";
            thirdParty.OrigenRegistro =
                verification == EstadoVerificacionCliente.Verificado
                    ? "OFICIAL"
                    : "OFFLINE";
            thirdParty.VerificadoAt =
                verification == EstadoVerificacionCliente.Verificado
                    ? thirdParty.VerificadoAt ?? now
                     : null;
            thirdParty.EsCliente = true;
            thirdParty.EstadoCliente = isNewClient ? 1 : request.Estado;
            thirdParty.UpdatedAt = thirdParty.Id == 0 ? null : now;
            EnsureIdentification(
                context,
                thirdParty,
                type.Id,
                validation.Normalized,
                verification,
                verificationSource,
                now);

            var relationship = thirdParty.Id == 0
                ? null
                : await context.EmpresasTerceros.SingleOrDefaultAsync(
                    x =>
                        x.EmpresaId == request.EmpresaId &&
                        x.TerceroId == thirdParty.Id,
                    cancellationToken);

            if (request.EmpresaTerceroId.HasValue &&
                (relationship is null ||
                 relationship.Id != request.EmpresaTerceroId.Value))
                return ClienteOperationResult.Fail(
                    "El cliente no pertenece a la empresa activa.");

            if (relationship is null)
            {
                relationship = new EmpresaTercero
                {
                    EmpresaId = request.EmpresaId,
                    Tercero = thirdParty,
                    CreatedAt = now
                };
                context.EmpresasTerceros.Add(relationship);
            }
            else
            {
                relationship.UpdatedAt = now;
            }

            relationship.ListaPrecioId = priceListId;
            relationship.CreditoHabilitado = request.CreditoHabilitado;
            relationship.Observacion = NormalizeOptional(request.Observacion, 1000);
            relationship.Estado = 1;

            EnsureActiveCompanyHasNotChanged(request.EmpresaId);
            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(CreateAudit(
                request.EmpresaId,
                thirdParty.Id,
                !thirdPartyExisted
                    ? TercerosAuditActions.ClienteCreado
                    : !alreadyClient
                        ? TercerosAuditActions.ClienteRolAsignado
                        : TercerosAuditActions.ClienteActualizado,
                !thirdPartyExisted
                    ? "Se creó el cliente global."
                    : !alreadyClient
                        ? "Se asignó el rol Cliente a un tercero existente."
                        : "Se actualizó el cliente global.",
                now));
            if (proof?.Tipo == TipoConstanciaVerificacion.Verificada)
            {
                context.Auditorias.Add(CreateAudit(
                    request.EmpresaId,
                    thirdParty.Id,
                    TercerosAuditActions.ClienteVerificado,
                    "Se confirmó la verificación oficial del cliente.",
                    now));
            }
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ClienteOperationResult.Ok(
                !isNewClient
                    ? "Cliente actualizado correctamente."
                    : "Cliente registrado correctamente.",
                relationship.Id,
                thirdParty.Id);
        }
        catch (DbUpdateException exception)
            when (IsExpectedIdentityConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            return ClienteOperationResult.Fail(
                "Ya existe un tercero con ese tipo y número de identificación.");
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ClienteOperationResult.Conflict(
                TercerosConcurrency.UserMessage);
        }
        catch (EmpresaAccessDeniedException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ClienteOperationResult.Fail(exception.Message);
        }
        catch (TercerosAccessDeniedException exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ClienteOperationResult.Fail(exception.Message);
        }
    }

    public async Task<ClienteOperationResult> CambiarEstadoAsync(
        long terceroId,
        long empresaId,
        int estado,
        uint version,
        CancellationToken cancellationToken = default)
    {
        if (estado is not (0 or 1))
            return ClienteOperationResult.Fail("El estado solicitado no es válido.");

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        try
        {
            await EnsureCompanyAccessAsync(context, empresaId, cancellationToken);
            await EnsureCanManageThirdPartiesAsync(
                context,
                empresaId,
                cancellationToken);
        }
        catch (InvalidOperationException exception) when (
            exception is EmpresaAccessDeniedException or
                TercerosAccessDeniedException)
        {
            return ClienteOperationResult.Fail(exception.Message);
        }
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        var thirdParty = await context.Terceros
            .SingleOrDefaultAsync(
                x =>
                    x.Id == terceroId &&
                    x.EsCliente,
                cancellationToken);
        if (thirdParty is null)
            return ClienteOperationResult.Fail("El cliente no fue encontrado.");
        if (thirdParty.EsConsumidorFinal)
            return ClienteOperationResult.Fail(
                "El consumidor final es un registro protegido.");

        context.Entry(thirdParty)
            .Property(x => x.Version)
            .OriginalValue = version;
        var now = DateTime.UtcNow;
        thirdParty.EstadoCliente = estado;
        thirdParty.UpdatedAt = now;
        try
        {
            EnsureActiveCompanyHasNotChanged(empresaId);
        }
        catch (EmpresaAccessDeniedException exception)
        {
            return ClienteOperationResult.Fail(exception.Message);
        }
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(CreateAudit(
                empresaId,
                thirdParty.Id,
                estado == 1
                    ? TercerosAuditActions.ClienteActivado
                    : TercerosAuditActions.ClienteInactivado,
                estado == 1
                    ? "Se activó el cliente global."
                    : "Se inactivó el cliente global.",
                now));
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return ClienteOperationResult.Conflict(
                TercerosConcurrency.UserMessage);
        }
        var relationshipId = await context.EmpresasTerceros.AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.TerceroId == terceroId)
            .Select(x => (long?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);
        return ClienteOperationResult.Ok(
            estado == 1
                ? "Cliente activado correctamente."
                : "Cliente inactivado correctamente.",
            relationshipId,
            thirdParty.Id);
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

    private async Task EnsureCompanyAccessAsync(
        KontaxDbContext context,
        long companyId,
        CancellationToken cancellationToken)
    {
        if (companyId <= 0 ||
            currentSession.UsuarioId <= 0 ||
            currentSession.EmpresaId != companyId)
        {
            DenyCompanyAccess();
        }

        var authorized = await context.UsuariosEmpresas.AsNoTracking()
            .AnyAsync(
                x => x.UsuarioId == currentSession.UsuarioId &&
                     x.EmpresaId == companyId &&
                     x.Estado == 1 &&
                     x.Empresa != null &&
                     x.Empresa.Estado == 1,
                cancellationToken);
        if (!authorized)
            DenyCompanyAccess();
    }

    private void EnsureActiveCompanyHasNotChanged(long companyId)
    {
        if (currentSession.UsuarioId <= 0 ||
            currentSession.EmpresaId != companyId)
        {
            DenyCompanyAccess();
        }
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
        if (authorized)
            return;

        logger.LogWarning(
            "A third-party mutation was denied because the active company role lacks the required permission.");
        throw new TercerosAccessDeniedException();
    }

    private void DenyCompanyAccess()
    {
        logger.LogWarning(
            "A client operation was denied because the active company context is not authorized.");
        throw new EmpresaAccessDeniedException();
    }

    private static EstadoVerificacionCliente MapVerification(
        string type,
        string storedStatus) =>
        type is "PASAPORTE" or "EXTERIOR"
            ? EstadoVerificacionCliente.NoAplica
            : storedStatus == "VERIFICADO"
                ? EstadoVerificacionCliente.Verificado
            : EstadoVerificacionCliente.NoVerificado;

    private static async Task<List<Tercero>> FindMatchingThirdPartiesAsync(
        KontaxDbContext context,
        string requestedTypeCode,
        string normalizedNumber,
        CancellationToken cancellationToken)
    {
        var canonicalKey = ClaveIdentidadTercero.Crear(
            requestedTypeCode,
            normalizedNumber);
        return await context.Terceros
            .Include(x => x.Identificaciones)
            .Where(x =>
                x.ClaveIdentidad == canonicalKey ||
                x.Identificaciones.Any(i =>
                    i.TipoIdentificacion!.Codigo == requestedTypeCode &&
                    i.NumeroNormalizado == normalizedNumber &&
                    i.Estado == 1))
            .ToListAsync(cancellationToken);
    }

    private static void EnsureIdentification(
        KontaxDbContext context,
        Tercero thirdParty,
        long typeId,
        string normalizedNumber,
        EstadoVerificacionCliente verification,
        string? verificationSource,
        DateTime now)
    {
        var identification = thirdParty.Identificaciones.SingleOrDefault(x =>
            x.TipoIdentificacionId == typeId &&
            x.NumeroNormalizado == normalizedNumber);
        if (identification is null)
        {
            identification = new TerceroIdentificacion
            {
                Tercero = thirdParty,
                TipoIdentificacionId = typeId,
                NumeroIdentificacion = normalizedNumber,
                NumeroNormalizado = normalizedNumber,
                EsPrincipal = thirdParty.Id == 0 ||
                    (thirdParty.TipoIdentificacionId == typeId &&
                     thirdParty.NumeroIdentificacion == normalizedNumber),
                CreatedAt = now
            };
            context.TercerosIdentificaciones.Add(identification);
        }

        identification.EstadoVerificacion =
            verification == EstadoVerificacionCliente.Verificado
                ? "VERIFICADO"
                : "PENDIENTE";
        identification.FuenteVerificacion = verification switch
        {
            EstadoVerificacionCliente.Verificado =>
                NormalizeOptional(verificationSource, 255),
            _ => "MANUAL"
        };
        identification.VerificadoAt =
            verification == EstadoVerificacionCliente.Verificado
                ? identification.VerificadoAt ?? now
                : null;
        identification.Estado = 1;
        identification.UpdatedAt = identification.Id == 0 ? null : now;
    }

    private static bool IsExpectedIdentityConflict(
        DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName:
                ClaveIdentidadTercero.UniqueConstraintName or
                "ux_terceros_tipo_identificacion_numero" or
                "ux_terceros_identificaciones_tipo_numero"
        };

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

}
