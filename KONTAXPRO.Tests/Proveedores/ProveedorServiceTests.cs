using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Models.Proveedores;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Infrastructure.Persistence;
using KONTAXPRO.Infrastructure.Proveedores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace KONTAXPRO.Tests.Proveedores;

public sealed class ProveedorServiceTests
{
    private static readonly Guid VerifiedProofId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OfflineProofId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");
    [Fact]
    public async Task NewSupplierCreatesGlobalRoleWithoutCompanyRelationship()
    {
        var factory = await CreateFactoryAsync();
        var result = await CreateService(factory).GuardarAsync(CreateRequest());

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        var thirdParty = await context.Terceros.SingleAsync();
        var identification = await context.TercerosIdentificaciones.SingleAsync();
        Assert.True(thirdParty.EsProveedor);
        Assert.Equal(1, thirdParty.EstadoProveedor);
        Assert.Equal(ClaveIdentidadTercero.Crear("RUC", "2459999906001"),
            thirdParty.ClaveIdentidad);
        Assert.Equal("2459999906001", identification.NumeroNormalizado);
        Assert.True(identification.EsPrincipal);
        Assert.Empty(context.EmpresasTerceros);
    }

    [Fact]
    public async Task SupplierCreationAndOfficialVerificationAreAuditedWithoutPersonalData()
    {
        var factory = await CreateFactoryAsync();
        var result = await CreateService(factory).GuardarAsync(CreateRequest());

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        var audits = await context.Auditorias
            .OrderBy(x => x.Id)
            .ToListAsync();
        Assert.Equal(
            [
                TercerosAuditActions.ProveedorCreado,
                TercerosAuditActions.ProveedorVerificado
            ],
            audits.Select(x => x.Accion).ToArray());
        Assert.All(audits, audit =>
        {
            Assert.Equal(1, audit.UsuarioId);
            Assert.Equal(1, audit.EmpresaId);
            Assert.Equal("terceros", audit.Entidad);
            Assert.Equal(result.TerceroId, audit.EntidadId);
            Assert.DoesNotContain("2459999906001", audit.Descripcion);
            Assert.DoesNotContain("proveedor@ejemplo.test", audit.Descripcion);
        });
    }

    [Fact]
    public async Task CedulaClientBecomesSupplierWithoutLosingClientDataOrDocuments()
    {
        var factory = await CreateFactoryAsync();
        await using (var seed = await factory.CreateDbContextAsync())
        {
            var now = DateTime.UtcNow;
            var thirdParty = new Tercero
            {
                Id = 50,
                TipoIdentificacionId = 1,
                NumeroIdentificacion = "2459999906",
                ClaveIdentidad = ClaveIdentidadTercero.Crear("CEDULA", "2459999906"),
                RazonSocial = "PERSONA NATURAL",
                Direccion = "DIRECCION CLIENTE",
                Correo = "cliente@kontax.com",
                OrigenRegistro = "OFICIAL",
                EstadoVerificacion = "VERIFICADO",
                FuenteVerificacion = "GUIA",
                VerificadoAt = now,
                EsCliente = true,
                EstadoCliente = 1,
                Estado = 1,
                CreatedAt = now
            };
            thirdParty.Identificaciones.Add(new TerceroIdentificacion
            {
                TipoIdentificacionId = 1,
                NumeroIdentificacion = "2459999906",
                NumeroNormalizado = "2459999906",
                EsPrincipal = true,
                EstadoVerificacion = "VERIFICADO",
                FuenteVerificacion = "GUIA",
                Estado = 1,
                CreatedAt = now
            });
            seed.Terceros.Add(thirdParty);
            seed.EmpresasTerceros.Add(new EmpresaTercero
            {
                EmpresaId = 1,
                Tercero = thirdParty,
                CreditoHabilitado = true,
                Observacion = "CONDICION CLIENTE",
                Estado = 1,
                CreatedAt = now
            });
            await seed.SaveChangesAsync();
        }

        var service = CreateService(factory);
        var local = await service.BuscarPorRucAsync("2459999906001");
        Assert.NotNull(local);
        Assert.Equal(50, local.TerceroId);
        Assert.False(local.EsProveedor);

        var result = await service.GuardarAsync(
            CreateRequest("2459999906001") with
            {
                TerceroId = 50,
                Version = 0
            });

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Single(context.Terceros);
        var documents = await context.TercerosIdentificaciones
            .OrderBy(x => x.TipoIdentificacionId)
            .Select(x => x.NumeroNormalizado)
            .ToArrayAsync();
        Assert.Equal(new[] { "2459999906", "2459999906001" }, documents);
        var relationship = await context.EmpresasTerceros.SingleAsync();
        Assert.True(relationship.CreditoHabilitado);
        Assert.Equal("CONDICION CLIENTE", relationship.Observacion);
        var savedThirdParty = await context.Terceros.SingleAsync();
        Assert.True(savedThirdParty.EsCliente);
        Assert.Equal(1, savedThirdParty.EstadoCliente);
        Assert.True(savedThirdParty.EsProveedor);
        Assert.Equal("2459999906", savedThirdParty.NumeroIdentificacion);
    }

    [Fact]
    public async Task CatalogIsIdenticalAfterCompanySwitch()
    {
        var factory = await CreateFactoryAsync();
        var session = new CurrentSession { UsuarioId = 1, EmpresaId = 1 };
        var service = CreateService(factory, session);
        Assert.True((await service.GuardarAsync(CreateRequest())).Success);

        var first = await service.ObtenerProveedoresAsync(new());
        session.EmpresaId = 2;
        var second = await service.ObtenerProveedoresAsync(new());

        Assert.Equal(first.Total, second.Total);
        Assert.Equal(Assert.Single(first.Items).TerceroId,
            Assert.Single(second.Items).TerceroId);
    }

    [Fact]
    public async Task StateChangeAffectsOnlySupplierRole()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var created = await service.GuardarAsync(CreateRequest());

        var result = await service.CambiarEstadoAsync(
            created.TerceroId!.Value,
            0,
            0);

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        var thirdParty = await context.Terceros.SingleAsync();
        Assert.Equal(0, thirdParty.EstadoProveedor);
        Assert.Equal(1, thirdParty.Estado);
        Assert.Contains(
            await context.Auditorias.ToListAsync(),
            x => x.Accion == TercerosAuditActions.ProveedorInactivado &&
                 x.EntidadId == thirdParty.Id);
    }

    [Fact]
    public async Task KpisUseClientDigitalContactRuleAndGlobalState()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var first = await service.GuardarAsync(CreateRequest());
        await service.GuardarAsync(CreateRequest("2459999815001") with
        {
            Correo = "cliente@kontax.com",
            Telefono = null,
            ConstanciaVerificacionId = OfflineProofId
        });
        await service.GuardarAsync(CreateRequest("2459999724001") with
        {
            Correo = null,
            Telefono = "0999999999"
        });
        await service.CambiarEstadoAsync(first.TerceroId!.Value, 0, 0);

        var catalog = await service.ObtenerProveedoresAsync(new());
        var noEmail = await service.ObtenerProveedoresAsync(new()
        {
            Kpi = ProveedorCatalogoKpi.SinCorreo
        });

        Assert.Equal(2, catalog.Kpis.Activos);
        Assert.Equal(1, catalog.Kpis.PendientesVerificar);
        Assert.Equal(2, catalog.Kpis.SinCorreo);
        Assert.Equal(1, catalog.Kpis.SinContactoDigital);
        Assert.Equal(2, noEmail.Total);
    }

    [Fact]
    public async Task EmptyOfficialTradeNameNeverOverwritesExistingValue()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var created = await service.GuardarAsync(CreateRequest() with
        {
        });
        Assert.True(created.Success, created.Message);

        var updated = await service.GuardarAsync(CreateRequest() with
        {
            TerceroId = created.TerceroId,
            Version = 0,
            RazonSocial = "RAZON ACTUALIZADA",
        });

        Assert.True(updated.Success, updated.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal("MARCA OFICIAL", (await context.Terceros.SingleAsync()).NombreComercial);
    }

    [Fact]
    public async Task MissingAuthenticationRejectsWrite()
    {
        var factory = await CreateFactoryAsync();
        var result = await CreateService(factory, new CurrentSession())
            .GuardarAsync(CreateRequest());

        Assert.False(result.Success);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Empty(context.Terceros);
    }

    [Fact]
    public async Task DeclaringOfficialVerificationWithoutProofIsRejected()
    {
        var factory = await CreateFactoryAsync();
        var result = await CreateService(factory).GuardarAsync(
            CreateRequest() with
            {
                ConstanciaVerificacionId = null
            });

        Assert.False(result.Success);
        Assert.Contains("Verifica el RUC", result.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Empty(context.Terceros);
    }

    [Fact]
    public async Task MissingActiveCompanyRejectsCatalogRead()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(
            factory,
            new CurrentSession { UsuarioId = 1 });

        var exception = await Assert.ThrowsAsync<EmpresaAccessDeniedException>(
            () => service.ObtenerProveedoresAsync(new()));

        Assert.Equal(EmpresaAccessDeniedException.UserMessage, exception.Message);
    }

    [Fact]
    public async Task UserWithoutManagementPermissionCanReadButCannotMutate()
    {
        var factory = await CreateFactoryAsync();
        var administrator = CreateService(factory);
        var created = await administrator.GuardarAsync(CreateRequest());
        Assert.True(created.Success, created.Message);
        var readOnlyUser = CreateService(
            factory,
            new CurrentSession { UsuarioId = 2, EmpresaId = 1 });

        var catalog = await readOnlyUser.ObtenerProveedoresAsync(new());
        var update = await readOnlyUser.CambiarEstadoAsync(
            created.TerceroId!.Value,
            0,
            0);
        var save = await readOnlyUser.GuardarAsync(CreateRequest() with
        {
            TerceroId = created.TerceroId,
            RazonSocial = "CAMBIO NO AUTORIZADO"
        });

        Assert.Single(catalog.Items);
        Assert.False(update.Success);
        Assert.Equal(TercerosAccessDeniedException.UserMessage, update.Message);
        Assert.False(save.Success);
        Assert.Equal(TercerosAccessDeniedException.UserMessage, save.Message);
        await using var context = await factory.CreateDbContextAsync();
        var thirdParty = await context.Terceros.SingleAsync();
        Assert.Equal(1, thirdParty.EstadoProveedor);
        Assert.Equal("PROVEEDOR DE PRUEBA", thirdParty.RazonSocial);
    }

    [Fact]
    public void ModelStoresClientAndSupplierRolesOnlyOnThirdParty()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new KontaxDbContext(options);
        var thirdParty = context.Model.FindEntityType(typeof(Tercero))!;
        Assert.NotNull(thirdParty.FindProperty(nameof(Tercero.EsCliente)));
        Assert.NotNull(thirdParty.FindProperty(nameof(Tercero.EstadoCliente)));
        Assert.NotNull(thirdParty.FindProperty(nameof(Tercero.EsProveedor)));
        Assert.NotNull(thirdParty.FindProperty(nameof(Tercero.EstadoProveedor)));
        var relationship = context.Model.FindEntityType(typeof(EmpresaTercero))!;
        Assert.Null(relationship.FindProperty("EsCliente"));
        Assert.Null(relationship.FindProperty("EsProveedor"));
        Assert.Null(relationship.FindProperty("CreditoProveedorHabilitado"));
        Assert.Contains(context.Model.FindEntityType(typeof(TerceroIdentificacion))!
            .GetIndexes(), index => index.IsUnique &&
            index.GetDatabaseName() == "ux_terceros_identificaciones_tipo_numero");
        var version = thirdParty.FindProperty(nameof(Tercero.Version));
        Assert.NotNull(version);
        Assert.True(version.IsConcurrencyToken);
        Assert.Equal(
            Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAddOrUpdate,
            version.ValueGenerated);
        var npgsqlOptions = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var npgsqlContext = new KontaxDbContext(npgsqlOptions);
        var npgsqlVersion = npgsqlContext.Model
            .FindEntityType(typeof(Tercero))!
            .FindProperty(nameof(Tercero.Version))!;
        Assert.Equal("xmin", npgsqlVersion.GetColumnName());
        Assert.Equal("xid", npgsqlVersion.GetColumnType());
    }

    [Fact]
    public async Task StaleSupplierVersionDoesNotOverwriteConcurrentContactChange()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var created = await service.GuardarAsync(CreateRequest());
        var opened = await service.ObtenerProveedorAsync(created.TerceroId!.Value);
        Assert.NotNull(opened);

        await using (var concurrent = await factory.CreateDbContextAsync())
        {
            var thirdParty = await concurrent.Terceros.SingleAsync();
            thirdParty.Correo = "cambio.concurrente@example.test";
            thirdParty.Version = opened.Version + 1;
            concurrent.Entry(thirdParty).Property(x => x.Version).IsModified = true;
            await concurrent.SaveChangesAsync();
        }
        int auditCountBefore;
        await using (var before = await factory.CreateDbContextAsync())
            auditCountBefore = await before.Auditorias.CountAsync();

        var result = await service.GuardarAsync(CreateRequest() with
        {
            TerceroId = created.TerceroId,
            Version = opened.Version,
            Correo = "valor.obsoleto@example.test",
            ConstanciaVerificacionId = null
        });

        Assert.False(result.Success);
        Assert.True(result.ConcurrencyConflict);
        Assert.Equal(TercerosConcurrency.UserMessage, result.Message);
        await using var verification = await factory.CreateDbContextAsync();
        Assert.Equal(
            "cambio.concurrente@example.test",
            (await verification.Terceros.SingleAsync()).Correo);
        Assert.Equal(auditCountBefore, await verification.Auditorias.CountAsync());
    }

    [Fact]
    public async Task StaleVersionRejectsSupplierStateChange()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var created = await service.GuardarAsync(CreateRequest());
        var opened = await service.ObtenerProveedorAsync(created.TerceroId!.Value);
        Assert.NotNull(opened);

        await using (var concurrent = await factory.CreateDbContextAsync())
        {
            var thirdParty = await concurrent.Terceros.SingleAsync();
            thirdParty.Telefono = "+593999999999";
            thirdParty.Version = opened.Version + 1;
            concurrent.Entry(thirdParty).Property(x => x.Version).IsModified = true;
            await concurrent.SaveChangesAsync();
        }
        int auditCountBefore;
        await using (var before = await factory.CreateDbContextAsync())
            auditCountBefore = await before.Auditorias.CountAsync();

        var result = await service.CambiarEstadoAsync(
            created.TerceroId.Value,
            0,
            opened.Version);

        Assert.False(result.Success);
        Assert.True(result.ConcurrencyConflict);
        await using var verification = await factory.CreateDbContextAsync();
        Assert.Equal(1, (await verification.Terceros.SingleAsync()).EstadoProveedor);
        Assert.Equal(auditCountBefore, await verification.Auditorias.CountAsync());
    }

    private static ProveedorGuardarRequest CreateRequest(
        string ruc = "2459999906001") => new()
    {
        Ruc = ruc,
        RazonSocial = "PROVEEDOR DE PRUEBA",
        Correo = "proveedor@ejemplo.test",
        ConstanciaVerificacionId = VerifiedProofId,
        Estado = 1
    };

    private static async Task<TestDbContextFactory> CreateFactoryAsync()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var factory = new TestDbContextFactory(options);
        await using var context = await factory.CreateDbContextAsync();
        var now = DateTime.UtcNow;
        context.TiposIdentificacion.AddRange(
            new TipoIdentificacion
            {
                Id = 1, CodigoSri = "05", Codigo = "CEDULA", Nombre = "CEDULA",
                LongitudMinima = 10, LongitudMaxima = 10, Estado = 1, CreatedAt = now
            },
            new TipoIdentificacion
            {
                Id = 2, CodigoSri = "04", Codigo = "RUC", Nombre = "RUC",
                LongitudMinima = 13, LongitudMaxima = 13, Estado = 1, CreatedAt = now
            });
        context.Empresas.AddRange(
            new Empresa
            {
                Id = 1, RegimenTributarioId = 1,
                NumeroIdentificacion = "1799999999001", RazonSocial = "EMPRESA UNO",
                Estado = 1, CreatedAt = now
            },
            new Empresa
            {
                Id = 2, RegimenTributarioId = 1,
                NumeroIdentificacion = "1799999999002", RazonSocial = "EMPRESA DOS",
                Estado = 1, CreatedAt = now
            });
        context.Usuarios.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.Usuario
            {
                Id = 1, NumeroIdentificacion = "0999999999",
                NombreCompleto = "USUARIO PRUEBA", PasswordHash = "HASH",
                Estado = 1, CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.Usuario
            {
                Id = 2, NumeroIdentificacion = "0888888888",
                NombreCompleto = "USUARIO SOLO LECTURA", PasswordHash = "HASH",
                Estado = 1, CreatedAt = now
            });
        context.UsuariosEmpresas.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresa
            {
                Id = 1, UsuarioId = 1, EmpresaId = 1,
                Estado = 1, CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresa
            {
                Id = 2, UsuarioId = 1, EmpresaId = 2,
                Estado = 1, CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresa
            {
                Id = 3, UsuarioId = 2, EmpresaId = 1,
                Estado = 1, CreatedAt = now
            });
        context.Roles.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.Rol
            {
                Id = 1, Codigo = "ADMINISTRADOR", Nombre = "ADMINISTRADOR",
                EsSistema = true, Estado = 1, CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.Rol
            {
                Id = 2, Codigo = "VENDEDOR", Nombre = "VENDEDOR",
                EsSistema = true, Estado = 1, CreatedAt = now
            });
        context.Permisos.Add(new KONTAXPRO.Domain.Entities.Seguridad.Permiso
        {
            Id = 1, Codigo = TercerosPermissions.Gestionar,
            Nombre = TercerosPermissions.Gestionar, Modulo = "COMERCIAL",
            Estado = 1, CreatedAt = now
        });
        context.RolesPermisos.Add(new KONTAXPRO.Domain.Entities.Seguridad.RolPermiso
        {
            Id = 1, RolId = 1, PermisoId = 1, CreatedAt = now
        });
        context.UsuariosEmpresasRoles.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresaRol
            {
                Id = 1, UsuarioEmpresaId = 1, RolId = 1, CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresaRol
            {
                Id = 2, UsuarioEmpresaId = 2, RolId = 1, CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresaRol
            {
                Id = 3, UsuarioEmpresaId = 3, RolId = 2, CreatedAt = now
            });
        await context.SaveChangesAsync();
        return factory;
    }

    private static ProveedorService CreateService(
        TestDbContextFactory factory,
        CurrentSession? session = null) => new(
            factory,
            session ?? new CurrentSession { UsuarioId = 1, EmpresaId = 1 },
            new TestProofStore(),
            NullLogger<ProveedorService>.Instance);

    private sealed class TestProofStore
        : IConstanciaVerificacionIdentificacionStore
    {
        public bool TryTake(
            Guid constanciaId,
            long usuarioId,
            string tipoIdentificacionCodigo,
            string numeroNormalizado,
            PropositoConsultaIdentificacion proposito,
            out ConstanciaVerificacionIdentificacion? constancia)
        {
            if (constanciaId != VerifiedProofId &&
                constanciaId != OfflineProofId)
            {
                constancia = null;
                return false;
            }

            var type = constanciaId == VerifiedProofId
                ? TipoConstanciaVerificacion.Verificada
                : TipoConstanciaVerificacion.OfflineAutorizada;
            constancia = new ConstanciaVerificacionIdentificacion(
                constanciaId,
                usuarioId,
                tipoIdentificacionCodigo,
                numeroNormalizado,
                proposito,
                type,
                type == TipoConstanciaVerificacion.Verificada
                    ? "PROVEEDOR DE PRUEBA"
                    : null,
                type == TipoConstanciaVerificacion.Verificada
                    ? "MARCA OFICIAL"
                    : null,
                type == TipoConstanciaVerificacion.Verificada
                    ? "GUIA"
                    : "MANUAL",
                DateTimeOffset.UtcNow.AddMinutes(10));
            return true;
        }
    }

    private sealed class TestDbContextFactory(DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);
        public Task<KontaxDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
