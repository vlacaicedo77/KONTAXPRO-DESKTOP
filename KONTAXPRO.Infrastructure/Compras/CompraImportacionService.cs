using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class CompraImportacionService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    IComprobanteCompraXmlReader xmlReader,
    ImportacionCompraStore store,
    CurrentSession currentSession,
    ComprasRuntimeOptions runtimeOptions,
    IProveedorService proveedorService,
    IConsultaIdentificacionService identificationService,
    IConsultaAutorizacionComprobanteSri autorizacionSri)
    : ICompraImportacionService
{
    public async Task<CompraImportacionAnalisisDto> AnalizarXmlAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue)
            return CompraImportacionAnalisisDto.Fallo(
                "Inicia sesión y selecciona una empresa antes de importar.");

        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var company = await EnsureImportAccessAsync(
            context, companyId, cancellationToken);
        if (company is null)
            return CompraImportacionAnalisisDto.Fallo(
                "No tienes autorización para importar XML en la empresa activa.");

        byte[] bytes;
        try
        {
            bytes = await ReadLimitedAsync(contenido, cancellationToken);
        }
        catch (InvalidDataException)
        {
            return CompraImportacionAnalisisDto.Fallo(
                "El XML supera el tamaño máximo permitido de 5 MB.",
                ErrorLecturaComprobanteCompra.ArchivoDemasiadoGrande);
        }
        await using var parseStream = new MemoryStream(bytes, writable: false);
        var reading = await xmlReader.LeerFacturaAsync(
            parseStream, nombreArchivo, cancellationToken);
        if (!reading.Exito || reading.Factura is null)
            return CompraImportacionAnalisisDto.Fallo(
                reading.Mensaje, reading.Error);

        var factura = reading.Factura;
        if (!CompraImportacionRules.ReceptorCorrespondeEmpresa(
                company.NumeroIdentificacion,
                factura.TipoIdentificacionComprador,
                factura.IdentificacionComprador))
            return CompraImportacionAnalisisDto.Fallo(
                "La factura pertenece a otro receptor y no puede registrarse en la empresa activa.");

        var pruebas = factura.Ambiente == "1";
        if (pruebas && !runtimeOptions.EsDevelopment)
            return CompraImportacionAnalisisDto.Fallo(
                "Una factura emitida en ambiente de pruebas no puede importarse fuera de Development.");

        var authorization = await autorizacionSri.ConsultarAsync(
            factura.ClaveAcceso, factura.Ambiente, cancellationToken);
        var authorizationError = AplicarResultadoAutorizacion(
            factura, authorization);
        if (authorizationError is not null)
            return CompraImportacionAnalisisDto.Fallo(
                authorizationError,
                ErrorLecturaComprobanteCompra.ComprobanteNoAutorizado);

        var duplicate = await context.DocumentosRecibidosSri.AsNoTracking()
            .Where(x => x.ClaveAcceso == factura.ClaveAcceso ||
                        x.ArchivoSha256 == factura.Sha256)
            .Select(x => new
            {
                DocumentoId = x.Id,
                CompraId = context.Compras
                    .Where(c => c.DocumentoRecibidoSriId == x.Id)
                    .Select(c => (long?)c.Id)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (duplicate is not null)
            return new CompraImportacionAnalisisDto
            {
                Mensaje = "Esta factura ya fue registrada.",
                Factura = factura,
                EsDuplicado = true,
                DocumentoExistenteId = duplicate.DocumentoId,
                CompraExistenteId = duplicate.CompraId
            };

        var supplier = await FindSupplierAsync(
            context, factura.RucEmisor, cancellationToken);
        var balance = CompraImportacionRules.ValidarCuadre(factura);
        if (!balance.Cuadra)
            return new CompraImportacionAnalisisDto
            {
                Mensaje = balance.Mensaje,
                Factura = factura,
                Cuadre = balance,
                EstadoProveedor = supplier.Estado,
                TerceroId = supplier.TerceroId,
                RazonSocialProveedorLocal = supplier.RazonSocial
            };

        var preparedSupplier = await EnsureSupplierReadyAsync(
            factura, cancellationToken);
        if (!preparedSupplier.Success)
            return CompraImportacionAnalisisDto.Fallo(
                preparedSupplier.Message);
        supplier = new SupplierResolution(
            EstadoProveedorImportacion.ProveedorActivo,
            preparedSupplier.TerceroId,
            preparedSupplier.RazonSocial);

        var importId = store.Add(
            currentSession.UsuarioId, companyId, factura, bytes);
        return new CompraImportacionAnalisisDto
        {
            Exito = true,
            Mensaje = "Factura XML analizada correctamente.",
            ImportacionId = importId,
            Factura = factura,
            Cuadre = balance,
            EsAmbientePruebas = pruebas,
            AdvertenciaAmbiente = ConstruirAdvertencia(
                pruebas, factura),
            EstadoProveedor = supplier.Estado,
            TerceroId = supplier.TerceroId,
            RazonSocialProveedorLocal = supplier.RazonSocial,
            DireccionProveedorLocal = preparedSupplier.Address,
            ProveedorCreadoAutomaticamente = preparedSupplier.Created
        };
    }

    private static string? AplicarResultadoAutorizacion(
        FacturaCompraXmlDto factura,
        ConsultaAutorizacionSriDto result)
    {
        switch (result.Estado)
        {
            case EstadoConsultaAutorizacionSri.Autorizado:
                if (!string.IsNullOrWhiteSpace(result.RucEmisor) &&
                    !string.Equals(result.RucEmisor, factura.RucEmisor,
                        StringComparison.Ordinal))
                    return "La autorización consultada en el SRI corresponde a otro emisor.";
                if (string.IsNullOrWhiteSpace(result.ComprobanteSha256) ||
                    !string.Equals(result.ComprobanteSha256,
                        factura.ComprobanteSha256,
                        StringComparison.OrdinalIgnoreCase))
                    return "El contenido del XML no coincide exactamente con el comprobante autorizado por el SRI.";
                factura.EstadoAutorizacionSri = "AUTORIZADO";
                factura.NumeroAutorizacion = result.NumeroAutorizacion ??
                                             factura.ClaveAcceso;
                factura.FechaAutorizacion = result.FechaAutorizacion ??
                                             factura.FechaAutorizacion;
                factura.EstadoValidacion = "AUTORIZADO_SRI";
                factura.MensajeValidacion =
                    "Firma y clave válidas. Autorización confirmada en el SRI.";
                return null;
            case EstadoConsultaAutorizacionSri.NoDisponible:
                factura.EstadoValidacion = "ADVERTENCIA";
                factura.MensajeValidacion =
                    "El XML tiene firma y clave válidas, pero el SRI no estuvo disponible para confirmar su estado actual.";
                return null;
            case EstadoConsultaAutorizacionSri.NoAutorizado:
                return "El SRI reporta que el comprobante no está autorizado.";
            case EstadoConsultaAutorizacionSri.PendienteAnulacion:
                return "El comprobante está pendiente de anulación en el SRI y no puede registrarse.";
            case EstadoConsultaAutorizacionSri.Anulado:
                return "El comprobante consta como anulado en el SRI y no puede registrarse.";
            default:
                return string.IsNullOrWhiteSpace(result.Mensaje)
                    ? "El comprobante no consta como autorizado en el SRI."
                    : result.Mensaje;
        }
    }

    private static string? ConstruirAdvertencia(
        bool pruebas,
        FacturaCompraXmlDto factura)
    {
        var warnings = new List<string>();
        if (pruebas)
            warnings.Add(
                "El comprobante fue emitido en ambiente de pruebas y no tiene validez tributaria.");
        if (factura.EstadoValidacion == "ADVERTENCIA")
            warnings.Add(factura.MensajeValidacion);
        return warnings.Count == 0 ? null : string.Join(" ", warnings);
    }

    private async Task<PreparedSupplier> EnsureSupplierReadyAsync(
        FacturaCompraXmlDto factura,
        CancellationToken cancellationToken)
    {
        var existing = await proveedorService.BuscarPorRucAsync(
            factura.RucEmisor, cancellationToken);
        if (existing is { EsProveedor: true, Estado: 1 })
            return PreparedSupplier.Ok(
                existing.TerceroId, existing.RazonSocial,
                existing.Direccion, false);

        if (existing is { EsProveedor: true })
        {
            var activation = await proveedorService.CambiarEstadoAsync(
                existing.TerceroId, 1, existing.Version, cancellationToken);
            return activation.Success
                ? PreparedSupplier.Ok(
                    existing.TerceroId, existing.RazonSocial,
                    existing.Direccion, false)
                : PreparedSupplier.Fail(activation.Message);
        }

        var name = existing?.RazonSocial ?? factura.RazonSocialEmisor;
        var address = existing?.Direccion ?? factura.DireccionMatriz;
        var email = existing?.Correo;
        Guid? verificationProofId = null;

        if (existing is null)
        {
            var flowId = Guid.NewGuid();
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var verification = await identificationService.ConsultarAsync(
                    new ConsultaIdentificacionRequest(
                        "RUC",
                        factura.RucEmisor,
                        PropositoConsultaIdentificacion.Proveedor,
                        flowId),
                    cancellationToken);
                verificationProofId = verification.ConstanciaVerificacionId ??
                                      verificationProofId;
                if (verification.Encontrado)
                {
                    name = verification.RazonSocial?.Trim() ?? name;
                    address = verification.Direccion?.Trim() ?? address;
                    email = ContactoClienteNormalizer.FirstEmailOrNull(
                        verification.Correo)?.ToLowerInvariant();
                    break;
                }
                if (verification.Estado is
                    EstadoConsultaIdentificacion.NoEncontrado or
                    EstadoConsultaIdentificacion.IdentificacionInvalida)
                    return PreparedSupplier.Fail(
                        verification.MensajeUsuario);
                if (attempt < 3)
                    await Task.Delay(500, cancellationToken);
            }

            if (!verificationProofId.HasValue)
                return PreparedSupplier.Fail(
                    "No fue posible verificar ni autorizar el registro offline del proveedor.");
        }

        var save = await proveedorService.GuardarAsync(
            new ProveedorGuardarRequest
            {
                TerceroId = existing?.TerceroId,
                Version = existing?.Version,
                Ruc = factura.RucEmisor,
                RazonSocial = name,
                Direccion = address,
                Correo = email,
                Telefono = existing?.Telefono,
                ConstanciaVerificacionId = verificationProofId,
                Estado = 1
            },
            cancellationToken);
        return save.Success && save.TerceroId.HasValue
            ? PreparedSupplier.Ok(save.TerceroId.Value, name, address, true)
            : PreparedSupplier.Fail(save.Message);
    }

    private async Task<global::KONTAXPRO.Domain.Entities.Configuracion.Empresa?>
        EnsureImportAccessAsync(
            KontaxDbContext context,
            long companyId,
            CancellationToken cancellationToken)
    {
        if (currentSession.EmpresaId != companyId)
            return null;
        var access = await context.UsuariosEmpresas.AsNoTracking()
            .AnyAsync(x => x.UsuarioId == currentSession.UsuarioId &&
                           x.EmpresaId == companyId && x.Estado == 1,
                cancellationToken);
        if (!access) return null;
        var allowed = await context.UsuariosEmpresasRoles.AsNoTracking()
            .AnyAsync(x =>
                x.UsuarioEmpresa!.UsuarioId == currentSession.UsuarioId &&
                x.UsuarioEmpresa.EmpresaId == companyId &&
                x.UsuarioEmpresa.Estado == 1 &&
                x.Rol!.Estado == 1 &&
                (x.Rol.Codigo == "ADMINISTRADOR" ||
                 x.Rol.RolesPermisos.Any(rp =>
                     rp.Permiso!.Codigo == ComprasPermissions.ImportarXml &&
                     rp.Permiso.Estado == 1)),
                cancellationToken);
        if (!allowed) return null;
        return await context.Empresas.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == companyId && x.Estado == 1,
                cancellationToken);
    }

    private static async Task<SupplierResolution> FindSupplierAsync(
        KontaxDbContext context,
        string ruc,
        CancellationToken cancellationToken)
    {
        var typeId = await context.TiposIdentificacion.AsNoTracking()
            .Where(x => x.Codigo == "RUC")
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        var key = ClaveIdentidadTercero.Crear("RUC", ruc);
        var thirdParty = await context.Terceros.AsNoTracking()
            .Where(x => x.ClaveIdentidad == key ||
                        x.Identificaciones.Any(i =>
                            i.TipoIdentificacionId == typeId &&
                            i.NumeroNormalizado == ruc && i.Estado == 1))
            .Select(x => new
            {
                x.Id,
                x.RazonSocial,
                x.EsProveedor,
                x.EstadoProveedor
            })
            .SingleOrDefaultAsync(cancellationToken);
        if (thirdParty is null)
            return new SupplierResolution(
                EstadoProveedorImportacion.NoExiste, null, null);
        var state = !thirdParty.EsProveedor
            ? EstadoProveedorImportacion.TerceroSinProveedor
            : thirdParty.EstadoProveedor == 1
                ? EstadoProveedorImportacion.ProveedorActivo
                : EstadoProveedorImportacion.ProveedorInactivo;
        return new SupplierResolution(state, thirdParty.Id,
            thirdParty.RazonSocial);
    }

    private sealed record SupplierResolution(
        EstadoProveedorImportacion Estado,
        long? TerceroId,
        string? RazonSocial);

    private sealed record PreparedSupplier(
        bool Success,
        string Message,
        long? TerceroId,
        string? RazonSocial,
        string? Address,
        bool Created)
    {
        public static PreparedSupplier Ok(
            long terceroId,
            string razonSocial,
            string? address,
            bool created) =>
            new(true, string.Empty, terceroId, razonSocial, address, created);

        public static PreparedSupplier Fail(string message) =>
            new(false, message, null, null, null, false);
    }

    private static async Task<byte[]> ReadLimitedAsync(
        Stream source,
        CancellationToken cancellationToken)
    {
        const int maximum = 5 * 1024 * 1024;
        if (source.CanSeek && source.Length > maximum)
            throw new InvalidDataException();
        await using var destination = new MemoryStream();
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0) break;
            if (destination.Length + read > maximum)
                throw new InvalidDataException();
            await destination.WriteAsync(
                buffer.AsMemory(0, read), cancellationToken);
        }
        return destination.ToArray();
    }
}

public sealed class ComprasRuntimeOptions
{
    public bool EsDevelopment { get; init; }
}
