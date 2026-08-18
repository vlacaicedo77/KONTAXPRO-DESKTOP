using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Persistence;
using KONTAXPRO.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.Seguridad;

public sealed class UsuarioEmpresaServiceTests
{
    [Fact]
    public async Task CambiarEstablecimientoActualizaPreferenciaYContextoOperativo()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        var now = DateTime.UtcNow;

        await using (var db = factory.CreateDbContext())
        {
            var company = new Empresa
            {
                Id = 10,
                RegimenTributarioId = 1,
                NumeroIdentificacion = "1724204787001",
                RazonSocial = "EMPRESA",
                Estado = 1,
                CreatedAt = now
            };
            var user = new Usuario
            {
                Id = 20,
                NumeroIdentificacion = "1724853377",
                NombreCompleto = "USUARIO",
                PasswordHash = "HASH",
                Estado = 1,
                CreatedAt = now
            };
            var first = new Establecimiento
            {
                Id = 30,
                EmpresaId = company.Id,
                Codigo = "001",
                Prefijo = "001",
                Nombre = "MATRIZ",
                NombreComercial = "MULTISERVICIOS JR",
                Direccion = "DIRECCION 1",
                EsMatriz = true,
                Estado = 1,
                CreatedAt = now
            };
            var second = new Establecimiento
            {
                Id = 31,
                EmpresaId = company.Id,
                Codigo = "002",
                Prefijo = "002",
                Nombre = "ESTABLECIMIENTO 002",
                NombreComercial = "AGROVETERINARIA JR",
                Direccion = "DIRECCION 2",
                Estado = 1,
                CreatedAt = now
            };
            var userCompany = new UsuarioEmpresa
            {
                Id = 40,
                UsuarioId = user.Id,
                EmpresaId = company.Id,
                Estado = 1,
                CreatedAt = now
            };
            var firstPoint = new PuntoEmision
            {
                Id = 50,
                EstablecimientoId = first.Id,
                Codigo = "001",
                Nombre = "PUNTO 001",
                Estado = 1,
                CreatedAt = now
            };
            var secondPoint = new PuntoEmision
            {
                Id = 51,
                EstablecimientoId = second.Id,
                Codigo = "001",
                Nombre = "PUNTO 002",
                Estado = 1,
                CreatedAt = now
            };
            var firstWarehouse = Warehouse(60, first.Id, now);
            var secondWarehouse = Warehouse(61, second.Id, now);

            db.AddRange(company, user, first, second, userCompany,
                firstPoint, secondPoint, firstWarehouse, secondWarehouse);
            db.UsuariosEmpresasEstablecimientos.AddRange(
                new UsuarioEmpresaEstablecimiento
                {
                    UsuarioEmpresaId = userCompany.Id,
                    EstablecimientoId = first.Id,
                    CreatedAt = now
                },
                new UsuarioEmpresaEstablecimiento
                {
                    UsuarioEmpresaId = userCompany.Id,
                    EstablecimientoId = second.Id,
                    CreatedAt = now
                });
            db.UsuariosConfiguracionesEmpresa.Add(
                new UsuarioConfiguracionEmpresa
                {
                    UsuarioId = user.Id,
                    EmpresaId = company.Id,
                    EstablecimientoId = first.Id,
                    PuntoEmisionId = firstPoint.Id,
                    BodegaId = firstWarehouse.Id,
                    CreatedAt = now
                });
            await db.SaveChangesAsync();
        }

        var session = new CurrentSession
        {
            UsuarioId = 20,
            EmpresaId = 10,
            EstablecimientoId = 30,
            CajaId = 99,
            CajaSesionId = 100
        };
        var service = new UsuarioEmpresaService(factory, session);
        var available = await service.ObtenerEstablecimientosUsuarioAsync(
            session.UsuarioId, session.EmpresaId.Value);

        Assert.Equal(2, available.Count);
        await service.SeleccionarEstablecimientoAsync(31);

        Assert.Equal(31, session.EstablecimientoId);
        Assert.Equal("002", session.EstablecimientoCodigo);
        Assert.Equal("AGROVETERINARIA JR", session.EstablecimientoNombre);
        Assert.Equal(51, session.PuntoEmisionId);
        Assert.Equal(61, session.BodegaId);
        Assert.Null(session.CajaId);
        Assert.Null(session.CajaSesionId);
        Assert.Equal(2, session.CantidadEstablecimientosDisponibles);

        await using var verification = factory.CreateDbContext();
        var preference = await verification.UsuariosConfiguracionesEmpresa
            .SingleAsync();
        Assert.Equal(31, preference.EstablecimientoId);
        Assert.Equal(51, preference.PuntoEmisionId);
        Assert.Equal(61, preference.BodegaId);
    }

    private static Bodega Warehouse(long id, long establishmentId,
        DateTime now) => new()
    {
        Id = id,
        EstablecimientoId = establishmentId,
        Codigo = "FAC",
        Nombre = "PRODUCTOS CON FACTURA",
        PermiteVentaFacturada = true,
        Estado = 1,
        CreatedAt = now
    };

    private sealed class TestDbContextFactory(
        DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);
    }
}
