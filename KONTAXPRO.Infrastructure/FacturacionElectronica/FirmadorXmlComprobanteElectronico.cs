using System.Text;
using System.Xml;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class FirmadorXmlComprobanteElectronico(
    IDbContextFactory<KontaxDbContext> dbContextFactory,
    IAlmacenamientoCertificadoSri certificateStorage,
    IValidadorCertificadoSri certificateValidator,
    IFirmadorXadesSri xades) : IFirmadorXmlComprobanteElectronico
{
    public bool TieneConfiguracionSegura => OperatingSystem.IsWindows();

    public async Task<Stream> FirmarAsync(
        Stream xml,
        long empresaId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var configuration = await context.FacturacionesElectronicas.AsNoTracking()
            .Where(x => x.EmpresaId == empresaId && x.Habilitada &&
                        x.CertificadoReferencia != null)
            .Select(x => new
            {
                Referencia = x.CertificadoReferencia!,
                Ruc = x.Empresa!.NumeroIdentificacion
            })
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "La empresa no tiene un certificado electrónico habilitado.");
        var material = await certificateStorage.LeerAsync(
            empresaId, configuration.Referencia, cancellationToken);
        try
        {
            var certificateValidation = await certificateValidator.ValidarAsync(
                material.Pkcs12, material.Password, configuration.Ruc, false,
                cancellationToken);
            if (!certificateValidation.EsValido)
                throw new InvalidOperationException(
                    "El certificado electrónico no puede utilizarse: " +
                    string.Join(" | ", certificateValidation.Errores));

            var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            using (var reader = XmlReader.Create(xml, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersFromEntities = 0,
                MaxCharactersInDocument = 10_000_000
            }))
                document.Load(reader);
            var signed = xades.Firmar(document, material.Pkcs12, material.Password);
            var output = new MemoryStream();
            using (var writer = XmlWriter.Create(output, new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = false,
                OmitXmlDeclaration = false,
                NewLineHandling = NewLineHandling.None,
                CloseOutput = false
            }))
                signed.Save(writer);
            output.Position = 0;
            return output;
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(
                material.Pkcs12);
        }
    }
}
