using System.Net;
using System.Text;
using KONTAXPRO.Application.Models.Compras;
using KONTAXPRO.Infrastructure.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class ConsultaAutorizacionComprobanteSriTests
{
    private const string AccessKey =
        "0608202601179999999900110010020000001231234567814";

    [Fact]
    public async Task AuthorizedResponseIsMappedAndUsesProductionEndpoint()
    {
        var requestedUris = new List<Uri>();
        const string officialXml =
            "<factura id=\"comprobante\"><infoTributaria><ruc>1799999999001</ruc></infoTributaria></factura>";
        var client = CreateClient((request, _) =>
        {
            requestedUris.Add(request.RequestUri!);
            if (request.RequestUri!.Host == "autorizacion-produccion.test")
                return Response($$"""
                    <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                      <soap:Body><RespuestaAutorizacionComprobante>
                        <autorizaciones><autorizacion>
                          <estado>AUTORIZADO</estado>
                          <numeroAutorizacion>{{AccessKey}}</numeroAutorizacion>
                          <fechaAutorizacion>2026-08-06T14:30:00-05:00</fechaAutorizacion>
                          <comprobante><![CDATA[{{officialXml}}]]></comprobante>
                        </autorizacion></autorizaciones>
                      </RespuestaAutorizacionComprobante></soap:Body>
                    </soap:Envelope>
                    """);
            return Response($$"""
                <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
                  <soap:Body>
                    <ns2:consultarEstadoAutorizacionComprobanteResponse xmlns:ns2="http://ec.gob.sri.ws.consultas">
                      <EstadoAutorizacionComprobante>
                        <claveAcceso>{{AccessKey}}</claveAcceso>
                        <estadoAutorizacion>AUTORIZADO</estadoAutorizacion>
                        <rucEmisor>1799999999001</rucEmisor>
                        <fechaAutorizacion>2026-08-06T14:30:00-05:00</fechaAutorizacion>
                      </EstadoAutorizacionComprobante>
                    </ns2:consultarEstadoAutorizacionComprobanteResponse>
                  </soap:Body>
                </soap:Envelope>
                """);
        });
        var service = CreateService(client);

        var result = await service.ConsultarAsync(AccessKey, "2");

        Assert.Equal(EstadoConsultaAutorizacionSri.Autorizado, result.Estado);
        Assert.Equal("1799999999001", result.RucEmisor);
        Assert.Equal(new DateTime(2026, 8, 6, 19, 30, 0,
            DateTimeKind.Utc), result.FechaAutorizacion);
        Assert.NotNull(result.ComprobanteSha256);
        Assert.Equal(64, result.ComprobanteSha256!.Length);
        Assert.Collection(requestedUris,
            uri => Assert.Equal(
                "https://produccion.test/ConsultaComprobante", uri.ToString()),
            uri => Assert.Equal(
                "https://autorizacion-produccion.test/AutorizacionComprobantesOffline",
                uri.ToString()));
    }

    [Fact]
    public async Task MissingDocumentIsNotTreatedAsTemporaryOutage()
    {
        var client = CreateClient((_, _) => Response($$"""
            <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
              <soap:Body>
                <EstadoAutorizacionComprobante>
                  <estadoConsulta>RECHAZADA</estadoConsulta>
                  <claveAcceso>{{AccessKey}}</claveAcceso>
                  <mensajes><mensaje>
                    <mensaje>ERROR AL CONSULTAR DATOS DEL SERVICIO WEB</mensaje>
                    <informacionAdicional>No existen datos para los parámetros ingresados</informacionAdicional>
                  </mensaje></mensajes>
                </EstadoAutorizacionComprobante>
              </soap:Body>
            </soap:Envelope>
            """));
        var result = await CreateService(client)
            .ConsultarAsync(AccessKey, "1");

        Assert.Equal(EstadoConsultaAutorizacionSri.NoEncontrado, result.Estado);
    }

    [Fact]
    public async Task ServiceOutageReturnsOfflineState()
    {
        var client = CreateClient((_, _) => new HttpResponseMessage(
            HttpStatusCode.ServiceUnavailable));
        var result = await CreateService(client)
            .ConsultarAsync(AccessKey, "2");

        Assert.Equal(EstadoConsultaAutorizacionSri.NoDisponible, result.Estado);
    }

    private static ConsultaAutorizacionComprobanteSri CreateService(
        HttpClient client) => new(client, new ConsultaAutorizacionSriOptions
        {
            UrlPruebas = "https://pruebas.test/ConsultaComprobante",
            UrlProduccion = "https://produccion.test/ConsultaComprobante",
            UrlAutorizacionPruebas =
                "https://autorizacion-pruebas.test/AutorizacionComprobantesOffline",
            UrlAutorizacionProduccion =
                "https://autorizacion-produccion.test/AutorizacionComprobantesOffline"
        });

    private static HttpClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send) =>
        new(new DelegateHandler(send));

    private static HttpResponseMessage Response(string xml) => new(
        HttpStatusCode.OK)
    {
        Content = new StringContent(xml, Encoding.UTF8, "text/xml")
    };

    private sealed class DelegateHandler(
        Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(send(request, cancellationToken));
    }
}
