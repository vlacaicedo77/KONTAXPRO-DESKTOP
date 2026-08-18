using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ValidadorXmlSri : IValidadorXmlSri
{
    private const string RecursoFactura = "KONTAXPRO.Infrastructure.FacturacionElectronica.Schemas.factura_V2.1.0.xsd";
    private const string RecursoFirma = "KONTAXPRO.Infrastructure.FacturacionElectronica.Schemas.xmldsig-core-schema.xsd";
    private readonly XmlSchemaSet schemas;

    public ValidadorXmlSri()
    {
        schemas = new XmlSchemaSet { XmlResolver = null };
        var assembly = Assembly.GetExecutingAssembly();
        using var firma = assembly.GetManifestResourceStream(RecursoFirma)
            ?? throw new InvalidOperationException("No se encontró el esquema XMLDSIG integrado.");
        using var factura = assembly.GetManifestResourceStream(RecursoFactura)
            ?? throw new InvalidOperationException("No se encontró el XSD oficial de factura integrado.");
        schemas.Add("http://www.w3.org/2000/09/xmldsig#", XmlReader.Create(firma, LecturaSegura()));
        schemas.Add(null, XmlReader.Create(factura, LecturaSegura()));
        schemas.Compile();
    }

    public ResultadoValidacionXmlSri ValidarFactura(XmlDocument documento)
    {
        ArgumentNullException.ThrowIfNull(documento);
        var errores = new List<string>();
        var settings = LecturaSegura();
        settings.ValidationType = ValidationType.Schema;
        settings.Schemas = schemas;
        settings.ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings;
        // El XSD oficial de XMLDSIG declara el contenido de ds:Object con
        // processContents="lax". Sin el esquema ETSI completo, .NET informa
        // los nodos XAdES como advertencias, no como errores del comprobante.
        // Conservamos la validación estricta para cualquier error XSD real.
        settings.ValidationEventHandler += (_, e) =>
        {
            if (e.Severity == XmlSeverityType.Error)
                errores.Add(e.Message);
        };

        using var stringReader = new StringReader(documento.OuterXml);
        using var reader = XmlReader.Create(stringReader, settings);
        try
        {
            while (reader.Read()) { }
        }
        catch (XmlException ex)
        {
            errores.Add(ex.Message);
        }

        return new ResultadoValidacionXmlSri(errores.Count == 0, errores);
    }

    private static XmlReaderSettings LecturaSegura() => new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        MaxCharactersFromEntities = 0,
        MaxCharactersInDocument = 10_000_000
    };
}
