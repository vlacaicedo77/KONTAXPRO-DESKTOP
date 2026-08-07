using System.Net;
using System.Text;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Infrastructure.Interoperabilidad;

namespace KONTAXPRO.Tests.Clientes;

public sealed class GuiaInteroperabilidadTests
{
    [Fact]
    public async Task TokenClient_ReusesTokenWhileItIsValid()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"access_token":"token-prueba","expires_in":3600}""")));
        var factory = CreateFactory(tokenHandler, new SequenceHttpMessageHandler(
            (_, _, _) => Task.FromResult(HttpResponses.Json("{}"))));
        var client = new GuiaTokenClient(
            factory,
            CreateOptions(),
            TimeProvider.System);

        var first = await client.GetAccessTokenAsync();
        var second = await client.GetAccessTokenAsync();

        Assert.Equal("token-prueba", first);
        Assert.Equal(first, second);
        Assert.Equal(1, tokenHandler.CallCount);
    }

    [Fact]
    public async Task TokenClient_RenewsBeforeExpiration()
    {
        var clock = new AdjustableTimeProvider();
        var tokenHandler = new SequenceHttpMessageHandler((_, call, _) =>
            Task.FromResult(HttpResponses.Json(
                $$"""{"access_token":"token-{{call}}","expires_in":60}""")));
        var factory = CreateFactory(tokenHandler, new SequenceHttpMessageHandler(
            (_, _, _) => Task.FromResult(HttpResponses.Json("{}"))));
        var client = new GuiaTokenClient(factory, CreateOptions(), clock);

        var first = await client.GetAccessTokenAsync();
        clock.Advance(TimeSpan.FromSeconds(51));
        var second = await client.GetAccessTokenAsync();

        Assert.Equal("token-1", first);
        Assert.Equal("token-2", second);
        Assert.Equal(2, tokenHandler.CallCount);
    }

    [Fact]
    public async Task TokenClient_SynchronizesConcurrentRefresh()
    {
        var tokenHandler = new SequenceHttpMessageHandler(async (_, _, token) =>
        {
            await Task.Delay(30, token);
            return HttpResponses.Json(
                """{"access_token":"shared-token","expires_in":3600}""");
        });
        var factory = CreateFactory(tokenHandler, new SequenceHttpMessageHandler(
            (_, _, _) => Task.FromResult(HttpResponses.Json("{}"))));
        var client = new GuiaTokenClient(
            factory,
            CreateOptions(),
            TimeProvider.System);

        var tokens = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => client.GetAccessTokenAsync()));

        Assert.All(tokens, token => Assert.Equal("shared-token", token));
        Assert.Equal(1, tokenHandler.CallCount);
    }

    [Fact]
    public async Task Provider_RenewsTokenAndRetriesOnceAfterUnauthorized()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, call, _) =>
            Task.FromResult(HttpResponses.Json(
                $$"""{"access_token":"token-{{call}}","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((request, call, _) =>
        {
            Assert.NotNull(request.Headers.Authorization);
            return Task.FromResult(call == 1
                ? new HttpResponseMessage(HttpStatusCode.Unauthorized)
                : HttpResponses.Json(
                    """{"estado":"OK","valores":{"Nombre":"PERSONA DE PRUEBA","NUI":"********4065"}}"""));
        });
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var tokenClient = new GuiaTokenClient(
            factory,
            options,
            TimeProvider.System);
        var provider = new GuiaIdentificacionProvider(
            factory,
            tokenClient,
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("PERSONA DE PRUEBA", result.RazonSocial);
        Assert.Equal(2, tokenHandler.CallCount);
        Assert.Equal(2, serviceHandler.CallCount);
    }

    [Fact]
    public async Task Provider_ReadsRucAndOptionalEmail()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"access_token":"token","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"estado":"OK","valores":{"razonSocial":"EMPRESA DE PRUEBA","email":"prueba@example.test"}}""")));
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "1790016919001"),
            "1790016919001");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("EMPRESA DE PRUEBA", result.RazonSocial);
        Assert.Equal("prueba@example.test", result.Correo);
    }

    [Fact]
    public async Task Provider_InvalidJsonReturnsSafeUnavailableResult()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"access_token":"token","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json("not-json")));
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(
            EstadoConsultaIdentificacion.FuentesNoDisponibles,
            result.Estado);
        Assert.DoesNotContain("not-json", result.DetalleTecnicoSeguro);
    }

    [Fact]
    public async Task Provider_FunctionalNotFoundReturnsNotFound()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"access_token":"token","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"valores":{"CodigoError":"404","Error":"Registro no encontrado"}}""")));
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(EstadoConsultaIdentificacion.NoEncontrado, result.Estado);
    }

    [Fact]
    public async Task Provider_Code009ReturnsInvalidIdentification()
    {
        var result = await QueryFunctionalErrorAsync(
            """{"valores":{"CodigoError":"009","Error":"N.U.I. INCORRECTO."},"estado":"error","mensaje":"N.U.I. INCORRECTO."}""",
            "CEDULA",
            "2300257941");

        Assert.Equal(
            EstadoConsultaIdentificacion.IdentificacionInvalida,
            result.Estado);
    }

    [Fact]
    public async Task Provider_Code001ReturnsNotFound()
    {
        var result = await QueryFunctionalErrorAsync(
            """{"valores":{"CodigoError":"001","Error":"RUC NO ENCONTRADO"},"estado":"error","mensaje":"RUC NO ENCONTRADO"}""",
            "RUC",
            "2300257941001");

        Assert.Equal(EstadoConsultaIdentificacion.NoEncontrado, result.Estado);
    }

    [Fact]
    public async Task Provider_AcceptsCedulaJsonMislabelledByServer()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(TextJson(
                """{"access_token":"token","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler(
            async (request, _, cancellationToken) =>
            {
                var body = await request.Content!.ReadAsStringAsync(
                    cancellationToken);
                Assert.Contains("clasificacion=C%C3%A9dula", body);
                Assert.Contains("numero=1710034065", body);
                return TextJson(
                    """{"estado":"SUCCESS","valores":[{"CodigoError":"000","Error":"NO","Nombre":"PERSONA DE PRUEBA"}]}""");
            });
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("PERSONA DE PRUEBA", result.RazonSocial);
    }

    [Fact]
    public async Task Provider_AcceptsRucWithNumericSuccessStatus()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(TextJson(
                """{"accessToken":"token","expiresIn":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler(
            async (request, _, cancellationToken) =>
            {
                var body = await request.Content!.ReadAsStringAsync(
                    cancellationToken);
                Assert.Contains("clasificacion=Natural", body);
                Assert.Contains("numero=1790016919001", body);
                return TextJson(
                    """{"estado":200,"valores":{"razonSocial":"EMPRESA DE PRUEBA","email":"prueba@example.test"}}""");
            });
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "1790016919001"),
            "1790016919001");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("EMPRESA DE PRUEBA", result.RazonSocial);
    }

    [Fact]
    public async Task Provider_RucWithSeveralEmailsUsesFirstOne()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(TextJson(
                """{"accessToken":"token","expiresIn":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(TextJson(
                """{"estado":200,"valores":{"razonSocial":"POLLOS DEL SUR","email":"contabilidad@pollosdelsur.com,gerencia@pollosdelsur.com"}}""")));
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "0791844092001"),
            "0791844092001");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("contabilidad@pollosdelsur.com", result.Correo);
    }

    [Fact]
    public async Task Provider_AcceptsZeroCodeWithInformationalErrorText()
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"access_token":"token","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"estado":"exito","mensaje":"CONSULTA REALIZADA.","valores":{"Nombre":"PERSONA DE PRUEBA","CodigoError":"000","Error":"CONSULTA REALIZADA."}}""")));
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("PERSONA DE PRUEBA", result.RazonSocial);
    }

    private static HttpResponseMessage TextJson(string json) => new()
    {
        Content = new StringContent(json, Encoding.UTF8, "text/html")
    };

    private static async Task<ProveedorIdentificacionResult>
        QueryFunctionalErrorAsync(string json, string type, string number)
    {
        var tokenHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(
                """{"access_token":"token","expires_in":3600}""")));
        var serviceHandler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(HttpResponses.Json(json)));
        var options = CreateOptions();
        var factory = CreateFactory(tokenHandler, serviceHandler);
        var provider = new GuiaIdentificacionProvider(
            factory,
            new GuiaTokenClient(factory, options, TimeProvider.System),
            options);

        return await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest(type, number),
            number);
    }

    private static InteroperabilidadOptions CreateOptions() => new()
    {
        Guia = new GuiaOptions
        {
            TokenUrl = "https://guia.test/token",
            ServicioUrl = "https://guia.test/service",
            ClientId = "test-id",
            ClientSecret = "test-secret"
        },
        Sifae = new SifaeOptions { BaseUrl = "https://sifae.test/?ruta=" }
    };

    private static StubHttpClientFactory CreateFactory(
        HttpMessageHandler tokenHandler,
        HttpMessageHandler serviceHandler) => new(
        new Dictionary<string, HttpClient>
        {
            [InteroperabilidadHttpClients.GuiaToken] = new(tokenHandler),
            [InteroperabilidadHttpClients.GuiaServicio] = new(serviceHandler),
            [InteroperabilidadHttpClients.Sifae] = new(serviceHandler)
        });

    private sealed class AdjustableTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow =
            new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan value) => _utcNow += value;
    }
}
