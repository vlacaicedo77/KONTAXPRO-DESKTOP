using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KONTAXPRO.Tests.PostgreSql;

public sealed class ConfiguracionSriPostgreSqlTests
{
    [PostgreSqlFact]
    [Trait("Category", "PostgreSQL")]
    public async Task Configuracion_usa_xmin_bloqueo_y_contexto_autorizado()
    {
        await PostgreSqlTestDatabase.EnsureReadyAsync();
        var ids = await CrearContextoAsync();
        try
        {
            var service = CrearServicio();
            var guardada = await service.GuardarAmbienteAsync(
                ids.EmpresaId, ids.UsuarioId, ids.EstablecimientoId,
                ids.PuntoEmisionId, "PRUEBAS", false, 0);

            Assert.NotEqual(0u, guardada.Version);
            var conflicto = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.GuardarAmbienteAsync(ids.EmpresaId,
                    ids.UsuarioId, ids.EstablecimientoId, ids.PuntoEmisionId,
                    "PRUEBAS", false, 0));
            Assert.Contains("modificada por otro usuario", conflicto.Message,
                StringComparison.OrdinalIgnoreCase);

            var diagnostico = await service.DiagnosticarAsync(ids.EmpresaId,
                ids.UsuarioId, ids.EstablecimientoId, ids.PuntoEmisionId);
            Assert.True(diagnostico.Items.Single(x =>
                x.Codigo == "ESTABLECIMIENTO").Correcto);
            Assert.True(diagnostico.Items.Single(x =>
                x.Codigo == "PUNTO_EMISION").Correcto);
        }
        finally
        {
            await LimpiarAsync(ids);
        }
    }

    private static async Task<IdsPrueba> CrearContextoAsync()
    {
        await using var context = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());
        var now = DateTime.UtcNow;
        var regimenId = await context.RegimenesTributarios.AsNoTracking()
            .Select(x => x.Id).FirstAsync();
        var adminId = await context.Roles.AsNoTracking()
            .Where(x => x.Codigo == "ADMINISTRADOR")
            .Select(x => x.Id).SingleAsync();
        var suffix = Random.Shared.Next(100_000_000, 999_999_999)
            .ToString(System.Globalization.CultureInfo.InvariantCulture);
        var empresa = new Empresa
        {
            RegimenTributarioId = regimenId,
            NumeroIdentificacion = $"99{suffix}01",
            RazonSocial = "EMPRESA SRI RELACIONAL DE PRUEBA",
            Estado = 1,
            CreatedAt = now
        };
        var usuario = new Usuario
        {
            NumeroIdentificacion = $"USR{Guid.NewGuid():N}"[..20],
            NombreCompleto = "USUARIO SRI RELACIONAL",
            PasswordHash = "PRUEBA",
            Estado = 1,
            CreatedAt = now
        };
        context.AddRange(empresa, usuario);
        await context.SaveChangesAsync();

        var establecimiento = new Establecimiento
        {
            EmpresaId = empresa.Id,
            Codigo = "001",
            Prefijo = "001",
            Nombre = "MATRIZ SRI TEST",
            Direccion = "DIRECCION DE PRUEBA",
            EsMatriz = true,
            Estado = 1,
            CreatedAt = now
        };
        var usuarioEmpresa = new UsuarioEmpresa
        {
            UsuarioId = usuario.Id,
            EmpresaId = empresa.Id,
            Estado = 1,
            CreatedAt = now
        };
        context.AddRange(establecimiento, usuarioEmpresa);
        await context.SaveChangesAsync();

        var punto = new PuntoEmision
        {
            EstablecimientoId = establecimiento.Id,
            Codigo = "100",
            Nombre = "PUNTO SRI TEST",
            Estado = 1,
            CreatedAt = now
        };
        context.AddRange(
            punto,
            new UsuarioEmpresaRol
            {
                UsuarioEmpresaId = usuarioEmpresa.Id,
                RolId = adminId,
                CreatedAt = now
            },
            new UsuarioEmpresaEstablecimiento
            {
                UsuarioEmpresaId = usuarioEmpresa.Id,
                EstablecimientoId = establecimiento.Id,
                CreatedAt = now
            });
        await context.SaveChangesAsync();
        return new(empresa.Id, usuario.Id, usuarioEmpresa.Id,
            establecimiento.Id, punto.Id);
    }

    private static ConfiguracionFacturacionElectronicaService CrearServicio() =>
        new(PostgreSqlTestDatabase.Factory, new ValidatorFake(),
            new StorageFake(), Options.Create(new SriEndpointsOptions()),
            new ContextFake(), new SchedulerFake(),
            NullLogger<ConfiguracionFacturacionElectronicaService>.Instance);

    private static async Task LimpiarAsync(IdsPrueba ids)
    {
        await using var context = new KontaxDbContext(
            PostgreSqlTestDatabase.CreateOptions());
        await context.Auditorias.Where(x => x.EmpresaId == ids.EmpresaId)
            .ExecuteDeleteAsync();
        await context.FacturacionesElectronicas
            .Where(x => x.EmpresaId == ids.EmpresaId).ExecuteDeleteAsync();
        await context.UsuariosEmpresasEstablecimientos
            .Where(x => x.UsuarioEmpresaId == ids.UsuarioEmpresaId)
            .ExecuteDeleteAsync();
        await context.UsuariosEmpresasRoles
            .Where(x => x.UsuarioEmpresaId == ids.UsuarioEmpresaId)
            .ExecuteDeleteAsync();
        await context.UsuariosEmpresas
            .Where(x => x.Id == ids.UsuarioEmpresaId).ExecuteDeleteAsync();
        await context.PuntosEmision.Where(x => x.Id == ids.PuntoEmisionId)
            .ExecuteDeleteAsync();
        await context.Establecimientos
            .Where(x => x.Id == ids.EstablecimientoId).ExecuteDeleteAsync();
        await context.Empresas.Where(x => x.Id == ids.EmpresaId)
            .ExecuteDeleteAsync();
        await context.Usuarios.Where(x => x.Id == ids.UsuarioId)
            .ExecuteDeleteAsync();
    }

    private sealed record IdsPrueba(
        long EmpresaId,
        long UsuarioId,
        long UsuarioEmpresaId,
        long EstablecimientoId,
        long PuntoEmisionId);

    private sealed class ValidatorFake : IValidadorCertificadoSri
    {
        public ResultadoValidacionCertificadoSri Validar(
            ReadOnlyMemory<byte> pkcs12, ReadOnlySpan<char> password,
            string? rucEsperado = null) =>
            new(false, null, ["No configurado"]);
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
            throw new FileNotFoundException();

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
