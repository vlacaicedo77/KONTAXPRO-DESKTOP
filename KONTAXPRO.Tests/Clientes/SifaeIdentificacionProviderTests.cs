using System.Collections.Concurrent;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Infrastructure.Interoperabilidad;

namespace KONTAXPRO.Tests.Clientes;

public sealed class SifaeIdentificacionProviderTests
{
    [Fact]
    public async Task CedulaWithResultReturnsName()
    {
        var handler = HandlerForRoutes(route => route.Contains("datos_demograficos")
            ? HttpResponses.Json(
                """{"estado":"OK","resultado":[{"nombre":"PERSONA DE PRUEBA"}]}""")
            : HttpResponses.Json("{}"));
        var provider = CreateProvider(handler);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("PERSONA DE PRUEBA", result.RazonSocial);
    }

    [Fact]
    public async Task CedulaWithoutResultsReturnsNotFound()
    {
        var provider = CreateProvider(HandlerForRoutes(_ =>
            HttpResponses.Json("""{"estado":"OK","resultado":[]}""")));

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(EstadoConsultaIdentificacion.NoEncontrado, result.Estado);
    }

    [Fact]
    public async Task ClientRucDoesNotFailWhenOptionalEmailFails()
    {
        var provider = CreateProvider(HandlerForRoutes(route =>
            route.Contains("ubicaciones_sri")
                ? HttpResponses.Json(
                    """{"estado":"OK","resultado":[{"razonSocial":"EMPRESA DE PRUEBA"}]}""")
                : new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)));

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "1790016919001"),
            "1790016919001");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("EMPRESA DE PRUEBA", result.RazonSocial);
        Assert.Null(result.Correo);
    }

    [Fact]
    public async Task ClientRucCombinesBusinessNameAndEmailFromTwoEndpoints()
    {
        var requestedRoutes = new ConcurrentBag<string>();
        var provider = CreateProvider(HandlerForRoutes(route =>
        {
            requestedRoutes.Add(route);
            return route.Contains("ubicaciones_sri")
                ? HttpResponses.Json(
                    """{"estado":"OK","resultado":[{"razonSocial":"PERSONA DE PRUEBA"}]}""")
                : HttpResponses.Json(
                    """{"estado":"OK","resultado":[{"email":"persona@example.test"}]}""");
        }));

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "1790016919001"),
            "1790016919001");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("PERSONA DE PRUEBA", result.RazonSocial);
        Assert.Equal("persona@example.test", result.Correo);
        Assert.Contains(requestedRoutes, route => route.Contains("ubicaciones_sri"));
        Assert.Contains(requestedRoutes, route => route.Contains("email_contribuyente"));
        Assert.DoesNotContain(
            requestedRoutes,
            route => route.Contains("ruc_contribuyente"));
    }

    [Fact]
    public async Task ClientRucWithSeveralEmailsUsesFirstOne()
    {
        var provider = CreateProvider(HandlerForRoutes(route =>
            route.Contains("ubicaciones_sri")
                ? HttpResponses.Json(
                    """{"estado":"OK","resultado":[{"razonSocial":"POLLOS DEL SUR"}]}""")
                : HttpResponses.Json(
                    """{"estado":"OK","resultado":[{"email":"contabilidad@pollosdelsur.com,gerencia@pollosdelsur.com"}]}""")));

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "0791844092001"),
            "0791844092001");

        Assert.Equal(EstadoConsultaIdentificacion.Encontrado, result.Estado);
        Assert.Equal("contabilidad@pollosdelsur.com", result.Correo);
    }

    [Fact]
    public async Task CompleteCompanyReportsMissingRequiredSection()
    {
        var provider = CreateProvider(HandlerForRoutes(route =>
        {
            if (route.Contains("ruc_representante_legal"))
                return new HttpResponseMessage(
                    System.Net.HttpStatusCode.ServiceUnavailable);
            return HttpResponses.Json(
                """{"estado":"OK","resultado":[{"razonSocial":"EMPRESA DE PRUEBA","email":"a@example.test"}]}""");
        }));

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest(
                "RUC",
                "1790016919001",
                PropositoConsultaIdentificacion.EmpresaCompleta),
            "1790016919001");

        Assert.Equal(EstadoConsultaIdentificacion.DatosIncompletos, result.Estado);
        Assert.Contains("ruc_representante_legal", result.SeccionesFaltantes);
    }

    [Fact]
    public async Task HtmlOrInvalidJsonReturnsUnavailable()
    {
        var handler = new SequenceHttpMessageHandler((_, _, _) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("<html>error</html>")
            }));
        var provider = CreateProvider(handler);

        var result = await provider.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
            "1710034065");

        Assert.Equal(
            EstadoConsultaIdentificacion.FuentesNoDisponibles,
            result.Estado);
    }

    private static SequenceHttpMessageHandler HandlerForRoutes(
        Func<string, HttpResponseMessage> response) => new((request, _, _) =>
            Task.FromResult(response(request.RequestUri!.ToString())));

    private static SifaeIdentificacionProvider CreateProvider(
        HttpMessageHandler handler)
    {
        var factory = new StubHttpClientFactory(
            new Dictionary<string, HttpClient>
            {
                [InteroperabilidadHttpClients.Sifae] = new(handler)
            });
        return new SifaeIdentificacionProvider(
            factory,
            new InteroperabilidadOptions
            {
                Sifae = new SifaeOptions
                {
                    BaseUrl = "https://sifae.test/?ruta="
                }
            });
    }
}
