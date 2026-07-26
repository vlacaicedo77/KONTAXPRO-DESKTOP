using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Common;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.Products;

public class ProductCatalogService : IProductCatalogService
{
    private readonly IDbContextFactory<KontaxDbContext> _dbContextFactory;

    public ProductCatalogService(
        IDbContextFactory<KontaxDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<CatalogItemDto>> ObtenerCategoriasAsync(
        long empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.CategoriasProducto
            .AsNoTracking()
            .Where(x =>
                x.EmpresaId == empresaId &&
                x.Estado == 1)
            .OrderBy(x => x.Nombre)
            .Select(x => new CatalogItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogItemDto>> ObtenerMarcasAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.Marcas
            .AsNoTracking()
            .Where(x => x.Estado == 1)
            .OrderBy(x => x.Nombre)
            .Select(x => new CatalogItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogItemDto>> ObtenerUnidadesMedidaAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.UnidadesMedida
            .AsNoTracking()
            .Where(x => x.Estado == 1)
            .OrderBy(x => x.Nombre)
            .Select(x => new CatalogItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<CatalogItemDto>> ObtenerTarifasImpuestoAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        return await context.TarifasImpuesto
            .AsNoTracking()
            .Where(x => x.Estado == 1)
            .OrderBy(x => x.Porcentaje)
            .Select(x => new CatalogItemDto
            {
                Id = x.Id,
                Nombre = x.Nombre
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CatalogCreateResult> CrearCategoriaAsync(
        long empresaId,
        string nombre,
        CancellationToken cancellationToken = default)
    {
        nombre = nombre.Trim();

        if (empresaId <= 0)
        {
            return CatalogCreateResult.Fail(
                "No existe una empresa activa.");
        }

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return CatalogCreateResult.Fail(
                "Debe ingresar el nombre de la categoría.");
        }

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var existente = await context.CategoriasProducto
            .FirstOrDefaultAsync(
                x =>
                    x.EmpresaId == empresaId &&
                    x.Nombre.ToLower() == nombre.ToLower(),
                cancellationToken);

        if (existente is not null)
        {
            if (existente.Estado == 0)
            {
                existente.Estado = 1;
                existente.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync(
                    cancellationToken);
            }

            return CatalogCreateResult.Ok(
                new CatalogItemDto
                {
                    Id = existente.Id,
                    Nombre = existente.Nombre
                },
                "La categoría ya existía y fue seleccionada.");
        }

        var categoria = new CategoriaProducto
        {
            EmpresaId = empresaId,
            Codigo = CrearCodigoCatalogo(nombre),
            Nombre = nombre,
            Estado = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.CategoriasProducto.Add(categoria);

        await context.SaveChangesAsync(
            cancellationToken);

        return CatalogCreateResult.Ok(
            new CatalogItemDto
            {
                Id = categoria.Id,
                Nombre = categoria.Nombre
            },
            "Categoría creada correctamente.");
    }

    public async Task<CatalogCreateResult> CrearMarcaAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        nombre = nombre.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
        {
            return CatalogCreateResult.Fail(
                "Debe ingresar el nombre de la marca.");
        }

        await using var context =
            await _dbContextFactory.CreateDbContextAsync(
                cancellationToken);

        var existente = await context.Marcas
            .FirstOrDefaultAsync(
                x => x.Nombre.ToLower() == nombre.ToLower(),
                cancellationToken);

        if (existente is not null)
        {
            if (existente.Estado == 0)
            {
                existente.Estado = 1;
                existente.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync(
                    cancellationToken);
            }

            return CatalogCreateResult.Ok(
                new CatalogItemDto
                {
                    Id = existente.Id,
                    Nombre = existente.Nombre
                },
                "La marca ya existía y fue seleccionada.");
        }

        var marca = new Marca
        {
            Codigo = CrearCodigoCatalogo(nombre),
            Nombre = nombre,
            Estado = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Marcas.Add(marca);

        await context.SaveChangesAsync(
            cancellationToken);

        return CatalogCreateResult.Ok(
            new CatalogItemDto
            {
                Id = marca.Id,
                Nombre = marca.Nombre
            },
            "Marca creada correctamente.");
    }

    private static string CrearCodigoCatalogo(string nombre)
    {
        var caracteres = nombre
            .Trim()
            .ToUpperInvariant()
            .Select(x => char.IsLetterOrDigit(x) ? x : '_')
            .ToArray();

        var codigo = string.Concat(caracteres);

        while (codigo.Contains("__", StringComparison.Ordinal))
        {
            codigo = codigo.Replace("__", "_", StringComparison.Ordinal);
        }

        return codigo.Trim('_');
    }
}
