using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ConfiguracionSri =
    KONTAXPRO.Domain.Entities.Configuracion.FacturacionElectronica;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class ConfiguracionFacturacionElectronicaSecurityTests
{
    [Fact]
    public async Task Lectura_rechaza_usuario_sin_acceso_SRI_en_empresa()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, []);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ObtenerAsync(7, 9));
    }

    [Fact]
    public async Task Cambio_de_ambiente_es_permiso_complementario_y_no_habilita_lectura()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_CAMBIAR_AMBIENTE"]);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ObtenerAsync(7, 9));
    }

    [Fact]
    public async Task Cambio_real_de_ambiente_exige_permiso_especifico()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_CONFIGURAR_FACTURACION"],
            crearConfiguracion: true);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GuardarAmbienteAsync(7, 9, null, null,
                "PRODUCCION", false, 0));
    }

    [Fact]
    public async Task Habilitar_rechaza_configuracion_incompleta()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_CONFIGURAR_FACTURACION"],
            crearConfiguracion: true);
        var service = CrearServicio(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GuardarAmbienteAsync(7, 9, null, null,
                "PRUEBAS", true, 0));

        Assert.Contains("No se puede habilitar", exception.Message);
        await using var context = factory.CreateDbContext();
        Assert.False((await context.FacturacionesElectronicas.SingleAsync())
            .Habilitada);
        Assert.Empty(context.Auditorias);
    }

    [Fact]
    public async Task Version_desactualizada_no_sobrescribe_configuracion()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_CONFIGURAR_FACTURACION"],
            crearConfiguracion: true);
        var service = CrearServicio(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GuardarAmbienteAsync(7, 9, null, null,
                "PRUEBAS", false, 99));

        Assert.Contains("modificada por otro usuario", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Nodo_cliente_no_puede_importar_certificado()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_CAMBIAR_CERTIFICADO"]);
        var storage = new StorageSpy();
        var service = CrearServicio(factory, servidor: false,
            storage: storage);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ImportarCertificadoAsync(7, 9, "firma.p12",
                new byte[] { 1 }, "clave", 0));

        Assert.Contains("SERVIDOR", exception.Message);
        Assert.Equal(0, storage.Guardados);
    }

    [Fact]
    public async Task Diagnostico_no_sustituye_contexto_activo_por_otro_existente()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_EJECUTAR_DIAGNOSTICO"],
            crearConfiguracion: true, crearContextoAlterno: true);
        var service = CrearServicio(factory);

        var result = await service.DiagnosticarAsync(7, 9, null, null);

        Assert.False(result.Items.Single(x =>
            x.Codigo == "ESTABLECIMIENTO").Correcto);
        Assert.False(result.Items.Single(x =>
            x.Codigo == "PUNTO_EMISION").Correcto);
    }

    [Fact]
    public async Task Configuracion_y_diagnostico_rechazan_establecimiento_no_autorizado()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory,
            ["SRI_CONFIGURAR_FACTURACION", "SRI_EJECUTAR_DIAGNOSTICO"],
            crearConfiguracion: true, crearContextoAlterno: true);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GuardarAmbienteAsync(7, 9, 40, 41,
                "PRUEBAS", false, 0));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.DiagnosticarAsync(7, 9, 40, 41));
    }

    [Fact]
    public void Endpoints_aceptan_solo_https_y_dominio_oficial_del_ambiente()
    {
        var options = new SriEndpointsOptions();
        Assert.Equal("celcer.sri.gob.ec", options.Recepcion(1).Host);
        Assert.Equal("cel.sri.gob.ec", options.Autorizacion(2).Host);

        options.RecepcionPruebas =
            "https://celcer.sri.gob.ec.ejemplo.invalid/servicio";
        Assert.Throws<InvalidOperationException>(() => options.Recepcion(1));
        options.RecepcionPruebas =
            "http://celcer.sri.gob.ec/servicio";
        Assert.Throws<InvalidOperationException>(() => options.Recepcion(1));
        options.RecepcionPruebas =
            "https://celcer.sri.gob.ec/ruta-inventada/RecepcionComprobantesOffline";
        Assert.Throws<InvalidOperationException>(() => options.Recepcion(1));
        options.RecepcionPruebas =
            "https://celcer.sri.gob.ec/comprobantes-electronicos-ws/RecepcionComprobantesOffline?otro=1";
        Assert.Throws<InvalidOperationException>(() => options.Recepcion(1));
    }

    [Fact]
    public async Task Numeraciones_exigen_permiso_especifico()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, [], crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ObtenerNumeracionesAsync(7, 9, "PRUEBAS"));
    }

    [Fact]
    public async Task Numeracion_inicial_se_actualiza_y_audita_en_contexto_autorizado()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        var numeraciones = await service.ObtenerNumeracionesAsync(
            7, 9, "PRUEBAS");

        var numeracion = Assert.Single(numeraciones);
        Assert.Equal("001-001-000000001", numeracion.NumeroCompletoProximo);
        Assert.False(numeracion.TieneComprobantesKontax);

        await service.ActualizarUltimoSecuencialAsync(
            7, 9, numeracion.Id, 0, 527);

        await using var context = factory.CreateDbContext();
        Assert.Equal(527, (await context.SecuencialesComprobantes
            .SingleAsync()).UltimoSecuencial);
        var auditoria = await context.Auditorias.SingleAsync();
        Assert.Equal("SRI_CONFIGURACION_SECUENCIAL", auditoria.Accion);
        Assert.Equal(40, auditoria.EstablecimientoId);
        Assert.DoesNotContain("1790012345001", auditoria.Descripcion ?? "");
    }

    [Fact]
    public async Task Numeracion_en_uso_no_puede_reconfigurarse()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory, crearComprobante: true);
        var service = CrearServicio(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ActualizarUltimoSecuencialAsync(7, 9, 60, 0, 1));

        Assert.Contains("ya emitió", exception.Message,
            StringComparison.OrdinalIgnoreCase);
        await using var context = factory.CreateDbContext();
        Assert.Equal(0, (await context.SecuencialesComprobantes
            .SingleAsync()).UltimoSecuencial);
        Assert.Empty(context.Auditorias);
    }

    [Fact]
    public async Task Numeracion_de_establecimiento_no_autorizado_no_se_expone_ni_edita()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory,
            autorizarEstablecimiento: false);
        var service = CrearServicio(factory);

        Assert.Empty(await service.ObtenerNumeracionesAsync(
            7, 9, "PRUEBAS"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ActualizarUltimoSecuencialAsync(7, 9, 60, 0, 1));
    }

    [Fact]
    public async Task Numeracion_con_valor_esperado_desactualizado_no_se_sobrescribe()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ActualizarUltimoSecuencialAsync(7, 9, 60, 7, 12));

        Assert.Contains("modificada por otro usuario", exception.Message,
            StringComparison.OrdinalIgnoreCase);
        await using var context = factory.CreateDbContext();
        Assert.Equal(0, (await context.SecuencialesComprobantes
            .SingleAsync()).UltimoSecuencial);
        Assert.Empty(context.Auditorias);
    }

    [Fact]
    public async Task Crear_punto_genera_todas_las_numeraciones_y_preferencia()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        await service.CrearPuntoEmisionAsync(7, 9,
            SolicitudPuntoNuevo(40, "100", "", true));

        var administracion = await service.ObtenerPuntosEmisionAsync(7, 9);
        Assert.Equal(2, administracion.Puntos.Count);
        Assert.Equal(2, administracion.PlantillasSecuenciales.Count);
        Assert.True(administracion.Puntos.Single(x =>
            x.Codigo == "100").EsPredeterminado);

        await using var context = factory.CreateDbContext();
        var punto = await context.PuntosEmision.SingleAsync(x =>
            x.Codigo == "100");
        Assert.Equal("PUNTO DE EMISIÓN 100", punto.Nombre);
        Assert.Equal(1, punto.Estado);
        var secuenciales = await context.SecuencialesComprobantes
            .Where(x => x.PuntoEmisionId == punto.Id)
            .OrderBy(x => x.TipoAmbienteId)
            .ToListAsync();
        Assert.Equal(2, secuenciales.Count);
        Assert.All(secuenciales, x => Assert.Equal(0, x.UltimoSecuencial));
        Assert.Equal(punto.Id, (await context.UsuariosConfiguracionesEmpresa
            .SingleAsync()).PuntoEmisionId);
        Assert.Contains(await context.Auditorias.ToListAsync(), x =>
            x.Accion == "SRI_CREACION_PUNTO_EMISION" &&
            x.EntidadId == punto.Id);
    }

    [Fact]
    public async Task Punto_duplicado_o_establecimiento_no_autorizado_se_rechaza()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory,
            autorizarEstablecimiento: false);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CrearPuntoEmisionAsync(7, 9,
                SolicitudPuntoNuevo(40, "100", "PUNTO", false)));

        await using (var context = factory.CreateDbContext())
        {
            context.UsuariosEmpresasEstablecimientos.Add(
                new UsuarioEmpresaEstablecimiento
                {
                    Id = 42,
                    UsuarioEmpresaId = 10,
                    EstablecimientoId = 40,
                    CreatedAt = DateTime.UtcNow
                });
            await context.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CrearPuntoEmisionAsync(7, 9,
                SolicitudPuntoNuevo(40, "1", "OTRO PUNTO", false)));
        Assert.Contains("ya existe", exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Inactivar_punto_conserva_registro_y_reasigna_preferencia()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CambiarEstadoPuntoEmisionAsync(7, 9, 41, false));
        await service.CrearPuntoEmisionAsync(7, 9,
            SolicitudPuntoNuevo(40, "100", "PUNTO KONTAXPRO", true));
        long punto100Id;
        await using (var read = factory.CreateDbContext())
            punto100Id = await read.PuntosEmision
                .Where(x => x.Codigo == "100").Select(x => x.Id)
                .SingleAsync();

        await service.CambiarEstadoPuntoEmisionAsync(7, 9, punto100Id,
            false);

        await using var context = factory.CreateDbContext();
        Assert.Equal(0, (await context.PuntosEmision.SingleAsync(x =>
            x.Id == punto100Id)).Estado);
        Assert.Equal(41, (await context.UsuariosConfiguracionesEmpresa
            .SingleAsync()).PuntoEmisionId);
        Assert.True(await context.SecuencialesComprobantes.AnyAsync(x =>
            x.PuntoEmisionId == punto100Id));
        Assert.Contains(await context.Auditorias.ToListAsync(), x =>
            x.Accion == "SRI_INACTIVACION_PUNTO_EMISION");
    }

    [Fact]
    public async Task Continuar_punto_existente_conserva_ultimo_numero_por_ambiente()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        await service.CrearPuntoEmisionAsync(7, 9,
            new CrearPuntoEmisionSriDto(40, "100", "CAJA ANTIGUA", false,
                true,
                [
                    new SecuencialInicialPuntoEmisionSriDto(50, 1, 845),
                    new SecuencialInicialPuntoEmisionSriDto(50, 2, 19)
                ]));

        await using var context = factory.CreateDbContext();
        var puntoId = await context.PuntosEmision
            .Where(x => x.Codigo == "100")
            .Select(x => x.Id)
            .SingleAsync();
        var secuenciales = await context.SecuencialesComprobantes
            .Where(x => x.PuntoEmisionId == puntoId)
            .OrderBy(x => x.TipoAmbienteId)
            .Select(x => x.UltimoSecuencial)
            .ToArrayAsync();
        Assert.Equal([845, 19], secuenciales);
        Assert.Contains(await context.Auditorias.ToListAsync(), x =>
            x.Accion == "SRI_CREACION_PUNTO_EMISION" &&
            x.Descripcion!.Contains("continuidad configurada"));
    }

    [Fact]
    public async Task Continuidad_incompleta_se_rechaza_sin_crear_punto()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CrearPuntoEmisionAsync(7, 9,
                new CrearPuntoEmisionSriDto(40, "100", "INCOMPLETO", false,
                    true,
                    [new SecuencialInicialPuntoEmisionSriDto(50, 1, 10)])));

        await using var context = factory.CreateDbContext();
        Assert.False(await context.PuntosEmision.AnyAsync(x =>
            x.Codigo == "100"));
        Assert.Empty(context.Auditorias);
    }

    [Fact]
    public async Task Renombrar_punto_cambia_solo_nombre_y_audita()
    {
        var factory = CrearFactory();
        await SembrarAsync(factory, ["SRI_ADMINISTRAR_SECUENCIALES"],
            crearContextoAlterno: true);
        await SembrarNumeracionAsync(factory);
        var service = CrearServicio(factory);

        await service.RenombrarPuntoEmisionAsync(7, 9, 41,
            "  caja principal  ");

        await using var context = factory.CreateDbContext();
        var punto = await context.PuntosEmision.SingleAsync(x => x.Id == 41);
        Assert.Equal("001", punto.Codigo);
        Assert.Equal("CAJA PRINCIPAL", punto.Nombre);
        Assert.Contains(await context.Auditorias.ToListAsync(), x =>
            x.Accion == "SRI_RENOMBRE_PUNTO_EMISION" && x.EntidadId == 41);
    }

    private static CrearPuntoEmisionSriDto SolicitudPuntoNuevo(
        long establecimientoId,
        string codigo,
        string nombre,
        bool predeterminado) =>
        new(establecimientoId, codigo, nombre, predeterminado, false, []);

    private static TestDbContextFactory CrearFactory()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new(options);
    }

    private static async Task SembrarAsync(
        TestDbContextFactory factory,
        IReadOnlyCollection<string> permisos,
        bool crearConfiguracion = false,
        bool crearContextoAlterno = false)
    {
        await using var context = factory.CreateDbContext();
        var now = DateTime.UtcNow;
        context.Empresas.Add(new Empresa
        {
            Id = 7,
            RegimenTributarioId = 1,
            NumeroIdentificacion = "1790012345001",
            RazonSocial = "EMPRESA DE PRUEBA",
            Estado = 1,
            CreatedAt = now
        });
        context.TiposAmbiente.AddRange(
            new TipoAmbiente
            {
                Id = 1, Codigo = 1, Nombre = "PRUEBAS", Estado = 1,
                CreatedAt = now
            },
            new TipoAmbiente
            {
                Id = 2, Codigo = 2, Nombre = "PRODUCCION", Estado = 1,
                CreatedAt = now
            });
        context.TiposEmision.Add(new TipoEmision
        {
            Id = 1, Codigo = 1, Nombre = "NORMAL", Estado = 1,
            CreatedAt = now
        });
        context.Usuarios.Add(new Usuario
        {
            Id = 9,
            NumeroIdentificacion = "1700000000",
            NombreCompleto = "USUARIO PRUEBA",
            PasswordHash = "x",
            Estado = 1,
            CreatedAt = now
        });
        context.UsuariosEmpresas.Add(new UsuarioEmpresa
        {
            Id = 10, UsuarioId = 9, EmpresaId = 7, Estado = 1,
            CreatedAt = now
        });
        context.Roles.Add(new Rol
        {
            Id = 11, Codigo = "OPERADOR", Nombre = "OPERADOR",
            Estado = 1, CreatedAt = now
        });
        context.UsuariosEmpresasRoles.Add(new UsuarioEmpresaRol
        {
            Id = 12, UsuarioEmpresaId = 10, RolId = 11,
            CreatedAt = now
        });
        long id = 20;
        foreach (var codigo in permisos)
        {
            context.Permisos.Add(new Permiso
            {
                Id = id,
                Codigo = codigo,
                Nombre = codigo,
                Modulo = "SRI",
                Estado = 1,
                CreatedAt = now
            });
            context.RolesPermisos.Add(new RolPermiso
            {
                Id = id + 100,
                RolId = 11,
                PermisoId = id,
                CreatedAt = now
            });
            id++;
        }
        if (crearConfiguracion)
            context.FacturacionesElectronicas.Add(new ConfiguracionSri
            {
                Id = 30,
                EmpresaId = 7,
                TipoAmbienteId = 1,
                TipoEmisionId = 1,
                Habilitada = false,
                CreatedAt = now
            });
        if (crearContextoAlterno)
        {
            context.Establecimientos.Add(new Establecimiento
            {
                Id = 40, EmpresaId = 7, Codigo = "001", Prefijo = "001",
                Nombre = "MATRIZ", Direccion = "DIRECCION", EsMatriz = true,
                Estado = 1, CreatedAt = now
            });
            context.PuntosEmision.Add(new PuntoEmision
            {
                Id = 41, EstablecimientoId = 40, Codigo = "001",
                Nombre = "PUNTO 001", Estado = 1, CreatedAt = now
            });
        }
        await context.SaveChangesAsync();
    }

    private static async Task SembrarNumeracionAsync(
        TestDbContextFactory factory,
        bool crearComprobante = false,
        bool autorizarEstablecimiento = true)
    {
        await using var context = factory.CreateDbContext();
        var now = DateTime.UtcNow;
        if (autorizarEstablecimiento)
            context.UsuariosEmpresasEstablecimientos.Add(
                new UsuarioEmpresaEstablecimiento
                {
                    Id = 42,
                    UsuarioEmpresaId = 10,
                    EstablecimientoId = 40,
                    CreatedAt = now
                });
        context.TiposComprobante.Add(new TipoComprobante
        {
            Id = 50,
            CodigoSri = "01",
            Codigo = "FACTURA",
            Nombre = "FACTURA",
            Estado = 1,
            CreatedAt = now
        });
        context.SecuencialesComprobantes.Add(new SecuencialComprobante
        {
            Id = 60,
            PuntoEmisionId = 41,
            TipoComprobanteId = 50,
            TipoAmbienteId = 1,
            UltimoSecuencial = 0,
            CreatedAt = now
        });

        if (crearComprobante)
            context.ComprobantesElectronicos.Add(new ComprobanteElectronico
            {
                Id = 70,
                EmpresaId = 7,
                TipoComprobanteId = 50,
                TipoOrigenComprobanteElectronicoId = 1,
                OrigenId = 80,
                TipoAmbienteId = 1,
                TipoEmisionId = 1,
                EstablecimientoId = 40,
                PuntoEmisionId = 41,
                Secuencial = 1,
                ClaveAcceso = new string('1', 49),
                EstadoComprobanteElectronicoId = 1,
                CreatedAt = now
            });

        await context.SaveChangesAsync();
    }

    private static ConfiguracionFacturacionElectronicaService CrearServicio(
        TestDbContextFactory factory,
        bool servidor = true,
        StorageSpy? storage = null) => new(
        factory,
        new ValidatorFake(),
        storage ?? new StorageSpy(),
        Options.Create(new SriEndpointsOptions()),
        new ContextFake(servidor),
        new SchedulerFake(),
        NullLogger<ConfiguracionFacturacionElectronicaService>.Instance);

    private sealed class TestDbContextFactory(
        DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);
    }

    private sealed class ValidatorFake : IValidadorCertificadoSri
    {
        public ResultadoValidacionCertificadoSri Validar(
            ReadOnlyMemory<byte> pkcs12,
            ReadOnlySpan<char> password,
            string? rucEsperado = null) => new(false, null, ["No válido"]);
    }

    private sealed class StorageSpy : IAlmacenamientoCertificadoSri
    {
        public int Guardados { get; private set; }

        public Task<string> GuardarAsync(long empresaId,
            ReadOnlyMemory<byte> pkcs12, string password,
            CancellationToken cancellationToken = default)
        {
            Guardados++;
            return Task.FromResult("ref");
        }

        public Task<MaterialCertificadoSri> LeerAsync(long empresaId,
            string referencia,
            CancellationToken cancellationToken = default) =>
            throw new FileNotFoundException();

        public Task EliminarAsync(long empresaId, string referencia,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ContextFake(bool servidor) : IContextoInstalacion
    {
        public Guid InstalacionId { get; } = Guid.NewGuid();
        public string TipoInstalacion => servidor ? "SERVIDOR" : "CLIENTE";
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
