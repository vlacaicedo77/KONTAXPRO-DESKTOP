using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class FirmadorXadesSri(
    IValidadorCertificadoSri validadorCertificado,
    TimeProvider timeProvider) : IFirmadorXadesSri
{
    private const string Ds = SignedXml.XmlDsigNamespaceUrl;
    private const string Xades = "http://uri.etsi.org/01903/v1.3.2#";
    private const string SignedPropertiesType = "http://uri.etsi.org/01903#SignedProperties";

    public XmlDocument Firmar(
        XmlDocument documento,
        ReadOnlyMemory<byte> pkcs12,
        ReadOnlySpan<char> password,
        DateTimeOffset? fechaFirma = null)
    {
        ArgumentNullException.ThrowIfNull(documento);
        ValidarDocumentoFirmable(documento);
        var resultado = validadorCertificado.ValidarLocal(pkcs12, password);
        if (!resultado.EsValido)
            throw new CryptographicException(string.Join(" ", resultado.Errores));

        using var certificate = X509CertificateLoader.LoadPkcs12(
            pkcs12.Span, password,
            X509KeyStorageFlags.EphemeralKeySet |
            X509KeyStorageFlags.Exportable);
        using var rsa = certificate.GetRSAPrivateKey()
            ?? throw new CryptographicException("El certificado no contiene una clave privada RSA.");

        var token = RandomNumberGenerator.GetInt32(1, int.MaxValue)
            .ToString(CultureInfo.InvariantCulture);
        var signatureId = $"Signature{token}";
        var signedInfoId = $"Signature-SignedInfo{token}";
        var signatureValueId = $"SignatureValue{token}";
        var keyInfoId = $"Certificate{token}";
        var objectId = $"Signature-Object{token}";
        var signedPropertiesId = $"Signature{token}-SignedProperties";
        var referenceId = $"Reference-ID-{token}";

        var signedXml = new SriSignedXml(documento)
        {
            SigningKey = rsa
        };
        signedXml.Signature.Id = signatureId;
        var signedInfo = signedXml.SignedInfo
            ?? throw new CryptographicException("No se pudo crear SignedInfo.");
        signedInfo.Id = signedInfoId;
        signedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedInfo.SignatureMethod = SignedXml.XmlDsigRSASHA1Url;

        var keyInfo = CrearKeyInfo(certificate, rsa, keyInfoId);
        signedXml.KeyInfo = keyInfo;

        var objectElement = CrearPropiedadesXades(
            documento, certificate, signatureId, signedPropertiesId,
            referenceId, fechaFirma ?? timeProvider.GetLocalNow());
        var objectContainer = documento.CreateElement("xadesContainer");
        objectContainer.AppendChild(objectElement);
        var dataObject = new DataObject
        {
            Id = objectId,
            Data = objectContainer.ChildNodes
        };
        signedXml.AddObject(dataObject);

        var documentReference = new Reference("#comprobante")
        {
            Id = referenceId,
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };
        documentReference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        documentReference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(documentReference);

        var keyReference = new Reference($"#{keyInfoId}")
        {
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };
        keyReference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(keyReference);

        var propertiesReference = new Reference($"#{signedPropertiesId}")
        {
            Type = SignedPropertiesType,
            DigestMethod = SignedXml.XmlDsigSHA1Url
        };
        propertiesReference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(propertiesReference);

        // SriSignedXml resuelve también los identificadores que todavía viven
        // dentro de KeyInfo y ds:Object. Así SignedXml calcula directamente la
        // firma definitiva, sin nodos temporales ni recálculos manuales.
        signedXml.ComputeSignature();
        var signature = signedXml.GetXml();
        var signatureValue = signature.SelectSingleNode(
            "ds:SignatureValue", CrearNs(signature.OwnerDocument!.NameTable)) as XmlElement
            ?? throw new CryptographicException("No se generó SignatureValue.");
        signatureValue.SetAttribute("Id", signatureValueId);
        documento.DocumentElement!.AppendChild(documento.ImportNode(signature, true));

        var verificacion = Verificar(documento);
        if (!verificacion.EsValida)
            throw new CryptographicException(
                "La verificación local de la firma falló: " +
                string.Join(" ", verificacion.Errores));
        return documento;
    }

    public ResultadoVerificacionFirmaSri Verificar(XmlDocument documentoFirmado)
    {
        var errores = new List<string>();
        try
        {
            ValidarDocumentoFirmable(documentoFirmado, requiereSinFirma: false);
            var firmas = documentoFirmado.GetElementsByTagName("Signature", Ds);
            if (firmas.Count != 1)
                return new(false, ["El XML debe contener exactamente una firma."]);
            var signatureElement = (XmlElement)firmas[0]!;
            var signedXml = new SriSignedXml(documentoFirmado);
            signedXml.LoadXml(signatureElement);

            var references = (signedXml.SignedInfo
                ?? throw new CryptographicException("La firma no contiene SignedInfo."))
                .References.Cast<Reference>().ToArray();
            if (references.Length != 3)
                errores.Add("La firma debe contener exactamente tres referencias.");
            foreach (var reference in references)
            {
                if (string.IsNullOrWhiteSpace(reference.Uri) ||
                    !reference.Uri.StartsWith('#'))
                    errores.Add("Todas las referencias de firma deben ser internas.");
            }
            if (!references.Any(x => x.Uri == "#comprobante"))
                errores.Add("Falta la referencia al comprobante.");
            if (!references.Any(x => x.Type == SignedPropertiesType))
                errores.Add("Falta la referencia SignedProperties.");

            var certificateNode = signatureElement.SelectSingleNode(
                ".//ds:X509Certificate", CrearNs(documentoFirmado.NameTable));
            if (certificateNode is null)
                errores.Add("La firma no contiene el certificado.");
            else
            {
                using var cert = X509CertificateLoader.LoadCertificate(
                    Convert.FromBase64String(certificateNode.InnerText));
                if (!signedXml.CheckSignature(cert, true))
                {
                    DiagnosticarFirma(documentoFirmado, signatureElement, cert, errores);
                }
            }
        }
        catch (Exception ex) when (ex is CryptographicException or XmlException or FormatException)
        {
            errores.Add(ex.Message);
        }
        return new(errores.Count == 0, errores);
    }

    private static KeyInfo CrearKeyInfo(X509Certificate2 certificate, RSA rsa, string id)
    {
        var keyInfo = new KeyInfo { Id = id };
        var x509 = new KeyInfoX509Data(certificate);
        x509.AddSubjectName(certificate.Subject);
        x509.AddIssuerSerial(certificate.Issuer, NumeroSerieDecimal(certificate));
        keyInfo.AddClause(x509);
        keyInfo.AddClause(new RSAKeyValue(rsa));
        return keyInfo;
    }

    private static XmlElement CrearPropiedadesXades(
        XmlDocument owner,
        X509Certificate2 certificate,
        string signatureId,
        string signedPropertiesId,
        string documentReferenceId,
        DateTimeOffset signingTime)
    {
        var objectDocument = new XmlDocument { PreserveWhitespace = true };
        var qualifying = objectDocument.CreateElement("etsi", "QualifyingProperties", Xades);
        qualifying.SetAttribute("Target", $"#{signatureId}");
        var signedProperties = objectDocument.CreateElement("etsi", "SignedProperties", Xades);
        signedProperties.SetAttribute("Id", signedPropertiesId);
        var signedSignatureProperties = objectDocument.CreateElement("etsi", "SignedSignatureProperties", Xades);
        AddText(objectDocument, signedSignatureProperties, Xades, "etsi", "SigningTime",
            signingTime.ToString("yyyy-MM-dd'T'HH:mm:sszzz", CultureInfo.InvariantCulture));

        var signingCertificate = objectDocument.CreateElement("etsi", "SigningCertificate", Xades);
        var cert = objectDocument.CreateElement("etsi", "Cert", Xades);
        var certDigest = objectDocument.CreateElement("etsi", "CertDigest", Xades);
        var digestMethod = objectDocument.CreateElement("ds", "DigestMethod", Ds);
        digestMethod.SetAttribute("Algorithm", SignedXml.XmlDsigSHA1Url);
        certDigest.AppendChild(digestMethod);
        AddText(objectDocument, certDigest, Ds, "ds", "DigestValue",
            Convert.ToBase64String(SHA1.HashData(certificate.RawData)));
        cert.AppendChild(certDigest);
        var issuerSerial = objectDocument.CreateElement("etsi", "IssuerSerial", Xades);
        AddText(objectDocument, issuerSerial, Ds, "ds", "X509IssuerName", certificate.Issuer);
        AddText(objectDocument, issuerSerial, Ds, "ds", "X509SerialNumber", NumeroSerieDecimal(certificate));
        cert.AppendChild(issuerSerial);
        signingCertificate.AppendChild(cert);
        signedSignatureProperties.AppendChild(signingCertificate);
        signedProperties.AppendChild(signedSignatureProperties);

        var signedDataObjectProperties = objectDocument.CreateElement("etsi", "SignedDataObjectProperties", Xades);
        var dataObjectFormat = objectDocument.CreateElement("etsi", "DataObjectFormat", Xades);
        dataObjectFormat.SetAttribute("ObjectReference", $"#{documentReferenceId}");
        AddText(objectDocument, dataObjectFormat, Xades, "etsi", "Description", "contenido comprobante");
        AddText(objectDocument, dataObjectFormat, Xades, "etsi", "MimeType", "text/xml");
        signedDataObjectProperties.AppendChild(dataObjectFormat);
        signedProperties.AppendChild(signedDataObjectProperties);
        qualifying.AppendChild(signedProperties);
        objectDocument.AppendChild(qualifying);
        return (XmlElement)owner.ImportNode(qualifying, true);
    }

    private static void AddText(XmlDocument document, XmlElement parent,
        string ns, string prefix, string name, string value)
    {
        var node = document.CreateElement(prefix, name, ns);
        node.InnerText = value;
        parent.AppendChild(node);
    }

    private static string NumeroSerieDecimal(X509Certificate2 certificate)
    {
        var littleEndian = certificate.GetSerialNumber();
        var unsigned = new byte[littleEndian.Length + 1];
        littleEndian.CopyTo(unsigned, 0);
        return new BigInteger(unsigned).ToString(CultureInfo.InvariantCulture);
    }

    private static XmlNamespaceManager CrearNs(XmlNameTable table)
    {
        var ns = new XmlNamespaceManager(table);
        ns.AddNamespace("ds", Ds);
        return ns;
    }

    private static void DiagnosticarFirma(
        XmlDocument document,
        XmlElement signature,
        X509Certificate2 certificate,
        List<string> errores)
    {
        var ns = CrearNs(document.NameTable);
        var signedInfo = (XmlElement)signature.SelectSingleNode("ds:SignedInfo", ns)!;
        var signatureValue = Convert.FromBase64String(
            signature.SelectSingleNode("ds:SignatureValue", ns)!.InnerText);
        using var rsa = certificate.GetRSAPublicKey();
        if (rsa is null || !rsa.VerifyHash(
                SHA1.HashData(Canonicalizar(signedInfo)), signatureValue,
                HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
            errores.Add("SignatureValue no es válido.");

        foreach (XmlElement reference in signedInfo.SelectNodes("ds:Reference", ns)!.Cast<XmlElement>())
        {
            var uri = reference.GetAttribute("URI");
            byte[] canonical;
            if (uri == "#comprobante")
            {
                var clone = new XmlDocument { PreserveWhitespace = true };
                clone.LoadXml(document.OuterXml);
                var cloneSignature = clone.SelectSingleNode(
                    "/factura/ds:Signature", CrearNs(clone.NameTable));
                cloneSignature!.ParentNode!.RemoveChild(cloneSignature);
                canonical = Canonicalizar(clone);
            }
            else
            {
                canonical = Canonicalizar(ResolverIdUnico(document, uri[1..]));
            }
            var esperado = Convert.FromBase64String(
                reference.SelectSingleNode("ds:DigestValue", ns)!.InnerText);
            if (!CryptographicOperations.FixedTimeEquals(
                    esperado, SHA1.HashData(canonical)))
                errores.Add($"El digest de {uri} no es válido.");
        }
    }

    private static XmlElement ResolverIdUnico(XmlDocument document, string id)
    {
        var matches = document.SelectNodes("//*[@Id or @ID or @id]")!
            .OfType<XmlElement>()
            .Where(x => x.GetAttribute("Id") == id ||
                        x.GetAttribute("ID") == id ||
                        x.GetAttribute("id") == id)
            .ToArray();
        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new CryptographicException(
                $"No existe el identificador XML '{id}'."),
            _ => throw new CryptographicException(
                $"El identificador XML '{id}' está duplicado.")
        };
    }

    private static byte[] Canonicalizar(XmlNode node)
    {
        var clone = new XmlDocument { PreserveWhitespace = true };
        if (node is XmlDocument document)
            clone.LoadXml(document.OuterXml);
        else
            clone.LoadXml(node.OuterXml);
        var transform = new XmlDsigC14NTransform(false);
        transform.LoadInput(clone);
        using var stream = (Stream)transform.GetOutput(typeof(Stream));
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static void ValidarDocumentoFirmable(
        XmlDocument document,
        bool requiereSinFirma = true)
    {
        if (document.DocumentElement?.Name != "factura")
            throw new XmlException("El documento raíz debe ser factura.");
        var ids = document.SelectNodes("//*[@Id or @ID or @id]")!
            .OfType<XmlElement>()
            .SelectMany(x => new[] { x.GetAttribute("Id"), x.GetAttribute("ID"), x.GetAttribute("id") })
            .Where(x => !string.IsNullOrEmpty(x))
            .GroupBy(x => x, StringComparer.Ordinal)
            .FirstOrDefault(x => x.Count() > 1);
        if (ids is not null)
            throw new CryptographicException($"El identificador XML '{ids.Key}' está duplicado.");
        if (document.DocumentElement.GetAttribute("id") != "comprobante")
            throw new XmlException("La factura debe declarar id=\"comprobante\".");
        if (requiereSinFirma && document.GetElementsByTagName("Signature", Ds).Count > 0)
            throw new CryptographicException("El XML ya contiene una firma.");
    }
}
