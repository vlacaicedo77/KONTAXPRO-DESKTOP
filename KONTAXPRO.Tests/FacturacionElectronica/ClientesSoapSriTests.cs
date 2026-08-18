using System.Net;
using System.Text;
using KONTAXPRO.Infrastructure.FacturacionElectronica;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace KONTAXPRO.Tests.FacturacionElectronica;

public sealed class ClientesSoapSriTests
{
    private const string Clave = "1608202601179001234500110010010000000011234567811";

    [Fact]
    public async Task Recepcion_interpreta_recibida_y_envia_xml_base64()
    {
        string? request = null;
        var client = Cliente(handler: async message =>
        {
            request = await message.Content!.ReadAsStringAsync();
            return Soap(HttpStatusCode.OK, Recepcion("RECIBIDA"));
        });
        var service = new ClienteRecepcionSri(client, Opciones(),
            NullLogger<ClienteRecepcionSri>.Instance);

        var result = await service.EnviarAsync(1, Clave,
            Encoding.UTF8.GetBytes("<factura/>"));

        Assert.True(result.Exitoso);
        Assert.Equal("RECIBIDA", result.Estado);
        Assert.Contains(Convert.ToBase64String(Encoding.UTF8.GetBytes("<factura/>")), request);
    }

    [Fact]
    public async Task Recepcion_preserva_todos_los_mensajes_de_devuelta()
    {
        var service = new ClienteRecepcionSri(
            Cliente(_ => Task.FromResult(Soap(HttpStatusCode.OK,
                Recepcion("DEVUELTA", true)))), Opciones(),
            NullLogger<ClienteRecepcionSri>.Instance);

        var result = await service.EnviarAsync(1, Clave, new byte[] { 1, 2, 3 });

        Assert.False(result.Exitoso);
        Assert.Equal("DEVUELTA", result.Estado);
        Assert.Equal(2, result.Mensajes.Count);
        Assert.Equal("43", result.Mensajes[0].Codigo);
        Assert.Equal("ERROR", result.Mensajes[0].Tipo);
    }

    [Theory]
    [InlineData("AUTORIZADO", "AUTORIZADO", true)]
    [InlineData("NO AUTORIZADO", "NO_AUTORIZADO", false)]
    [InlineData("EN PROCESO", "PENDIENTE", false)]
    [InlineData("PPR", "PENDIENTE", false)]
    public async Task Autorizacion_normaliza_estados(
        string sri, string expected, bool success)
    {
        var service = new ClienteAutorizacionSri(
            Cliente(_ => Task.FromResult(Soap(HttpStatusCode.OK,
                Autorizacion(sri)))), Opciones(),
            NullLogger<ClienteAutorizacionSri>.Instance);

        var result = await service.ConsultarAsync(1, Clave);

        Assert.Equal(expected, result.Estado);
        Assert.Equal(success, result.Exitoso);
        if (success) Assert.Equal("1608202601", result.NumeroAutorizacion);
    }

    [Fact]
    public async Task Soap_fault_http_500_y_respuesta_vacia_son_errores_tecnicos()
    {
        var fault = new ClienteRecepcionSri(
            Cliente(_ => Task.FromResult(Soap(HttpStatusCode.OK,
                "<soap:Envelope xmlns:soap='http://schemas.xmlsoap.org/soap/envelope/'><soap:Body><soap:Fault><faultstring>SRI no disponible</faultstring></soap:Fault></soap:Body></soap:Envelope>"))),
            Opciones(), NullLogger<ClienteRecepcionSri>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fault.EnviarAsync(1, Clave, new byte[] { 1 }));

        var http500 = new ClienteRecepcionSri(
            Cliente(_ => Task.FromResult(Soap(HttpStatusCode.InternalServerError, "error"))),
            Opciones(), NullLogger<ClienteRecepcionSri>.Instance);
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            http500.EnviarAsync(1, Clave, new byte[] { 1 }));

        var empty = new ClienteRecepcionSri(
            Cliente(_ => Task.FromResult(Soap(HttpStatusCode.OK, ""))),
            Opciones(), NullLogger<ClienteRecepcionSri>.Instance);
        await Assert.ThrowsAnyAsync<Exception>(() =>
            empty.EnviarAsync(1, Clave, new byte[] { 1 }));
    }

    [Fact]
    public async Task Respeta_cancelacion_y_no_convierte_timeout_en_respuesta_sri()
    {
        var service = new ClienteRecepcionSri(
            Cliente(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return Soap(HttpStatusCode.OK, Recepcion("RECIBIDA"));
            }), Opciones(), NullLogger<ClienteRecepcionSri>.Instance);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.EnviarAsync(1, Clave, new byte[] { 1 }, cancellation.Token));
    }

    [Fact]
    public async Task Diagnostico_consulta_wsdl_sin_emitir_comprobantes()
    {
        var requests = new List<HttpRequestMessage>();
        var service = new DiagnosticoComunicacionSri(
            Cliente(message =>
            {
                requests.Add(message);
                return Task.FromResult(Soap(HttpStatusCode.OK,
                    "<definitions xmlns='http://schemas.xmlsoap.org/wsdl/'/>"));
            }), Opciones(), NullLogger<DiagnosticoComunicacionSri>.Instance);

        var result = await service.ProbarAsync(1);

        Assert.True(result.Disponible);
        Assert.Equal(2, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("?wsdl", request.RequestUri!.Query);
        });
    }

    private static HttpClient Cliente(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) =>
        new(new Handler((message, _) => handler(message)));
    private static HttpClient Cliente(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) =>
        new(new Handler(handler));
    private static IOptions<SriEndpointsOptions> Opciones() => Options.Create(new SriEndpointsOptions());
    private static HttpResponseMessage Soap(HttpStatusCode status, string content) => new(status)
    {
        Content = new StringContent(content, Encoding.UTF8, "text/xml")
    };

    private static string Recepcion(string state, bool messages = false) => $"""
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
          <soap:Body><validarComprobanteResponse><RespuestaRecepcionComprobante>
            <estado>{state}</estado>
            {(messages ? "<comprobantes><comprobante><mensajes><mensaje><identificador>43</identificador><mensaje>CLAVE REGISTRADA</mensaje><informacionAdicional>Detalle uno</informacionAdicional><tipo>ERROR</tipo></mensaje><mensaje><identificador>45</identificador><mensaje>SECUENCIAL</mensaje><informacionAdicional>Detalle dos</informacionAdicional><tipo>ERROR</tipo></mensaje></mensajes></comprobante></comprobantes>" : "")}
          </RespuestaRecepcionComprobante></validarComprobanteResponse></soap:Body>
        </soap:Envelope>
        """;

    private static string Autorizacion(string state) => $"""
        <soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body>
         <autorizacionComprobanteResponse><RespuestaAutorizacionComprobante><autorizaciones><autorizacion>
          <estado>{state}</estado><numeroAutorizacion>1608202601</numeroAutorizacion>
          <fechaAutorizacion>2026-08-16T12:30:00-05:00</fechaAutorizacion>
          <mensajes><mensaje><identificador>60</identificador><mensaje>INFO</mensaje><tipo>INFORMACION</tipo></mensaje></mensajes>
         </autorizacion></autorizaciones></RespuestaAutorizacionComprobante></autorizacionComprobanteResponse>
        </soap:Body></soap:Envelope>
        """;

    private sealed class Handler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> callback)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            callback(request, cancellationToken);
    }
}
