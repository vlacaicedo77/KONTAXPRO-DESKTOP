using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using KONTAXPRO.Application.FacturacionElectronica;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Infrastructure.FacturacionElectronica;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class MotorFacturaElectronicaSriTests
{
    private static readonly DateTimeOffset Ahora =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public async Task Flujo_simulado_genera_valida_firma_envia_autoriza_y_persiste()
    {
        var time = new TiempoFijo(Ahora);
        var validator = new ValidadorCertificadoSri(time, _ => true);
        var signer = new FirmadorXadesSri(validator, time);
        var reception = new RecepcionFake();
        var authorization = new AutorizacionFake();
        var motor = new MotorFacturaElectronicaSri(
            new GeneradorXmlFacturaSri(), new ValidadorXmlSri(), validator,
            signer,
            reception, authorization);
        var certificate = CrearCertificado();
        var invoice = CrearFactura();

        var result = await motor.ProcesarAsync(invoice, certificate, "secreto");

        Assert.Equal("RECIBIDA", result.Recepcion.Estado);
        Assert.Equal("AUTORIZADO", result.Autorizacion?.Estado);
        Assert.Equal(invoice.ClaveAcceso, reception.ClaveRecibida);
        Assert.Contains("<Signature", Encoding.UTF8.GetString(result.XmlFirmado));

        var directory = Path.Combine(Path.GetTempPath(),
            "kontaxpro-sri-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var storage = new AlmacenamientoDocumentosElectronicos(
                new ContextoInstalacionLocal(directory, "SERVIDOR"));
            var artifact = new ArtefactoElectronico(
                7, invoice.RucEmisor, invoice.ClaveAcceso, "GENERADO");
            await GuardarAsync(storage, artifact, result.XmlGenerado);
            await GuardarAsync(storage, artifact with { TipoArtefacto = "FIRMADO" },
                result.XmlFirmado);
            await GuardarAsync(storage, artifact with { TipoArtefacto = "AUTORIZADO" },
                Encoding.UTF8.GetBytes(result.Autorizacion!.XmlAutorizado!));

            await using var stored = await storage.AbrirLecturaAsync(
                artifact with { TipoArtefacto = "FIRMADO" });
            using var memory = new MemoryStream();
            await stored.CopyToAsync(memory);
            Assert.Equal(result.XmlFirmado, memory.ToArray());
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            CryptographicOperations.ZeroMemory(certificate);
        }
    }

    [Fact]
    public void Dpapi_protege_el_secreto_y_lo_vincula_al_proposito()
    {
        var protector = new ProtectorSecretosWindows();
        var clear = Encoding.UTF8.GetBytes("clave temporal de prueba");
        var encrypted = protector.Proteger(clear, "SRI:P12:EMPRESA:7");
        try
        {
            Assert.NotEqual(clear, encrypted);
            Assert.Equal(clear,
                protector.Desproteger(encrypted, "SRI:P12:EMPRESA:7"));
            Assert.Throws<CryptographicException>(() =>
                protector.Desproteger(encrypted, "SRI:P12:EMPRESA:8"));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(clear);
            CryptographicOperations.ZeroMemory(encrypted);
        }
    }

    private static async Task GuardarAsync(
        IAlmacenamientoDocumentosElectronicos storage,
        ArtefactoElectronico artifact,
        byte[] content)
    {
        await using var stream = new MemoryStream(content, writable: false);
        await storage.GuardarAsync(artifact, stream);
    }

    private static FacturaElectronicaSri CrearFactura()
    {
        var accessKey = new GeneradorClaveAccesoSri().Generar(new(
            new DateOnly(2026, 8, 16), "01", "1790012345001", 1,
            "001", "001", 1, "12345678"));
        return new(accessKey, 1, 1, "1790012345001", "EMPRESA DE PRUEBA",
            "KONTAXPRO TEST", "DIRECCIÓN MATRIZ", "001", "001", 1,
            new DateOnly(2026, 8, 16), "DIRECCIÓN LOCAL", null, true, null,
            "05", "CLIENTE PRUEBA", "2300257942", "DIRECCIÓN CLIENTE",
            10m, 0m, 11.50m,
            [new("2", "4", 10m, 15m, 1.50m)],
            [new("PRODUCTO DE PRUEBA", 1m, 10m, 0m, 10m,
                [new("2", "4", 15m, 10m, 1.50m)], "P001")],
            [new("01", 11.50m)],
            [new("Email", "cliente@kontax.com")]);
    }

    private static byte[] CrearCertificado()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=EMPRESA DE PRUEBA, SERIALNUMBER=1790012345001", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            true));
        using var certificate = request.CreateSelfSigned(
            Ahora.AddDays(-1), Ahora.AddYears(1));
        return certificate.Export(X509ContentType.Pkcs12, "secreto");
    }

    private sealed class RecepcionFake : IClienteRecepcionSri
    {
        public string? ClaveRecibida { get; private set; }

        public Task<ResultadoSri> EnviarAsync(int ambiente, string claveAcceso,
            ReadOnlyMemory<byte> xmlFirmado,
            CancellationToken cancellationToken = default)
        {
            ClaveRecibida = claveAcceso;
            Assert.Equal(1, ambiente);
            Assert.False(xmlFirmado.IsEmpty);
            return Task.FromResult(new ResultadoSri(true, "RECIBIDA", null,
                null, []));
        }
    }

    private sealed class AutorizacionFake : IClienteAutorizacionSri
    {
        public Task<ResultadoSri> ConsultarAsync(int ambiente,
            string claveAcceso, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ResultadoSri(true, "AUTORIZADO", claveAcceso,
                Ahora.UtcDateTime, [],
                $"<autorizacion><estado>AUTORIZADO</estado>" +
                $"<numeroAutorizacion>{claveAcceso}</numeroAutorizacion></autorizacion>"));
    }

    private sealed class TiempoFijo(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone(
                "ECT", TimeSpan.FromHours(-5), "ECT", "ECT");
    }
}
