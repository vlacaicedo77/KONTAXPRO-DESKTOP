using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

internal sealed class SriSignedXml : SignedXml
{
    public SriSignedXml(XmlDocument document) : base(document) { }

    public override XmlElement? GetIdElement(XmlDocument? document, string idValue)
    {
        if (document is null || string.IsNullOrWhiteSpace(idValue))
            return null;

        var matches = document.SelectNodes(
            "//*[@Id or @ID or @id]")!
            .OfType<XmlElement>()
            .Where(x => x.GetAttribute("Id") == idValue ||
                        x.GetAttribute("ID") == idValue ||
                        x.GetAttribute("id") == idValue)
            .ToArray();
        if (matches.Length > 1)
            throw new CryptographicException(
                $"El identificador XML '{idValue}' está duplicado.");
        if (matches.Length == 1)
            return matches[0];

        var fromBase = base.GetIdElement(document, idValue);
        if (fromBase is not null)
            return fromBase;

        if (Signature.KeyInfo?.Id == idValue)
            return Signature.KeyInfo.GetXml();

        foreach (DataObject dataObject in Signature.ObjectList)
        {
            var objectElement = dataObject.GetXml();
            var objectMatches = objectElement.SelectNodes(
                    ".//*[@Id or @ID or @id]")!
                .OfType<XmlElement>()
                .Where(x => x.GetAttribute("Id") == idValue ||
                            x.GetAttribute("ID") == idValue ||
                            x.GetAttribute("id") == idValue)
                .ToArray();
            if (objectMatches.Length > 1)
                throw new CryptographicException(
                    $"El identificador XML '{idValue}' está duplicado.");
            if (objectMatches.Length == 1)
                return objectMatches[0];
        }

        return null;
    }
}
