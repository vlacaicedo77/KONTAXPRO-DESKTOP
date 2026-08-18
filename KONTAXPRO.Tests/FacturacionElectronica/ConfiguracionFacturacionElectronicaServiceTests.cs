using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class ConfiguracionFacturacionElectronicaServiceTests
{
    [Fact]
    public async Task Empresa_sin_configuracion_abre_con_valores_iniciales_sin_mutar()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        await using (var context = factory.CreateDbContext())
        {
            context.Empresas.Add(new Empresa
            {
                Id = 7,
                RegimenTributarioId = 1,
                NumeroIdentificacion = "1790012345001",
                RazonSocial = "EMPRESA DE PRUEBA",
                Estado = 1,
                CreatedAt = DateTime.UtcNow
            });
            context.TiposAmbiente.Add(new TipoAmbiente
            {
                Id = 1, Codigo = 1, Nombre = "PRUEBAS", Estado = 1,
                CreatedAt = DateTime.UtcNow
            });
            context.TiposEmision.Add(new TipoEmision
            {
                Id = 1, Codigo = 1, Nombre = "NORMAL", Estado = 1,
                CreatedAt = DateTime.UtcNow
            });
            context.Usuarios.Add(new Usuario
            {
                Id = 9, NumeroIdentificacion = "1700000000",
                NombreCompleto = "USUARIO PRUEBA", PasswordHash = "x",
                Estado = 1, CreatedAt = DateTime.UtcNow
            });
            context.UsuariosEmpresas.Add(new UsuarioEmpresa
            {
                Id = 10, UsuarioId = 9, EmpresaId = 7, Estado = 1,
                CreatedAt = DateTime.UtcNow
            });
            context.Roles.Add(new Rol
            {
                Id = 11, Codigo = "ADMIN", Nombre = "ADMIN",
                Estado = 1, CreatedAt = DateTime.UtcNow
            });
            context.Permisos.Add(new Permiso
            {
                Id = 12, Codigo = "SRI_EJECUTAR_DIAGNOSTICO",
                Nombre = "Diagnosticar", Modulo = "SRI", Estado = 1,
                CreatedAt = DateTime.UtcNow
            });
            context.RolesPermisos.Add(new RolPermiso
            {
                Id = 13, RolId = 11, PermisoId = 12,
                CreatedAt = DateTime.UtcNow
            });
            context.UsuariosEmpresasRoles.Add(new UsuarioEmpresaRol
            {
                Id = 14, UsuarioEmpresaId = 10, RolId = 11,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }
        var service = new ConfiguracionFacturacionElectronicaService(
            factory, new ValidatorFake(), new StorageFake(),
            Options.Create(new SriEndpointsOptions()), new ContextFake(),
            new SchedulerFake(),
            NullLogger<ConfiguracionFacturacionElectronicaService>.Instance);

        var result = await service.ObtenerAsync(7, 9);

        Assert.Equal("PRUEBAS", result.AmbienteCodigo);
        Assert.Equal("NORMAL", result.TipoEmisionCodigo);
        Assert.False(result.Habilitada);
        await using var verification = factory.CreateDbContext();
        Assert.Empty(verification.FacturacionesElectronicas);
    }

    private sealed class TestDbContextFactory(
        DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);
    }

    private sealed class ValidatorFake : IValidadorCertificadoSri
    {
        public ResultadoValidacionCertificadoSri Validar(
            ReadOnlyMemory<byte> pkcs12, ReadOnlySpan<char> password,
            string? rucEsperado = null) => new(false, null, []);
    }

    private sealed class StorageFake : IAlmacenamientoCertificadoSri
    {
        public Task<string> GuardarAsync(long empresaId,
            ReadOnlyMemory<byte> pkcs12, string password,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<MaterialCertificadoSri> LeerAsync(long empresaId,
            string referencia,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task EliminarAsync(long empresaId, string referencia,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ContextFake : IContextoInstalacion
    {
        public Guid InstalacionId { get; } = Guid.NewGuid();
        public string TipoInstalacion => "SERVIDOR";
        public string DirectorioBase => Path.GetTempPath();
    }

    private sealed class SchedulerFake : IFacturacionElectronicaWorkerScheduler
    {
        public bool EstaEjecutandose => true;
        public Task IniciarAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
        public Task DetenerAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
