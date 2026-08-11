using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.Compras;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.Compras;

public sealed class CompraImportacionAccessTests
{
    [Fact]
    public async Task AnalizarXml_SinPermisoImportar_NoLeeElArchivo()
    {
        var factory = await CreateFactoryAsync("CONSULTA");
        var reader = new TrackingReader();
        var service = new CompraImportacionService(
            factory,
            reader,
            new ImportacionCompraStore(TimeProvider.System),
            new CurrentSession { UsuarioId = 3, EmpresaId = 7 },
            new ComprasRuntimeOptions { EsDevelopment = true },
            null!, null!, null!);

        var result = await service.AnalizarXmlAsync(
            new MemoryStream([1, 2, 3]), "factura.xml");

        Assert.False(result.Exito);
        Assert.Contains("autorización", result.Mensaje,
            StringComparison.OrdinalIgnoreCase);
        Assert.False(reader.Called);
    }

    [Fact]
    public async Task AnalizarXml_Administrador_PuedeIniciarLectura()
    {
        var factory = await CreateFactoryAsync("ADMINISTRADOR");
        var reader = new TrackingReader();
        var service = new CompraImportacionService(
            factory,
            reader,
            new ImportacionCompraStore(TimeProvider.System),
            new CurrentSession { UsuarioId = 3, EmpresaId = 7 },
            new ComprasRuntimeOptions { EsDevelopment = true },
            null!, null!, null!);

        var result = await service.AnalizarXmlAsync(
            new MemoryStream([1, 2, 3]), "factura.xml");

        Assert.False(result.Exito);
        Assert.True(reader.Called);
        Assert.Equal("No esperado", result.Mensaje);
    }

    [Fact]
    public async Task AnalizarXml_RolConPermiso_PuedeIniciarLectura()
    {
        var factory = await CreateFactoryAsync("COMPRADOR", true);
        var reader = new TrackingReader();
        var service = new CompraImportacionService(
            factory,
            reader,
            new ImportacionCompraStore(TimeProvider.System),
            new CurrentSession { UsuarioId = 3, EmpresaId = 7 },
            new ComprasRuntimeOptions { EsDevelopment = true },
            null!, null!, null!);

        await service.AnalizarXmlAsync(
            new MemoryStream([1, 2, 3]), "factura.xml");

        Assert.True(reader.Called);
    }

    private static async Task<TestDbContextFactory> CreateFactoryAsync(
        string roleCode,
        bool grantImportPermission = false)
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        await using var context = factory.CreateDbContext();
        var now = DateTime.UtcNow;
        context.Empresas.Add(new Empresa
        {
            Id = 7,
            RegimenTributarioId = 1,
            NumeroIdentificacion = "1799999999001",
            RazonSocial = "EMPRESA",
            Estado = 1,
            CreatedAt = now
        });
        context.UsuariosEmpresas.Add(new UsuarioEmpresa
        {
            Id = 11,
            UsuarioId = 3,
            EmpresaId = 7,
            Estado = 1,
            CreatedAt = now
        });
        context.Roles.Add(new Rol
        {
            Id = 13,
            Codigo = roleCode,
            Nombre = roleCode,
            Estado = 1,
            CreatedAt = now
        });
        context.UsuariosEmpresasRoles.Add(new UsuarioEmpresaRol
        {
            Id = 17,
            UsuarioEmpresaId = 11,
            RolId = 13,
            CreatedAt = now
        });
        if (grantImportPermission)
        {
            context.Permisos.Add(new Permiso
            {
                Id = 19,
                Codigo = ComprasPermissions.ImportarXml,
                Nombre = ComprasPermissions.ImportarXml,
                Modulo = "COMPRAS",
                Estado = 1,
                CreatedAt = now
            });
            context.RolesPermisos.Add(new RolPermiso
            {
                Id = 23,
                RolId = 13,
                PermisoId = 19,
                CreatedAt = now
            });
        }
        await context.SaveChangesAsync();
        return factory;
    }

    private sealed class TrackingReader : IComprobanteCompraXmlReader
    {
        public bool Called { get; private set; }

        public Task<LecturaComprobanteCompraResultado> LeerFacturaAsync(
            Stream contenido,
            string nombreArchivo,
            CancellationToken cancellationToken = default)
        {
            Called = true;
            return Task.FromResult(LecturaComprobanteCompraResultado.Fallo(
                ErrorLecturaComprobanteCompra.XmlMalFormado, "No esperado"));
        }
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);

        public Task<KontaxDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
