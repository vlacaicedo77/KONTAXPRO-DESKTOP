using System.Linq.Expressions;
using KONTAXPRO.Application.Compras;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Domain.Entities.Compras;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class CompraProductoResolverService(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    CurrentSession currentSession)
    : ICompraProductoResolverService
{
    public async Task<IReadOnlyList<LineaCompraResueltaDto>> ResolverAsync(
        ResolverProductosCompraRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue || request.Lineas.Count == 0)
            return [];

        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var access = await context.UsuariosEmpresas.AsNoTracking().AnyAsync(x =>
            x.UsuarioId == currentSession.UsuarioId &&
            x.EmpresaId == companyId && x.Estado == 1,
            cancellationToken);
        if (!access) return [];

        var sourceCodes = request.Lineas
            .SelectMany(x => new[] { x.CodigoPrincipal, x.CodigoAuxiliar })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var codeVariants = sourceCodes
            .SelectMany(x => new[] { x, x.ToUpperInvariant(), x.ToLowerInvariant() })
            .Distinct(StringComparer.Ordinal).ToList();

        var barcodeMatches = codeVariants.Count == 0
            ? []
            : await context.ProductosPresentaciones.AsNoTracking()
                .Where(x => x.EmpresaId == companyId &&
                            x.CodigoBarras != null &&
                            codeVariants.Contains(x.CodigoBarras))
                .Select(x => new ProductCandidate
                {
                    ProductId = x.ProductoId,
                    PresentationId = x.Id,
                    ProductName = x.Producto!.Nombre,
                    BrandName = x.Producto.Marca != null
                        ? x.Producto.Marca.Nombre : null,
                    Model = x.Producto.Modelo,
                    Description = x.Producto.Descripcion,
                    PresentationName = x.Nombre,
                    PresentationCode = x.Codigo,
                    Barcode = x.CodigoBarras,
                    Factor = x.FactorConversion,
                    IsBase = x.EsPresentacionBase,
                    HandlesLots = x.Producto.ManejaLotes,
                    HandlesSeries = x.Producto.ManejaSeries,
                    HandlesExpiration = x.Producto.ManejaFechaCaducidad,
                    ProductActive = x.Producto.Estado == 1,
                    PresentationActive = x.Estado == 1,
                    AllowsPurchase = x.PermiteCompra
                }).ToListAsync(cancellationToken);

        var normalizedCodes = sourceCodes
            .Select(CompraImportacionRules.NormalizarCodigoProveedor)
            .Where(x => x.Length > 0).Distinct().ToList();
        var equivalences = request.TerceroProveedorId.HasValue &&
                           normalizedCodes.Count > 0
            ? await context.ProveedoresProductosEquivalencias.AsNoTracking()
                .Where(x => x.EmpresaId == companyId &&
                            x.TerceroId == request.TerceroProveedorId.Value &&
                            normalizedCodes.Contains(
                                x.CodigoProveedorNormalizado))
                .Select(x => new EquivalenceCandidate
                {
                    NormalizedCode = x.CodigoProveedorNormalizado,
                    Type = x.TipoCodigo,
                    Product = new ProductCandidate
                    {
                        ProductId = x.ProductoPresentacion!.ProductoId,
                        PresentationId = x.ProductoPresentacionId,
                        ProductName = x.ProductoPresentacion.Producto!.Nombre,
                        BrandName = x.ProductoPresentacion.Producto.Marca != null
                            ? x.ProductoPresentacion.Producto.Marca.Nombre : null,
                        Model = x.ProductoPresentacion.Producto.Modelo,
                        Description = x.ProductoPresentacion.Producto.Descripcion,
                        PresentationName = x.ProductoPresentacion.Nombre,
                        PresentationCode = x.ProductoPresentacion.Codigo,
                        Barcode = x.ProductoPresentacion.CodigoBarras,
                        Factor = x.ProductoPresentacion.FactorConversion,
                        IsBase = x.ProductoPresentacion.EsPresentacionBase,
                        HandlesLots = x.ProductoPresentacion.Producto.ManejaLotes,
                        HandlesSeries = x.ProductoPresentacion.Producto.ManejaSeries,
                        HandlesExpiration = x.ProductoPresentacion.Producto.ManejaFechaCaducidad,
                        ProductActive = x.ProductoPresentacion.Producto.Estado == 1,
                        PresentationActive = x.ProductoPresentacion.Estado == 1,
                        AllowsPurchase = x.ProductoPresentacion.PermiteCompra
                    }
                }).ToListAsync(cancellationToken)
            : [];

        var terms = request.Lineas.SelectMany(x =>
                TextoCompraSimilarity.Tokens(x.Descripcion))
            .Where(x => x.Length >= 4).Distinct().Take(30).ToList();
        var textCandidates = terms.Count == 0
            ? []
            : await context.ProductosPresentaciones.AsNoTracking()
                .Where(x => x.EmpresaId == companyId && x.Estado == 1 &&
                            x.PermiteCompra && x.Producto!.Estado == 1)
                .Where(BuildTextPredicate(terms))
                .OrderByDescending(x => x.EsPresentacionBase)
                .Take(300)
                .Select(x => new ProductCandidate
                {
                    ProductId = x.ProductoId,
                    PresentationId = x.Id,
                    ProductName = x.Producto!.Nombre,
                    BrandName = x.Producto.Marca != null
                        ? x.Producto.Marca.Nombre : null,
                    Model = x.Producto.Modelo,
                    Description = x.Producto.Descripcion,
                    PresentationName = x.Nombre,
                    PresentationCode = x.Codigo,
                    Barcode = x.CodigoBarras,
                    Factor = x.FactorConversion,
                    IsBase = x.EsPresentacionBase,
                    HandlesLots = x.Producto.ManejaLotes,
                    HandlesSeries = x.Producto.ManejaSeries,
                    HandlesExpiration = x.Producto.ManejaFechaCaducidad,
                    ProductActive = true,
                    PresentationActive = true,
                    AllowsPurchase = true
                }).ToListAsync(cancellationToken);

        return request.Lineas.Select(line => ResolveLine(
            line, barcodeMatches, equivalences, textCandidates)).ToList();
    }

    public async Task<IReadOnlyList<CandidatoProductoCompraDto>>
        BuscarPresentacionesAsync(
            string? busqueda = null,
            long? productoId = null,
            int limite = 20,
            CancellationToken cancellationToken = default)
    {
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue ||
            (!productoId.HasValue && string.IsNullOrWhiteSpace(busqueda)))
            return [];

        var companyId = currentSession.EmpresaId.Value;
        var normalizedSearch = busqueda?.Trim().ToUpperInvariant() ??
                               string.Empty;
        if (!productoId.HasValue && normalizedSearch.Length < 2)
            return [];

        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var access = await context.UsuariosEmpresas.AsNoTracking().AnyAsync(x =>
            x.UsuarioId == currentSession.UsuarioId &&
            x.EmpresaId == companyId && x.Estado == 1,
            cancellationToken);
        if (!access) return [];

        var query = context.ProductosPresentaciones.AsNoTracking()
            .Where(x => x.EmpresaId == companyId && x.Estado == 1 &&
                        x.PermiteCompra && x.Producto!.Estado == 1 &&
                        x.Producto.ManejaInventario);
        if (productoId.HasValue)
        {
            query = query.Where(x => x.ProductoId == productoId.Value);
        }
        else
        {
            query = query.Where(x =>
                x.Codigo.ToUpper().Contains(normalizedSearch) ||
                (x.CodigoBarras != null &&
                 x.CodigoBarras.ToUpper().Contains(normalizedSearch)) ||
                 x.Nombre.ToUpper().Contains(normalizedSearch) ||
                 x.Producto!.Nombre.ToUpper().Contains(normalizedSearch) ||
                 (x.Producto.Marca != null &&
                  x.Producto.Marca.Nombre.ToUpper().Contains(normalizedSearch)) ||
                 (x.Producto.Modelo != null &&
                  x.Producto.Modelo.ToUpper().Contains(normalizedSearch)));
        }

        var take = Math.Clamp(limite, 1, 30);
        return await query
            .OrderByDescending(x => x.EsPresentacionBase)
            .ThenBy(x => x.Producto!.Nombre)
            .ThenBy(x => x.Nombre)
            .Take(take)
            .Select(x => new CandidatoProductoCompraDto
            {
                ProductoId = x.ProductoId,
                 ProductoPresentacionId = x.Id,
                 ProductoNombre = x.Producto!.Nombre,
                 MarcaNombre = x.Producto.Marca != null
                     ? x.Producto.Marca.Nombre
                     : null,
                 PresentacionNombre = x.Nombre,
                 PresentacionCodigo = x.Codigo,
                CodigoBarras = x.CodigoBarras,
                FactorConversion = x.FactorConversion,
                EsPresentacionBase = x.EsPresentacionBase,
                 ManejaLotes = x.Producto!.ManejaLotes,
                 ManejaSeries = x.Producto.ManejaSeries,
                 ManejaFechaCaducidad = x.Producto.ManejaFechaCaducidad,
                 CostoPromedio = x.Producto.Costo != null
                     ? x.Producto.Costo.CostoPromedio
                     : 0,
                 Confianza = 0,
                Motivo = "BUSQUEDA_MANUAL"
            }).ToListAsync(cancellationToken);
    }

    public async Task<CompraOperationResult> GuardarEquivalenciaAsync(
        GuardarEquivalenciaProveedorProductoRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalized = CompraImportacionRules.NormalizarCodigoProveedor(
            request.CodigoProveedor);
        var type = request.TipoCodigo.Trim().ToUpperInvariant();
        if (request.TerceroProveedorId <= 0 ||
            request.ProductoPresentacionId <= 0 || normalized.Length == 0 ||
            type is not ("PRINCIPAL" or "AUXILIAR"))
            return CompraOperationResult.Fail(
                "Proveedor, código, tipo y presentación son obligatorios.");
        if (!currentSession.IsAuthenticated ||
            !currentSession.EmpresaId.HasValue)
            return CompraOperationResult.Fail(
                "No existe una sesión empresarial activa.");

        var companyId = currentSession.EmpresaId.Value;
        await using var context =
            await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction =
            await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (!await HasPermissionAsync(context, companyId,
                    ComprasPermissions.Crear, cancellationToken))
                return CompraOperationResult.Fail(
                    "No tienes autorización para memorizar equivalencias de compra.");
            var supplier = await context.Terceros.SingleOrDefaultAsync(x =>
                    x.Id == request.TerceroProveedorId && x.EsProveedor &&
                    x.EstadoProveedor == 1,
                cancellationToken);
            if (supplier is null)
                return CompraOperationResult.Fail(
                    "El proveedor no existe o está inactivo.");
            var presentation = await context.ProductosPresentaciones
                .Include(x => x.Producto)
                .SingleOrDefaultAsync(x =>
                        x.Id == request.ProductoPresentacionId &&
                        x.EmpresaId == companyId && x.Estado == 1 &&
                        x.PermiteCompra && x.Producto!.Estado == 1,
                    cancellationToken);
            if (presentation is null)
                return CompraOperationResult.Fail(
                    "La presentación no está activa o no pertenece a la empresa.");

            var equivalence = await context.ProveedoresProductosEquivalencias
                .SingleOrDefaultAsync(x => x.EmpresaId == companyId &&
                    x.TerceroId == supplier.Id && x.TipoCodigo == type &&
                    x.CodigoProveedorNormalizado == normalized,
                    cancellationToken);
            var now = DateTime.UtcNow;
            string action;
            if (equivalence is null)
            {
                equivalence = new ProveedorProductoEquivalencia
                {
                    EmpresaId = companyId,
                    TerceroId = supplier.Id,
                    CodigoProveedor = request.CodigoProveedor.Trim(),
                    CodigoProveedorNormalizado = normalized,
                    TipoCodigo = type,
                    ProductoPresentacionId = presentation.Id,
                    DescripcionOriginal = Normalize(request.DescripcionOriginal),
                    CreadoPorUsuarioId = currentSession.UsuarioId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                context.ProveedoresProductosEquivalencias.Add(equivalence);
                action = ComprasAuditActions.EquivalenciaCreada;
            }
            else
            {
                if (equivalence.ProductoPresentacionId != presentation.Id &&
                    !request.ReemplazarExistente)
                    return CompraOperationResult.Fail(
                        "El código ya está asociado a otra presentación. Confirma expresamente el reemplazo.");
                equivalence.ProductoPresentacionId = presentation.Id;
                equivalence.CodigoProveedor = request.CodigoProveedor.Trim();
                equivalence.DescripcionOriginal =
                    Normalize(request.DescripcionOriginal);
                equivalence.UpdatedAt = now;
                action = ComprasAuditActions.EquivalenciaActualizada;
            }

            await context.SaveChangesAsync(cancellationToken);
            context.Auditorias.Add(new Auditoria
            {
                UsuarioId = currentSession.UsuarioId,
                EmpresaId = companyId,
                EstablecimientoId = currentSession.EstablecimientoId,
                Accion = action,
                Entidad = "proveedores_productos_equivalencias",
                EntidadId = equivalence.Id,
                Descripcion = "Se confirmó una equivalencia entre un código de proveedor y una presentación de producto.",
                CreatedAt = now
            });
            if (currentSession.EmpresaId != companyId)
                return CompraOperationResult.Fail(
                    "La empresa activa cambió durante la operación.");
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return CompraOperationResult.Ok(equivalence.Id,
                "La equivalencia quedó guardada para futuras compras.");
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CompraOperationResult.Conflict(
                "La equivalencia fue modificada simultáneamente. Vuelve a resolver la línea.");
        }
    }

    private static LineaCompraResueltaDto ResolveLine(
        LineaCompraAResolverDto line,
        IReadOnlyList<ProductCandidate> barcodeMatches,
        IReadOnlyList<EquivalenceCandidate> equivalences,
        IReadOnlyList<ProductCandidate> textCandidates)
    {
        foreach (var code in new[] { line.CodigoPrincipal, line.CodigoAuxiliar })
        {
            if (string.IsNullOrWhiteSpace(code)) continue;
            var exact = barcodeMatches.Where(x => string.Equals(
                    x.Barcode, code.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
            var valid = exact.Where(IsValid).ToList();
            if (valid.Count == 1)
                return Recognized(line, valid[0], "CODIGO_BARRAS",
                    "Código de barras exacto reconocido.");
            if (valid.Count > 1)
                return Suggested(line, valid, "CODIGO_AMBIGUO",
                    "El código coincide con más de una presentación y requiere revisión.",
                    1m);
            if (exact.Count > 0)
                return new LineaCompraResueltaDto
                {
                    Orden = line.Orden,
                    Mensaje = "El código corresponde a un producto o presentación inactiva o no habilitada para compra."
                };
        }

        foreach (var pair in new[]
                 {
                     (line.CodigoPrincipal, "PRINCIPAL"),
                     (line.CodigoAuxiliar, "AUXILIAR")
                 })
        {
            var normalized = CompraImportacionRules
                .NormalizarCodigoProveedor(pair.Item1);
            if (normalized.Length == 0) continue;
            var matches = equivalences.Where(x =>
                    x.NormalizedCode == normalized && x.Type == pair.Item2)
                .Select(x => x.Product).Where(IsValid).ToList();
            if (matches.Count == 1)
                return Recognized(line, matches[0], "EQUIVALENCIA_PROVEEDOR",
                    "Equivalencia previamente confirmada.");
        }

        var candidates = textCandidates.Select(candidate => new
            {
                 Candidate = candidate,
                 Score = TextoCompraSimilarity.Calcular(line.Descripcion,
                     $"{candidate.ProductName} {candidate.BrandName} {candidate.Model} " +
                     $"{candidate.Description} {candidate.PresentationName}")
            })
            .Where(x => x.Score >= 0.35m)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.ProductName)
            .Take(3).ToList();
        return candidates.Count == 0
            ? new LineaCompraResueltaDto
            {
                Orden = line.Orden,
                Mensaje = "No se encontró una coincidencia automática."
            }
            : Suggested(line, candidates.Select(x => x.Candidate).ToList(),
                "SIMILITUD_TEXTO",
                "Se encontraron posibles coincidencias que deben confirmarse manualmente.",
                null,
                candidates.Select(x => x.Score).ToList());
    }

    private static LineaCompraResueltaDto Recognized(
        LineaCompraAResolverDto line,
        ProductCandidate product,
        string origin,
        string message) => new()
    {
        Orden = line.Orden,
        Estado = "RECONOCIDA",
        Origen = origin,
        Mensaje = message,
        ProductoId = product.ProductId,
        ProductoPresentacionId = product.PresentationId,
        ProductoNombre = product.ProductName,
        PresentacionNombre = product.PresentationName,
        FactorConversion = product.Factor,
        ManejaLotes = product.HandlesLots,
        ManejaSeries = product.HandlesSeries,
        ManejaFechaCaducidad = product.HandlesExpiration
    };

    private static LineaCompraResueltaDto Suggested(
        LineaCompraAResolverDto line,
        IReadOnlyList<ProductCandidate> products,
        string origin,
        string message,
        decimal? fixedScore,
        IReadOnlyList<decimal>? scores = null) => new()
    {
        Orden = line.Orden,
        Estado = "SUGERIDA",
        Origen = origin,
        Mensaje = message,
        Candidatos = products.Select((x, index) => new CandidatoProductoCompraDto
        {
            ProductoId = x.ProductId,
             ProductoPresentacionId = x.PresentationId,
             ProductoNombre = x.ProductName,
             MarcaNombre = x.BrandName,
             PresentacionNombre = x.PresentationName,
             PresentacionCodigo = x.PresentationCode,
            CodigoBarras = x.Barcode,
            FactorConversion = x.Factor,
            EsPresentacionBase = x.IsBase,
            ManejaLotes = x.HandlesLots,
            ManejaSeries = x.HandlesSeries,
            ManejaFechaCaducidad = x.HandlesExpiration,
            Confianza = fixedScore ?? scores![index],
            Motivo = origin
        }).ToList()
    };

    private static bool IsValid(ProductCandidate candidate) =>
        candidate.ProductActive && candidate.PresentationActive &&
        candidate.AllowsPurchase;

    private static Expression<Func<ProductoPresentacion, bool>>
        BuildTextPredicate(IReadOnlyList<string> terms)
    {
        var parameter = Expression.Parameter(typeof(ProductoPresentacion), "p");
        Expression body = Expression.Constant(false);
        var product = Expression.Property(parameter, nameof(ProductoPresentacion.Producto));
        foreach (var term in terms)
        {
            var pattern = Expression.Constant($"%{term}%");
            body = Expression.OrElse(body,
                Ilike(Property(product, nameof(Producto.Nombre)), pattern));
            body = Expression.OrElse(body,
                Ilike(Property(Property(product, nameof(Producto.Marca)),
                    nameof(KONTAXPRO.Domain.Entities.Catalogos.Marca.Nombre)),
                    pattern));
            body = Expression.OrElse(body,
                Ilike(Property(product, nameof(Producto.Modelo)), pattern));
            body = Expression.OrElse(body,
                Ilike(Property(product, nameof(Producto.Descripcion)), pattern));
            body = Expression.OrElse(body,
                Ilike(Expression.Property(parameter,
                    nameof(ProductoPresentacion.Nombre)), pattern));
            body = Expression.OrElse(body,
                Ilike(Expression.Property(parameter,
                    nameof(ProductoPresentacion.Codigo)), pattern));
        }
        return Expression.Lambda<Func<ProductoPresentacion, bool>>(
            body, parameter);
    }

    private static Expression Property(Expression parent, string name) =>
        Expression.Property(parent, name);

    private static Expression Ilike(Expression value, Expression pattern) =>
        Expression.Call(
            typeof(NpgsqlDbFunctionsExtensions),
            nameof(NpgsqlDbFunctionsExtensions.ILike),
            Type.EmptyTypes,
            Expression.Property(null, typeof(EF), nameof(EF.Functions)),
            value,
            pattern);

    private async Task<bool> HasPermissionAsync(
        KontaxDbContext context,
        long companyId,
        string permission,
        CancellationToken cancellationToken) =>
        await context.UsuariosEmpresasRoles.AsNoTracking().AnyAsync(x =>
            x.UsuarioEmpresa!.UsuarioId == currentSession.UsuarioId &&
            x.UsuarioEmpresa.EmpresaId == companyId &&
            x.UsuarioEmpresa.Estado == 1 && x.Rol!.Estado == 1 &&
            (x.Rol.Codigo == "ADMINISTRADOR" ||
             x.Rol.RolesPermisos.Any(rp => rp.Permiso!.Codigo == permission &&
                                           rp.Permiso.Estado == 1)),
            cancellationToken);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class ProductCandidate
    {
        public long ProductId { get; init; }
         public long PresentationId { get; init; }
         public string ProductName { get; init; } = string.Empty;
         public string? BrandName { get; init; }
         public string? Model { get; init; }
        public string? Description { get; init; }
        public string PresentationName { get; init; } = string.Empty;
        public string PresentationCode { get; init; } = string.Empty;
        public string? Barcode { get; init; }
        public decimal Factor { get; init; }
        public bool IsBase { get; init; }
        public bool HandlesLots { get; init; }
        public bool HandlesSeries { get; init; }
        public bool HandlesExpiration { get; init; }
        public bool ProductActive { get; init; }
        public bool PresentationActive { get; init; }
        public bool AllowsPurchase { get; init; }
    }

    private sealed class EquivalenceCandidate
    {
        public string NormalizedCode { get; init; } = string.Empty;
        public string Type { get; init; } = string.Empty;
        public required ProductCandidate Product { get; init; }
    }
}
