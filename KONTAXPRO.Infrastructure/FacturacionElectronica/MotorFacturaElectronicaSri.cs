using System.Text;
using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class MotorFacturaElectronicaSri(
    IGeneradorXmlFacturaSri generadorXml,
    IValidadorXmlSri validadorXml,
    IValidadorCertificadoSri validadorCertificado,
    IFirmadorXadesSri firmador,
    IClienteRecepcionSri recepcion,
    IClienteAutorizacionSri autorizacion)
    : IMotorFacturaElectronicaSri
{
    public async Task<ResultadoMotorFacturaSri> ProcesarAsync(
        FacturaElectronicaSri factura,
        ReadOnlyMemory<byte> pkcs12,
        string password,
        CancellationToken cancellationToken = default)
    {
        var certificateValidation = await validadorCertificado.ValidarAsync(
            pkcs12, password, factura.RucEmisor, false, cancellationToken);
        if (!certificateValidation.EsValido)
            throw new InvalidOperationException(
                "El certificado electrónico no puede utilizarse: " +
                string.Join(" | ", certificateValidation.Errores));

        var document = generadorXml.Generar(factura);
        var validation = validadorXml.ValidarFactura(document);
        if (!validation.EsValido)
            throw new InvalidOperationException(
                "El XML de factura no cumple el XSD oficial: " +
                string.Join(" | ", validation.Errores));
        var generated = Serializar(document);
        var signed = firmador.Firmar(document, pkcs12, password);
        var signedValidation = validadorXml.ValidarFactura(signed);
        if (!signedValidation.EsValido)
            throw new InvalidOperationException(
                "El XML firmado no cumple el XSD oficial: " +
                string.Join(" | ", signedValidation.Errores));
        var verification = firmador.Verificar(signed);
        if (!verification.EsValida)
            throw new InvalidOperationException(
                "La firma local no es válida: " + string.Join(" | ", verification.Errores));
        var signedBytes = Serializar(signed);
        var reception = await recepcion.EnviarAsync(
            factura.Ambiente, factura.ClaveAcceso, signedBytes, cancellationToken);
        ResultadoSri? authorization = null;
        if (reception.Estado == "RECIBIDA")
            authorization = await autorizacion.ConsultarAsync(
                factura.Ambiente, factura.ClaveAcceso, cancellationToken);
        return new(factura.ClaveAcceso, generated, signedBytes,
            reception, authorization);
    }

    private static byte[] Serializar(XmlDocument document)
    {
        using var memory = new MemoryStream();
        using (var writer = XmlWriter.Create(memory, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false,
            OmitXmlDeclaration = false,
            NewLineHandling = NewLineHandling.None
        }))
            document.Save(writer);
        return memory.ToArray();
    }
}
