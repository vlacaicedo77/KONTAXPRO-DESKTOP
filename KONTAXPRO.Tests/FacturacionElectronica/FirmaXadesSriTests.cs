using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using KONTAXPRO.Application.FacturacionElectronica;
using KONTAXPRO.Application.Models.FacturacionElectronica;
using KONTAXPRO.Infrastructure.FacturacionElectronica;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class FirmaXadesSriTests
{
    private static readonly DateTimeOffset Ahora =
        new(2026, 8, 16, 12, 0, 0, TimeSpan.FromHours(-5));

    [Fact]
    public void Valida_pkcs12_clave_privada_rsa_vigencia_y_ruc()
    {
        var p12 = CrearCertificado(Ahora.AddDays(-1), Ahora.AddYears(1));
        var servicio = new ValidadorCertificadoSri(
            new TiempoFijo(Ahora), _ => true);

        var resultado = servicio.Validar(p12, "secreto", "1790012345001");

        Assert.True(resultado.EsValido, string.Join(" ", resultado.Errores));
        Assert.NotNull(resultado.Certificado);
        Assert.True(resultado.Certificado.TieneClavePrivada);
        Assert.True(resultado.Certificado.EsRsa);
        Assert.True(resultado.Certificado.Vigente);
    }

    [Fact]
    public void Rechaza_password_incorrecto_certificado_vencido_y_certificado_sin_clave()
    {
        var validador = new ValidadorCertificadoSri(
            new TiempoFijo(Ahora), _ => true);
        var vigente = CrearCertificado(Ahora.AddDays(-1), Ahora.AddYears(1));
        Assert.False(validador.Validar(vigente, "incorrecto").EsValido);

        var vencido = CrearCertificado(Ahora.AddYears(-2), Ahora.AddYears(-1));
        Assert.False(validador.Validar(vencido, "secreto").EsValido);

        using var publico = X509CertificateLoader.LoadCertificate(
            ObtenerCertificadoPublico(vigente));
        var sinClave = publico.Export(X509ContentType.Pkcs12, "secreto");
        Assert.False(validador.Validar(sinClave, "secreto").EsValido);
    }

    [Fact]
    public void Revocacion_inaccesible_es_advertencia_y_no_revocacion_confirmada()
    {
        var chain = ValidadorCertificadoSri.InterpretarEstadosCadena(false,
        [
            new X509ChainStatus
            {
                Status = X509ChainStatusFlags.RevocationStatusUnknown |
                         X509ChainStatusFlags.OfflineRevocation,
                StatusInformation = "Servicio sin conexión"
            }
        ]);
        var validator = new ValidadorCertificadoSri(
            new TiempoFijo(Ahora), _ => chain);

        var result = validator.Validar(
            CrearCertificado(Ahora.AddDays(-1), Ahora.AddYears(1)),
            "secreto", "1790012345001");

        Assert.True(result.EsValido);
        Assert.False(result.ValidacionCompleta);
        Assert.Equal(EstadoCadenaCertificadoSri.Valida,
            result.EstadoCadena);
        Assert.Equal(EstadoRevocacionCertificadoSri.Desconocido,
            result.EstadoRevocacion);
        Assert.Equal(EstadoCertificadoSri.EstadoRevocacionDesconocido,
            result.Estado);
        Assert.NotEmpty(result.Advertencias);
        Assert.DoesNotContain(result.Errores, x =>
            x.Contains("revocado", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Revocacion_confirmada_bloquea_certificado()
    {
        var chain = ValidadorCertificadoSri.InterpretarEstadosCadena(false,
        [
            new X509ChainStatus
            {
                Status = X509ChainStatusFlags.Revoked,
                StatusInformation = "Revocado"
            }
        ]);
        var validator = new ValidadorCertificadoSri(
            new TiempoFijo(Ahora), _ => chain);

        var result = validator.Validar(
            CrearCertificado(Ahora.AddDays(-1), Ahora.AddYears(1)),
            "secreto", "1790012345001");

        Assert.False(result.EsValido);
        Assert.Equal(EstadoRevocacionCertificadoSri.Revocado,
            result.EstadoRevocacion);
        Assert.Equal(EstadoCertificadoSri.Revocado, result.Estado);
        Assert.Contains(result.Errores, x =>
            x.Contains("revocado", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(X509ChainStatusFlags.UntrustedRoot,
        "autoridad raíz")]
    [InlineData(X509ChainStatusFlags.PartialChain,
        "certificado intermedio")]
    public void Cadena_no_disponible_se_conserva_como_diagnostico_no_bloqueante(
        X509ChainStatusFlags status,
        string expected)
    {
        var chain = ValidadorCertificadoSri.InterpretarEstadosCadena(false,
        [
            new X509ChainStatus
            {
                Status = status,
                StatusInformation = status.ToString()
            }
        ]);

        Assert.Equal(EstadoCadenaCertificadoSri.NoValida,
            chain.EstadoCadena);
        Assert.Contains(chain.Detalles, x =>
            x.Contains(expected, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(chain.Errores);

        var validator = new ValidadorCertificadoSri(
            new TiempoFijo(Ahora), _ => chain);
        var result = validator.Validar(
            CrearCertificado(Ahora.AddDays(-1), Ahora.AddYears(1)),
            "secreto", "1790012345001");

        Assert.True(result.EsValido);
        Assert.DoesNotContain(result.Errores, x =>
            x.Contains("cadena", StringComparison.OrdinalIgnoreCase));
        if (status == X509ChainStatusFlags.PartialChain)
        {
            Assert.Equal(EstadoRevocacionCertificadoSri.NoComprobado,
                result.EstadoRevocacion);
            Assert.NotEmpty(result.Advertencias);
        }
    }

    [Fact]
    public void Certificado_proximo_a_caducar_conserva_validez_con_advertencia()
    {
        var validator = new ValidadorCertificadoSri(
            new TiempoFijo(Ahora), _ => true);

        var result = validator.Validar(
            CrearCertificado(Ahora.AddDays(-1), Ahora.AddDays(20)),
            "secreto", "1790012345001");

        Assert.True(result.EsValido);
        Assert.Equal(EstadoCertificadoSri.ProximoACaducar, result.Estado);
        Assert.InRange(result.Certificado!.DiasParaCaducar, 19, 20);
    }

    [Fact]
    public void Firma_xades_con_tres_referencias_y_verifica_localmente()
    {
        var (firmador, p12) = CrearFirmador();
        var documento = CrearDocumento();

        var firmado = firmador.Firmar(documento, p12, "secreto", Ahora);
        var resultado = firmador.Verificar(firmado);

        Assert.True(resultado.EsValida, string.Join(" ", resultado.Errores));
        var ns = Ns(firmado);
        Assert.Equal(3, firmado.SelectNodes("//ds:SignedInfo/ds:Reference", ns)!.Count);
        Assert.Single(firmado.SelectNodes("//etsi:SignedProperties", ns)!.Cast<XmlNode>());
        Assert.Equal("comprobante", firmado.DocumentElement!.GetAttribute("id"));
        var objectReference = firmado.SelectSingleNode(
            "//etsi:DataObjectFormat", ns) as XmlElement;
        var documentReferenceId = firmado.SelectSingleNode(
            "//ds:Reference[@URI='#comprobante']", ns) as XmlElement;
        Assert.Equal($"#{documentReferenceId!.GetAttribute("Id")}",
            objectReference!.GetAttribute("ObjectReference"));

        var signature = (XmlElement)firmado.GetElementsByTagName(
            "Signature", SignedXml.XmlDsigNamespaceUrl)[0]!;
        var standardVerifier = new SignedXml(firmado);
        standardVerifier.LoadXml(signature);
        using var certificate = X509CertificateLoader.LoadPkcs12(
            p12, "secreto", X509KeyStorageFlags.EphemeralKeySet);
        Assert.True(standardVerifier.CheckSignature(certificate, true));
    }

    [Theory]
    [InlineData("/factura/infoFactura/importeTotal")]
    [InlineData("//etsi:SigningTime")]
    [InlineData("//ds:KeyInfo/ds:X509Data/ds:X509SubjectName")]
    public void Alterar_documento_signed_properties_o_keyinfo_invalida_firma(string xpath)
    {
        var (firmador, p12) = CrearFirmador();
        var firmado = firmador.Firmar(CrearDocumento(), p12, "secreto", Ahora);
        var node = firmado.SelectSingleNode(xpath, Ns(firmado))!;
        node.InnerText += " ALTERADO";

        Assert.False(firmador.Verificar(firmado).EsValida);
    }

    [Fact]
    public void Rechaza_identificador_duplicado()
    {
        var (firmador, p12) = CrearFirmador();
        var documento = CrearDocumento();
        var duplicado = documento.CreateElement("dato");
        duplicado.SetAttribute("Id", "comprobante");
        documento.DocumentElement!.AppendChild(duplicado);

        Assert.Throws<CryptographicException>(() =>
            firmador.Firmar(documento, p12, "secreto", Ahora));
    }

    private static (FirmadorXadesSri Firmador, byte[] P12) CrearFirmador()
    {
        var time = new TiempoFijo(Ahora);
        return (new(new ValidadorCertificadoSri(time, _ => true), time),
            CrearCertificado(Ahora.AddDays(-1), Ahora.AddYears(1)));
    }

    private static byte[] CrearCertificado(DateTimeOffset desde, DateTimeOffset hasta)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=EMPRESA DE PRUEBA, SERIALNUMBER=1790012345001",
            rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            true));
        using var certificate = request.CreateSelfSigned(desde, hasta);
        return certificate.Export(X509ContentType.Pkcs12, "secreto");
    }

    private static byte[] ObtenerCertificadoPublico(byte[] p12)
    {
        using var certificate = X509CertificateLoader.LoadPkcs12(
            p12, "secreto", X509KeyStorageFlags.EphemeralKeySet);
        return certificate.Export(X509ContentType.Cert);
    }

    private static XmlDocument CrearDocumento()
    {
        var clave = new GeneradorClaveAccesoSri().Generar(new(
            new DateOnly(2026, 8, 16), "01", "1790012345001", 1,
            "001", "001", 1, "12345678"));
        var factura = new FacturaElectronicaSri(
            clave, 1, 1, "1790012345001", "EMPRESA DE PRUEBA", null,
            "DIRECCION MATRIZ", "001", "001", 1,
            new DateOnly(2026, 8, 16), "DIRECCION LOCAL", null, true,
            null, "05", "CLIENTE PRUEBA", "2300257942", null,
            10, 0, 11.50m, [new("2", "4", 10, 15, 1.50m)],
            [new("PRODUCTO", 1, 10, 0, 10,
                [new("2", "4", 15, 10, 1.50m)], "P001")],
            [new("01", 11.50m)], []);
        return new GeneradorXmlFacturaSri().Generar(factura);
    }

    private static XmlNamespaceManager Ns(XmlDocument document)
    {
        var ns = new XmlNamespaceManager(document.NameTable);
        ns.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");
        ns.AddNamespace("etsi", "http://uri.etsi.org/01903/v1.3.2#");
        return ns;
    }

    private sealed class TiempoFijo(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone =>
            TimeZoneInfo.CreateCustomTimeZone("ECT", TimeSpan.FromHours(-5), "ECT", "ECT");
    }
}
