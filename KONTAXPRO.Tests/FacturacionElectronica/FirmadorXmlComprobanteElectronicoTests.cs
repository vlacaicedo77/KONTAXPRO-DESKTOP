using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class FirmadorXmlComprobanteElectronicoTests
{
    [Fact]
    public async Task Revocacion_confirmada_bloquea_antes_de_firmar()
    {
        var factory = await CrearFactoryAsync();
        var xades = new XadesSpy();
        var service = new FirmadorXmlComprobanteElectronico(
            factory, new StorageFake(),
            new ValidatorFake(Revocado()), xades);
        await using var xml = Xml();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.FirmarAsync(xml, 7));

        Assert.Equal(0, xades.Firmas);
    }

    [Fact]
    public async Task Revocacion_temporalmente_desconocida_permite_firmar()
    {
        var factory = await CrearFactoryAsync();
        var xades = new XadesSpy();
        var service = new FirmadorXmlComprobanteElectronico(
            factory, new StorageFake(),
            new ValidatorFake(DesconocidoUtilizable()), xades);
        await using var xml = Xml();

        await using var signed = await service.FirmarAsync(xml, 7);

        Assert.Equal(1, xades.Firmas);
        Assert.True(signed.Length > 0);
    }

    private static async Task<TestDbContextFactory> CrearFactoryAsync()
    {
        var options = new DbContextOptionsBuilder<KontaxDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var factory = new TestDbContextFactory(options);
        await using var context = factory.CreateDbContext();
        context.Empresas.Add(new Empresa
        {
            Id = 7,
            RegimenTributarioId = 1,
            NumeroIdentificacion = "1790012345001",
            RazonSocial = "EMPRESA PRUEBA",
            Estado = 1,
            CreatedAt = DateTime.UtcNow
        });
        context.FacturacionesElectronicas.Add(new(
        )
        {
            Id = 8,
            EmpresaId = 7,
            TipoAmbienteId = 1,
            TipoEmisionId = 1,
            CertificadoReferencia = "ref",
            Habilitada = true,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();
        return factory;
    }

    private static MemoryStream Xml() => new(
        "<factura id='comprobante'/>"u8.ToArray());

    private static ResultadoValidacionCertificadoSri Revocado() =>
        new(false, null, ["El certificado está revocado."])
        {
            Estado = EstadoCertificadoSri.Revocado,
            EstadoCadena = EstadoCadenaCertificadoSri.Valida,
            EstadoRevocacion = EstadoRevocacionCertificadoSri.Revocado
        };

    private static ResultadoValidacionCertificadoSri DesconocidoUtilizable() =>
        new(true, null, [])
        {
            Estado = EstadoCertificadoSri.EstadoRevocacionDesconocido,
            EstadoCadena = EstadoCadenaCertificadoSri.Valida,
            EstadoRevocacion = EstadoRevocacionCertificadoSri.Desconocido,
            Advertencias = ["CRL/OCSP no disponible"]
        };

    private sealed class TestDbContextFactory(
        DbContextOptions<KontaxDbContext> options)
        : IDbContextFactory<KontaxDbContext>
    {
        public KontaxDbContext CreateDbContext() => new(options);
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
            Task.FromResult(new MaterialCertificadoSri([1], "secreto"));

        public Task EliminarAsync(long empresaId, string referencia,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class ValidatorFake(
        ResultadoValidacionCertificadoSri result)
        : IValidadorCertificadoSri
    {
        public ResultadoValidacionCertificadoSri Validar(
            ReadOnlyMemory<byte> pkcs12,
            ReadOnlySpan<char> password,
            string? rucEsperado = null) => result;
    }

    private sealed class XadesSpy : IFirmadorXadesSri
    {
        public int Firmas { get; private set; }

        public XmlDocument Firmar(XmlDocument documento,
            ReadOnlyMemory<byte> pkcs12,
            ReadOnlySpan<char> password,
            DateTimeOffset? fechaFirma = null)
        {
            Firmas++;
            return documento;
        }

        public ResultadoVerificacionFirmaSri Verificar(
            XmlDocument documentoFirmado) => new(true, []);
    }
}
