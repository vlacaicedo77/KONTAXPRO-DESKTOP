using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Clientes;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Security;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Infrastructure.Clientes;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace KONTAXPRO.Tests.Clientes;

public sealed class ClienteServiceTests
{
    private static readonly Guid OfflineProofId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid VerifiedProofId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");
    [Theory]
    [InlineData("A", "A")]
    [InlineData("B", "B")]
    [InlineData("c", "C")]
    [InlineData("BASE", "A")]
    [InlineData(null, "A")]
    [InlineData("D", "D")]
    [InlineData("e", "E")]
    public void PriceClassificationUsesUpToFiveBaseClasses(
        string? code,
        string expected)
    {
        var client = new ClienteCatalogoItemDto
        {
            ListaPrecioCodigo = code
        };

        Assert.Equal(expected, client.ClasificacionPrecio);
    }

    [Fact]
    public async Task ClientCreationIsAuditedWithActiveContextAndWithoutPersonalData()
    {
        var factory = await CreateFactoryAsync();
        var result = await CreateService(factory)
            .GuardarAsync(CreateRequest(1, 101));

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        var audit = await context.Auditorias.SingleAsync();
        Assert.Equal(TercerosAuditActions.ClienteCreado, audit.Accion);
        Assert.Equal(1, audit.UsuarioId);
        Assert.Equal(1, audit.EmpresaId);
        Assert.Equal("terceros", audit.Entidad);
        Assert.Equal(result.TerceroId, audit.EntidadId);
        Assert.DoesNotContain("1710034065", audit.Descripcion);
        Assert.DoesNotContain("Av. Principal 123", audit.Descripcion);
    }

    [Fact]
    public async Task SameIdentificationInTwoCompaniesReusesGlobalThirdParty()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);

        var first = await service.GuardarAsync(CreateRequest(1, 101));
        var second = await CreateService(factory, 2)
            .GuardarAsync(CreateRequest(2, 201) with
            {
                TerceroId = first.TerceroId,
                Version = 0
            });

        Assert.True(first.Success, first.Message);
        Assert.True(second.Success, second.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal(1, await context.Terceros.CountAsync());
        Assert.Equal(2, await context.EmpresasTerceros.CountAsync());
        var assignedLists = await context.EmpresasTerceros
            .OrderBy(x => x.EmpresaId)
            .Select(x => x.ListaPrecioId!.Value)
            .ToArrayAsync();
        Assert.Equal(new long[] { 101, 201 }, assignedLists);
    }

    [Fact]
    public async Task ExistingThirdPartyGetsClientRoleWithoutDuplication()
    {
        var factory = await CreateFactoryAsync();
        await using (var seed = await factory.CreateDbContextAsync())
        {
            seed.Terceros.Add(new Tercero
            {
                Id = 50,
                TipoIdentificacionId = 1,
                NumeroIdentificacion = "1710034065",
                RazonSocial = "TERCERO EXISTENTE",
                OrigenRegistro = "OFFLINE",
                EstadoVerificacion = "PENDIENTE",
                Estado = 1,
                CreatedAt = DateTime.UtcNow
            });
            await seed.SaveChangesAsync();
        }
        var service = CreateService(factory);

        var result = await service.GuardarAsync(CreateRequest(1, 101) with
        {
            TerceroId = 50,
            Version = 0
        });

        Assert.True(result.Success, result.Message);
        Assert.Equal(50, result.TerceroId);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Single(context.Terceros);
        Assert.Single(context.EmpresasTerceros);
    }

    [Fact]
    public async Task ForeignCompanyPriceListIsRejectedWithoutPartialRecords()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);

        var result = await service.GuardarAsync(CreateRequest(1, 201));

        Assert.False(result.Success);
        Assert.Contains("empresa activa", result.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Empty(context.Terceros);
        Assert.Empty(context.EmpresasTerceros);
    }

    [Fact]
    public async Task MissingAssignmentUsesCompanyBasePriceList()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var request = CreateRequest(1, null);

        var result = await service.GuardarAsync(request);

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal(101, (await context.EmpresasTerceros.SingleAsync()).ListaPrecioId);
    }

    [Fact]
    public async Task ChangingStateIsGlobalAndKeepsCompanyConfigurations()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var first = await service.GuardarAsync(CreateRequest(1, 101));
        var second = await CreateService(factory, 2)
            .GuardarAsync(CreateRequest(2, 201) with
            {
                TerceroId = first.TerceroId,
                Version = 0
            });
        await using (var seed = await factory.CreateDbContextAsync())
        {
            var sharedThirdParty = await seed.Terceros.SingleAsync();
            sharedThirdParty.EsProveedor = true;
            sharedThirdParty.EstadoProveedor = 1;
            await seed.SaveChangesAsync();
        }

        var stateResult = await service.CambiarEstadoAsync(
            first.TerceroId!.Value,
            1,
            0,
            0);

        Assert.True(stateResult.Success);
        await using var context = await factory.CreateDbContextAsync();
        var states = await context.EmpresasTerceros
            .OrderBy(x => x.EmpresaId)
            .Select(x => x.Estado)
            .ToArrayAsync();
        Assert.Equal([1, 1], states);
        var thirdParty = await context.Terceros.SingleAsync();
        Assert.Equal(0, thirdParty.EstadoCliente);
        Assert.True(thirdParty.EsProveedor);
        Assert.Equal(1, thirdParty.EstadoProveedor);
        Assert.NotNull(second.EmpresaTerceroId);
        Assert.Contains(
            await context.Auditorias.ToListAsync(),
            x => x.Accion == TercerosAuditActions.ClienteInactivado &&
                 x.EntidadId == thirdParty.Id);
    }

    [Fact]
    public async Task CatalogIsGlobalAndUsesActiveCompanyDefaults()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        await service.GuardarAsync(CreateRequest(1, 101));

        var companyOne = await service.ObtenerClientesAsync(
            new ClienteCatalogoQuery { EmpresaId = 1 });
        var companyTwo = await CreateService(factory, 2).ObtenerClientesAsync(
            new ClienteCatalogoQuery { EmpresaId = 2 });

        Assert.Single(companyOne.Items);
        Assert.Equal("A", companyOne.Items[0].ClasificacionPrecio);
        var companyTwoClient = Assert.Single(companyTwo.Items);
        Assert.Null(companyTwoClient.EmpresaTerceroId);
        Assert.Equal("A", companyTwoClient.ClasificacionPrecio);
        Assert.True(companyTwoClient.CreditoHabilitado);
    }

    [Fact]
    public async Task ActiveAuthorizedCompanyAllowsCommercialOperations()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory, 2);

        var lists = await service.ObtenerListasPrecioAsync(2);
        var saved = await service.GuardarAsync(CreateRequest(2, 201));

        Assert.Single(lists);
        Assert.True(saved.Success, saved.Message);
    }

    [Fact]
    public async Task ManipulatedCompanyIdDoesNotReturnCommercialData()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory, 1);

        var exception = await Assert.ThrowsAsync<EmpresaAccessDeniedException>(
            () => service.ObtenerClientesAsync(
                new ClienteCatalogoQuery { EmpresaId = 2 }));

        Assert.Equal(EmpresaAccessDeniedException.UserMessage, exception.Message);
    }

    [Fact]
    public async Task UserWithoutCompanyAccessCannotReadOrModifyCommercialData()
    {
        var factory = await CreateFactoryAsync();
        var authorized = CreateService(factory, 1);
        var created = await authorized.GuardarAsync(CreateRequest(1, 101));
        var unauthorized = CreateService(factory, 1, userId: 99);

        await Assert.ThrowsAsync<EmpresaAccessDeniedException>(
            () => unauthorized.ObtenerClienteAsync(
                created.TerceroId!.Value,
                1));
        var update = await unauthorized.CambiarEstadoAsync(
            created.TerceroId!.Value,
            1,
            0,
            0);

        Assert.False(update.Success);
        Assert.Equal(EmpresaAccessDeniedException.UserMessage, update.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal(1, (await context.Terceros.SingleAsync()).EstadoCliente);
    }

    [Fact]
    public async Task UserWithoutManagementPermissionCanReadButCannotMutate()
    {
        var factory = await CreateFactoryAsync();
        var administrator = CreateService(factory, 1);
        var created = await administrator.GuardarAsync(CreateRequest(1, 101));
        Assert.True(created.Success, created.Message);
        var readOnlyUser = CreateService(factory, 1, userId: 2);

        var catalog = await readOnlyUser.ObtenerClientesAsync(
            new ClienteCatalogoQuery { EmpresaId = 1 });
        var update = await readOnlyUser.CambiarEstadoAsync(
            created.TerceroId!.Value,
            1,
            0,
            0);
        var save = await readOnlyUser.GuardarAsync(CreateRequest(1, 101));

        Assert.Single(catalog.Items);
        Assert.False(update.Success);
        Assert.Equal(TercerosAccessDeniedException.UserMessage, update.Message);
        Assert.False(save.Success);
        Assert.Equal(TercerosAccessDeniedException.UserMessage, save.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Equal(1, (await context.Terceros.SingleAsync()).EstadoCliente);
    }

    [Fact]
    public async Task InactiveOrMissingCompanyIsRejectedWithoutPartialData()
    {
        var factory = await CreateFactoryAsync();
        await using (var context = await factory.CreateDbContextAsync())
        {
            (await context.Empresas.SingleAsync(x => x.Id == 2)).Estado = 0;
            await context.SaveChangesAsync();
        }

        var inactive = await CreateService(factory, 2)
            .GuardarAsync(CreateRequest(2, 201));
        var missing = await CreateService(factory, 99)
            .GuardarAsync(CreateRequest(99, null));

        Assert.False(inactive.Success);
        Assert.False(missing.Success);
        Assert.Equal(EmpresaAccessDeniedException.UserMessage, inactive.Message);
        Assert.Equal(EmpresaAccessDeniedException.UserMessage, missing.Message);
        await using var verify = await factory.CreateDbContextAsync();
        Assert.Empty(verify.Terceros);
        Assert.Empty(verify.EmpresasTerceros);
    }

    [Fact]
    public async Task UnauthorizedIdentificationSearchDoesNotRevealOtherCompanyConfiguration()
    {
        var factory = await CreateFactoryAsync();
        var authorized = CreateService(factory, 1);
        await authorized.GuardarAsync(CreateRequest(1, 101));
        var unauthorized = CreateService(factory, 1, userId: 99);

        await Assert.ThrowsAsync<EmpresaAccessDeniedException>(
            () => unauthorized.BuscarPorIdentificacionAsync(
                1,
                "1710034065",
                1));
    }

    [Fact]
    public async Task CatalogKpisCountOperationalExceptionsAndExcludePassportFromPendingVerification()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        await service.GuardarAsync(CreateRequest(1, 101));

        await using (var seed = await factory.CreateDbContextAsync())
        {
            var now = DateTime.UtcNow;
            seed.TiposIdentificacion.Add(new TipoIdentificacion
            {
                Id = 3,
                CodigoSri = "06",
                Codigo = "PASAPORTE",
                Nombre = "PASAPORTE",
                LongitudMinima = 3,
                LongitudMaxima = 20,
                Estado = 1,
                CreatedAt = now
            });
            seed.Terceros.Add(new Tercero
            {
                Id = 60,
                TipoIdentificacionId = 3,
                NumeroIdentificacion = "PA123456",
                RazonSocial = "CLIENTE CON PASAPORTE",
                Direccion = "Av. Secundaria 456",
                Correo = "pasaporte@example.com",
                OrigenRegistro = "OFFLINE",
                EstadoVerificacion = "PENDIENTE",
                EsCliente = true,
                EstadoCliente = 1,
                Estado = 1,
                CreatedAt = now
            });
            seed.EmpresasTerceros.Add(new EmpresaTercero
            {
                Id = 60,
                EmpresaId = 1,
                TerceroId = 60,
                ListaPrecioId = 101,
                CreditoHabilitado = true,
                Estado = 1,
                CreatedAt = now
            });
            await seed.SaveChangesAsync();
        }

        var catalog = await service.ObtenerClientesAsync(
            new ClienteCatalogoQuery { EmpresaId = 1 });
        var pending = await service.ObtenerClientesAsync(
            new ClienteCatalogoQuery
            {
                EmpresaId = 1,
                Kpi = ClienteCatalogoKpi.PendientesVerificar
            });
        var noContact = await service.ObtenerClientesAsync(
            new ClienteCatalogoQuery
            {
                EmpresaId = 1,
                Kpi = ClienteCatalogoKpi.SinContactoDigital
            });
        var advancedPending = await service.ObtenerClientesAsync(
            new ClienteCatalogoQuery
            {
                EmpresaId = 1,
                Verificacion = ClienteVerificacionFiltro.NoVerificados
            });

        Assert.Equal(2, catalog.Kpis.Clientes);
        Assert.Equal(1, catalog.Kpis.PendientesVerificar);
        Assert.Equal(1, catalog.Kpis.SinCredito);
        Assert.Equal(1, catalog.Kpis.SinContactoDigital);
        Assert.Single(pending.Items);
        Assert.Equal("CEDULA", pending.Items[0].TipoIdentificacionCodigo);
        Assert.Single(noContact.Items);
        Assert.Equal("CEDULA", noContact.Items[0].TipoIdentificacionCodigo);
        Assert.Single(advancedPending.Items);
        Assert.Equal("CEDULA", advancedPending.Items[0].TipoIdentificacionCodigo);
    }

    [Fact]
    public async Task CedulaThenNaturalPersonRucReusesSameThirdParty()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);

        var cedula = await service.GuardarAsync(CreateRequest(1, 101));
        var ruc = await service.GuardarAsync(CreateNationalRequest(
            1,
            101,
            2,
            "1710034065001") with
        {
            TerceroId = cedula.TerceroId,
            Version = 0
        });

        Assert.True(cedula.Success, cedula.Message);
        Assert.True(ruc.Success, ruc.Message);
        Assert.Equal(cedula.TerceroId, ruc.TerceroId);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Single(context.Terceros);
        Assert.Single(context.EmpresasTerceros);
    }

    [Fact]
    public async Task SearchByRucFindsExistingEquivalentCedula()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        await service.GuardarAsync(CreateRequest(1, 101));

        var result = await service.BuscarPorIdentificacionAsync(
            2,
            "1710034065001",
            1);

        Assert.NotNull(result);
        Assert.Equal("CEDULA", result.TipoIdentificacionCodigo);
        Assert.Equal("1710034065", result.NumeroIdentificacion);
        Assert.True(result.EsCliente);
        Assert.True(result.TieneConfiguracionEmpresaActual);
    }

    [Fact]
    public async Task RucThenCedulaAlsoReusesSameThirdParty()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);

        var ruc = await service.GuardarAsync(CreateNationalRequest(
            1,
            101,
            2,
            "1710034065001"));
        var cedula = await service.GuardarAsync(CreateRequest(1, 101) with
        {
            TerceroId = ruc.TerceroId,
            Version = 0
        });

        Assert.True(ruc.Success, ruc.Message);
        Assert.True(cedula.Success, cedula.Message);
        Assert.Equal(ruc.TerceroId, cedula.TerceroId);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Single(context.Terceros);
    }

    [Fact]
    public async Task NewClientIsActiveAndStoresNormalizedContactData()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var request = new ClienteGuardarRequest
        {
            EmpresaId = 1,
            TipoIdentificacionId = 1,
            NumeroIdentificacion = "1710034065",
            RazonSocial = "PERSONA DE PRUEBA",
            Direccion = "Av. Principal 123",
            Correo = "CLIENTE@Example.COM",
            Telefono = "099 999 9999",
            ListaPrecioId = 101,
            CreditoHabilitado = true,
            ConstanciaVerificacionId = OfflineProofId,
            Estado = 0
        };

        var result = await service.GuardarAsync(request);

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        var thirdParty = await context.Terceros.SingleAsync();
        var relationship = await context.EmpresasTerceros.SingleAsync();
        Assert.Equal("cliente@example.com", thirdParty.Correo);
        Assert.Equal("+593999999999", thirdParty.Telefono);
        Assert.True(thirdParty.EsCliente);
        Assert.Equal(1, thirdParty.EstadoCliente);
        Assert.Equal(1, relationship.Estado);
        Assert.True(relationship.CreditoHabilitado);
    }

    [Fact]
    public async Task NationalClientCannotForgeOfficialOrOfflineVerification()
    {
        var factory = await CreateFactoryAsync();
        var request = CreateRequest(1, 101);
        request = new ClienteGuardarRequest
        {
            EmpresaId = request.EmpresaId,
            TipoIdentificacionId = request.TipoIdentificacionId,
            NumeroIdentificacion = request.NumeroIdentificacion,
            RazonSocial = request.RazonSocial,
            Direccion = request.Direccion,
            ListaPrecioId = request.ListaPrecioId,
            ConstanciaVerificacionId = null
        };

        var result = await CreateService(factory).GuardarAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Verifica la identificación", result.Message);
        await using var context = await factory.CreateDbContextAsync();
        Assert.Empty(context.Terceros);
    }

    [Fact]
    public async Task NewClientWithoutEmailStoresDefaultEmail()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);

        var result = await service.GuardarAsync(CreateRequest(1, 101));

        Assert.True(result.Success, result.Message);
        await using var context = await factory.CreateDbContextAsync();
        var thirdParty = await context.Terceros.SingleAsync();
        Assert.Equal(ContactoClienteNormalizer.DefaultEmail, thirdParty.Correo);
        Assert.Equal("Av. Principal 123", thirdParty.Direccion);
    }

    [Fact]
    public async Task EditingPendingNationalClientPersistsOfficialVerification()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var created = await service.GuardarAsync(CreateRequest(1, 101));
        Assert.True(created.Success, created.Message);

        var verified = await service.GuardarAsync(new ClienteGuardarRequest
        {
            EmpresaId = 1,
            EmpresaTerceroId = created.EmpresaTerceroId,
            TerceroId = created.TerceroId,
            Version = 0,
            TipoIdentificacionId = 1,
            NumeroIdentificacion = "1710034065",
            RazonSocial = "PERSONA VERIFICADA",
            Direccion = "Av. Oficial 456",
            Correo = "oficial@example.com",
            ListaPrecioId = 101,
            CreditoHabilitado = true,
            ConstanciaVerificacionId = VerifiedProofId,
            Estado = 1
        });

        Assert.True(verified.Success, verified.Message);
        await using var context = await factory.CreateDbContextAsync();
        var thirdParty = await context.Terceros.SingleAsync();
        Assert.Equal("VERIFICADO", thirdParty.EstadoVerificacion);
        Assert.Equal("GUIA", thirdParty.FuenteVerificacion);
        Assert.Equal("OFICIAL", thirdParty.OrigenRegistro);
        Assert.NotNull(thirdParty.VerificadoAt);
        Assert.Equal("PERSONA VERIFICADA", thirdParty.RazonSocial);
    }

    [Fact]
    public async Task StaleClientVersionDoesNotOverwriteConcurrentContactChange()
    {
        var factory = await CreateFactoryAsync();
        var service = CreateService(factory);
        var created = await service.GuardarAsync(CreateRequest(1, 101));
        var opened = await service.ObtenerClienteAsync(
            created.TerceroId!.Value,
            1);
        Assert.NotNull(opened);

        await using (var concurrent = await factory.CreateDbContextAsync())
        {
            var thirdParty = await concurrent.Terceros.SingleAsync();
            thirdParty.Direccion = "DIRECCION CONCURRENTE";
            thirdParty.Version = opened.Version + 1;
            concurrent.Entry(thirdParty).Property(x => x.Version).IsModified = true;
            await concurrent.SaveChangesAsync();
        }
        int auditCountBefore;
        await using (var before = await factory.CreateDbContextAsync())
            auditCountBefore = await before.Auditorias.CountAsync();

        var result = await service.GuardarAsync(CreateRequest(1, 101) with
        {
            EmpresaTerceroId = created.EmpresaTerceroId,
            TerceroId = created.TerceroId,
            Version = opened.Version,
            Direccion = "DIRECCION OBSOLETA",
            ConstanciaVerificacionId = null
        });

        Assert.False(result.Success);
        Assert.True(result.ConcurrencyConflict);
        Assert.Equal(TercerosConcurrency.UserMessage, result.Message);
        await using var verification = await factory.CreateDbContextAsync();
        Assert.Equal(
            "DIRECCION CONCURRENTE",
            (await verification.Terceros.SingleAsync()).Direccion);
        Assert.Equal(auditCountBefore, await verification.Auditorias.CountAsync());
    }

    private static ClienteGuardarRequest CreateRequest(
        long companyId,
        long? priceListId) => CreateNationalRequest(
            companyId,
            priceListId,
            1,
            "1710034065");

    private static ClienteGuardarRequest CreateNationalRequest(
        long companyId,
        long? priceListId,
        long typeId,
        string number) => new()
    {
        EmpresaId = companyId,
        TipoIdentificacionId = typeId,
        NumeroIdentificacion = number,
        RazonSocial = "PERSONA DE PRUEBA",
        Direccion = "Av. Principal 123",
        ListaPrecioId = priceListId,
        ConstanciaVerificacionId = OfflineProofId,
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
                Id = 1,
                CodigoSri = "05",
                Codigo = "CEDULA",
                Nombre = "CEDULA",
                LongitudMinima = 10,
                LongitudMaxima = 10,
                Estado = 1,
                CreatedAt = now
            },
            new TipoIdentificacion
            {
                Id = 2,
                CodigoSri = "04",
                Codigo = "RUC",
                Nombre = "RUC",
                LongitudMinima = 13,
                LongitudMaxima = 13,
                Estado = 1,
                CreatedAt = now
            });
        context.Empresas.AddRange(
            new Empresa
            {
                Id = 1,
                RegimenTributarioId = 1,
                NumeroIdentificacion = "1790016919001",
                RazonSocial = "EMPRESA UNO",
                Estado = 1,
                CreatedAt = now
            },
            new Empresa
            {
                Id = 2,
                RegimenTributarioId = 1,
                NumeroIdentificacion = "1760001550001",
                RazonSocial = "EMPRESA DOS",
                Estado = 1,
                CreatedAt = now
            });
        context.Usuarios.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.Usuario
            {
                Id = 1,
                NumeroIdentificacion = "0999999999",
                NombreCompleto = "USUARIO DE PRUEBA",
                PasswordHash = "TEST_HASH",
                Estado = 1,
                CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.Usuario
            {
                Id = 2,
                NumeroIdentificacion = "0888888888",
                NombreCompleto = "USUARIO SOLO LECTURA",
                PasswordHash = "TEST_HASH",
                Estado = 1,
                CreatedAt = now
            });
        context.UsuariosEmpresas.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresa
            {
                Id = 1,
                UsuarioId = 1,
                EmpresaId = 1,
                Estado = 1,
                CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresa
            {
                Id = 2,
                UsuarioId = 1,
                EmpresaId = 2,
                Estado = 1,
                CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresa
            {
                Id = 3,
                UsuarioId = 2,
                EmpresaId = 1,
                Estado = 1,
                CreatedAt = now
            });
        context.Roles.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.Rol
            {
                Id = 1,
                Codigo = "ADMINISTRADOR",
                Nombre = "ADMINISTRADOR",
                EsSistema = true,
                Estado = 1,
                CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.Rol
            {
                Id = 2,
                Codigo = "VENDEDOR",
                Nombre = "VENDEDOR",
                EsSistema = true,
                Estado = 1,
                CreatedAt = now
            });
        context.Permisos.Add(new KONTAXPRO.Domain.Entities.Seguridad.Permiso
        {
            Id = 1,
            Codigo = TercerosPermissions.Gestionar,
            Nombre = TercerosPermissions.Gestionar,
            Modulo = "COMERCIAL",
            Estado = 1,
            CreatedAt = now
        });
        context.RolesPermisos.Add(new KONTAXPRO.Domain.Entities.Seguridad.RolPermiso
        {
            Id = 1,
            RolId = 1,
            PermisoId = 1,
            CreatedAt = now
        });
        context.UsuariosEmpresasRoles.AddRange(
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresaRol
            {
                Id = 1,
                UsuarioEmpresaId = 1,
                RolId = 1,
                CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresaRol
            {
                Id = 2,
                UsuarioEmpresaId = 2,
                RolId = 1,
                CreatedAt = now
            },
            new KONTAXPRO.Domain.Entities.Seguridad.UsuarioEmpresaRol
            {
                Id = 3,
                UsuarioEmpresaId = 3,
                RolId = 2,
                CreatedAt = now
            });
        context.ListasPrecio.AddRange(
            new ListaPrecio
            {
                Id = 101,
                EmpresaId = 1,
                Codigo = "BASE",
                Nombre = "LISTA BASE UNO",
                EsListaBase = true,
                Estado = 1,
                CreatedAt = now
            },
            new ListaPrecio
            {
                Id = 201,
                EmpresaId = 2,
                Codigo = "BASE",
                Nombre = "LISTA BASE DOS",
                EsListaBase = true,
                Estado = 1,
                CreatedAt = now
            });
        await context.SaveChangesAsync();
        return factory;
    }

    private static ClienteService CreateService(
        TestDbContextFactory factory,
        long companyId = 1,
        long userId = 1) =>
        new(
            factory,
            new CurrentSession
            {
                UsuarioId = userId,
                EmpresaId = companyId
            },
            new TestProofStore(),
            NullLogger<ClienteService>.Instance);

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
            var type = constanciaId == VerifiedProofId
                ? TipoConstanciaVerificacion.Verificada
                : constanciaId == OfflineProofId
                    ? TipoConstanciaVerificacion.OfflineAutorizada
                    : (TipoConstanciaVerificacion?)null;
            if (!type.HasValue)
            {
                constancia = null;
                return false;
            }

            constancia = new ConstanciaVerificacionIdentificacion(
                constanciaId,
                usuarioId,
                tipoIdentificacionCodigo,
                numeroNormalizado,
                proposito,
                type.Value,
                type == TipoConstanciaVerificacion.Verificada
                    ? "PERSONA VERIFICADA"
                    : null,
                null,
                type == TipoConstanciaVerificacion.Verificada
                    ? "GUIA"
                    : "MANUAL",
                DateTimeOffset.UtcNow.AddMinutes(10));
            return true;
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
