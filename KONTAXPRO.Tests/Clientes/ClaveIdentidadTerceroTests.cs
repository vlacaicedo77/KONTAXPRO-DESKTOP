using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.Clientes;

public sealed class ClaveIdentidadTerceroTests
{
    [Theory]
    [InlineData("CEDULA", "1710034065", "NAT:1710034065")]
    [InlineData("RUC", "1710034065001", "NAT:1710034065")]
    [InlineData("RUC", "1790016919001", "RUC:1790016919001")]
    [InlineData("RUC", "1760001550001", "RUC:1760001550001")]
    [InlineData("PASAPORTE", " ab-12345 ", "PAS:AB-12345")]
    [InlineData("EXTERIOR", " ext_987 ", "EXT:EXT_987")]
    public void CreatesCanonicalKeyByRealIdentity(
        string type,
        string number,
        string expected)
    {
        Assert.Equal(expected, ClaveIdentidadTercero.Crear(type, number));
    }

    [Fact]
    public void NaturalCedulaAndRucHaveSameKey()
    {
        var cedula = ClaveIdentidadTercero.Crear("CEDULA", "1710034065");
        var ruc = ClaveIdentidadTercero.Crear("RUC", "1710034065001");

        Assert.Equal(cedula, ruc);
    }

    [Fact]
    public void PassportAndExteriorDoNotCollide()
    {
        var passport = ClaveIdentidadTercero.Crear("PASAPORTE", "AB123456");
        var exterior = ClaveIdentidadTercero.Crear("EXTERIOR", "AB123456");

        Assert.NotEqual(passport, exterior);
    }

    [Fact]
    public void EfModelHasUniqueCanonicalIdentityIndex()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        using var context = new KontaxDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(Tercero));
        var index = entityType!.GetIndexes().Single(x =>
            x.GetDatabaseName() == ClaveIdentidadTercero.UniqueConstraintName);

        Assert.True(index.IsUnique);
        Assert.Equal(
            nameof(Tercero.ClaveIdentidad),
            Assert.Single(index.Properties).Name);
    }

    [Fact]
    public async Task DbContextRecalculatesKeyWhenIdentificationChanges()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new KontaxDbContext(options);
        context.TiposIdentificacion.AddRange(
            new TipoIdentificacion
            {
                Id = 1,
                CodigoSri = "05",
                Codigo = "CEDULA",
                Nombre = "CÉDULA",
                LongitudMinima = 10,
                LongitudMaxima = 10,
                Estado = 1,
                CreatedAt = DateTime.UtcNow
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
                CreatedAt = DateTime.UtcNow
            });
        var thirdParty = new Tercero
        {
            TipoIdentificacionId = 1,
            NumeroIdentificacion = "1710034065",
            RazonSocial = "PERSONA DE PRUEBA",
            OrigenRegistro = "OFFLINE",
            EstadoVerificacion = "PENDIENTE",
            CreatedAt = DateTime.UtcNow
        };
        context.Terceros.Add(thirdParty);
        await context.SaveChangesAsync();
        Assert.Equal("NAT:1710034065", thirdParty.ClaveIdentidad);

        thirdParty.TipoIdentificacionId = 2;
        thirdParty.NumeroIdentificacion = "1790016919001";
        await context.SaveChangesAsync();

        Assert.Equal("RUC:1790016919001", thirdParty.ClaveIdentidad);
    }
}
