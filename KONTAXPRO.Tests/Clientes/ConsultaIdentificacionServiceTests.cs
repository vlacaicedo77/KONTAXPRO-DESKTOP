using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Interoperabilidad;
using KONTAXPRO.Application.Session;
using KONTAXPRO.Infrastructure.Interoperabilidad;

namespace KONTAXPRO.Tests.Clientes;

public sealed class ConsultaIdentificacionServiceTests
{
    [Fact]
    public async Task ConsultarAsync_DoesNotCallSifaeWhenGuiaSucceeds()
    {
        var guia = new FakeProvider("GUIA", Found("GUIA"));
        var sifae = new FakeProvider("SIFAE", Found("SIFAE"));
        var service = CreateService([guia, sifae]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"));

        Assert.True(result.Encontrado);
        Assert.Equal("GUIA", result.Fuente);
        Assert.Equal(1, guia.Calls);
        Assert.Equal(0, sifae.Calls);
    }

    [Theory]
    [InlineData(EstadoConsultaIdentificacion.FuentesNoDisponibles)]
    [InlineData(EstadoConsultaIdentificacion.NoEncontrado)]
    [InlineData(EstadoConsultaIdentificacion.DatosIncompletos)]
    public async Task ConsultarAsync_UsesSifaeForEveryNonSuccessfulGuiaResult(
        EstadoConsultaIdentificacion guiaState)
    {
        var guia = new FakeProvider("GUIA", new ProveedorIdentificacionResult
        {
            Estado = guiaState,
            Fuente = "GUIA"
        });
        var sifae = new FakeProvider("SIFAE", Found("SIFAE"));
        var service = CreateService([guia, sifae]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "1790016919001"));

        Assert.True(result.Encontrado);
        Assert.Equal("SIFAE", result.Fuente);
        Assert.Equal(1, sifae.Calls);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("2300257941")]
    public async Task ConsultarAsync_InvalidIdentificationCallsNoProvider(
        string number)
    {
        var guia = new FakeProvider("GUIA", Found("GUIA"));
        var sifae = new FakeProvider("SIFAE", Found("SIFAE"));
        var service = CreateService([guia, sifae]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", number));

        Assert.Equal(
            EstadoConsultaIdentificacion.IdentificacionInvalida,
            result.Estado);
        Assert.Equal(0, guia.Calls);
        Assert.Equal(0, sifae.Calls);
    }

    [Fact]
    public async Task ConsultarAsync_BothUnavailableAllowsManualEntry()
    {
        var unavailable = new ProveedorIdentificacionResult
        {
            Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles
        };
        var service = CreateService(
        [
            new FakeProvider("GUIA", unavailable),
            new FakeProvider("SIFAE", unavailable)
        ]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"));

        Assert.Equal(
            EstadoConsultaIdentificacion.FuentesNoDisponibles,
            result.Estado);
        Assert.Contains("manualmente", result.MensajeUsuario);
    }

    [Fact]
    public async Task OfflineProofIsIssuedOnlyAfterThirdUnavailableRound()
    {
        var unavailable = new ProveedorIdentificacionResult
        {
            Estado = EstadoConsultaIdentificacion.FuentesNoDisponibles
        };
        var store = new ConstanciaVerificacionIdentificacionStore(
            TimeProvider.System);
        var service = new ConsultaIdentificacionService(
        [
            new FakeProvider("GUIA", unavailable),
            new FakeProvider("SIFAE", unavailable)
        ], store, new CurrentSession { UsuarioId = 7 });
        var flowId = Guid.NewGuid();
        var request = new ConsultaIdentificacionRequest(
            "CEDULA",
            "1710034065",
            PropositoConsultaIdentificacion.Cliente,
            flowId);

        var first = await service.ConsultarAsync(request);
        var second = await service.ConsultarAsync(request);
        var third = await service.ConsultarAsync(request);

        Assert.Null(first.ConstanciaVerificacionId);
        Assert.Null(second.ConstanciaVerificacionId);
        Assert.NotNull(third.ConstanciaVerificacionId);
        Assert.True(store.TryTake(
            third.ConstanciaVerificacionId!.Value,
            7,
            "CEDULA",
            "1710034065",
            PropositoConsultaIdentificacion.Cliente,
            out var proof));
        Assert.Equal(TipoConstanciaVerificacion.OfflineAutorizada, proof!.Tipo);
        Assert.False(store.TryTake(
            third.ConstanciaVerificacionId.Value,
            7,
            "CEDULA",
            "1710034065",
            PropositoConsultaIdentificacion.Cliente,
            out _));
    }

    [Fact]
    public async Task OfficialProofIsBoundToUserDocumentAndPurpose()
    {
        var store = new ConstanciaVerificacionIdentificacionStore(
            TimeProvider.System);
        var service = new ConsultaIdentificacionService(
        [
            new FakeProvider("GUIA", Found("GUIA")),
            new FakeProvider("SIFAE", Found("SIFAE"))
        ], store, new CurrentSession { UsuarioId = 9 });

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest(
                "RUC",
                "1790016919001",
                PropositoConsultaIdentificacion.Proveedor,
                Guid.NewGuid()));

        Assert.NotNull(result.ConstanciaVerificacionId);
        Assert.False(store.TryTake(
            result.ConstanciaVerificacionId!.Value,
            10,
            "RUC",
            "1790016919001",
            PropositoConsultaIdentificacion.Proveedor,
            out _));
        Assert.False(store.TryTake(
            result.ConstanciaVerificacionId.Value,
            9,
            "RUC",
            "1790016919001",
            PropositoConsultaIdentificacion.Cliente,
            out _));
        Assert.True(store.TryTake(
            result.ConstanciaVerificacionId.Value,
            9,
            "RUC",
            "1790016919001",
            PropositoConsultaIdentificacion.Proveedor,
            out var proof));
        Assert.Equal("PERSONA DE PRUEBA", proof!.RazonSocial);
        Assert.Equal("GUIA", proof.Fuente);
    }

    [Fact]
    public async Task ConsultarAsync_BothNotFoundReturnsNotFound()
    {
        var notFound = new ProveedorIdentificacionResult
        {
            Estado = EstadoConsultaIdentificacion.NoEncontrado
        };
        var service = CreateService(
        [
            new FakeProvider("GUIA", notFound),
            new FakeProvider("SIFAE", notFound)
        ]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"));

        Assert.Equal(EstadoConsultaIdentificacion.NoEncontrado, result.Estado);
    }

    [Theory]
    [InlineData(
        EstadoConsultaIdentificacion.NoEncontrado,
        EstadoConsultaIdentificacion.FuentesNoDisponibles,
        EstadoConsultaIdentificacion.NoEncontrado)]
    [InlineData(
        EstadoConsultaIdentificacion.FuentesNoDisponibles,
        EstadoConsultaIdentificacion.NoEncontrado,
        EstadoConsultaIdentificacion.NoEncontrado)]
    [InlineData(
        EstadoConsultaIdentificacion.IdentificacionInvalida,
        EstadoConsultaIdentificacion.FuentesNoDisponibles,
        EstadoConsultaIdentificacion.IdentificacionInvalida)]
    public async Task ConsultarAsync_PreservesDefinitiveOfficialResult(
        EstadoConsultaIdentificacion guiaState,
        EstadoConsultaIdentificacion sifaeState,
        EstadoConsultaIdentificacion expected)
    {
        var service = CreateService(
        [
            new FakeProvider("GUIA", new ProveedorIdentificacionResult
            {
                Estado = guiaState
            }),
            new FakeProvider("SIFAE", new ProveedorIdentificacionResult
            {
                Estado = sifaeState
            })
        ]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("RUC", "2300257941001"));

        Assert.Equal(expected, result.Estado);
        Assert.DoesNotContain("manualmente", result.MensajeUsuario);
    }

    [Theory]
    [InlineData("HTTP")]
    [InlineData("IO")]
    [InlineData("CANCELACION_INTERNA")]
    public async Task ConsultarAsync_GuiaTechnicalFailureFallsBackToSifae(
        string failure)
    {
        var guia = new ThrowingProvider("GUIA", failure);
        var sifae = new FakeProvider("SIFAE", Found("SIFAE"));
        var service = CreateService([guia, sifae]);

        var result = await service.ConsultarAsync(
            new ConsultaIdentificacionRequest("CEDULA", "1710034065"));

        Assert.True(result.Encontrado);
        Assert.Equal("SIFAE", result.Fuente);
        Assert.Equal(1, guia.Calls);
        Assert.Equal(1, sifae.Calls);
    }

    [Fact]
    public async Task ConsultarAsync_UserCancellationDoesNotCallFallback()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var guia = new ThrowingProvider("GUIA", "CANCELACION_USUARIO");
        var sifae = new FakeProvider("SIFAE", Found("SIFAE"));
        var service = CreateService([guia, sifae]);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.ConsultarAsync(
                new ConsultaIdentificacionRequest("CEDULA", "1710034065"),
                cancellation.Token));

        Assert.Equal(0, sifae.Calls);
    }

    private static ProveedorIdentificacionResult Found(string source) => new()
    {
        Estado = EstadoConsultaIdentificacion.Encontrado,
        Fuente = source,
        RazonSocial = "PERSONA DE PRUEBA"
    };

    private static ConsultaIdentificacionService CreateService(
        IEnumerable<IProveedorConsultaIdentificacion> providers) => new(
            providers,
            new ConstanciaVerificacionIdentificacionStore(TimeProvider.System),
            new CurrentSession { UsuarioId = 1 });

    private sealed class FakeProvider(
        string source,
        ProveedorIdentificacionResult result) : IProveedorConsultaIdentificacion
    {
        public string Fuente => source;
        public int Calls { get; private set; }

        public Task<ProveedorIdentificacionResult> ConsultarAsync(
            ConsultaIdentificacionRequest request,
            string numeroNormalizado,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(result.WithSource(source));
        }
    }

    private sealed class ThrowingProvider(string source, string failure)
        : IProveedorConsultaIdentificacion
    {
        public string Fuente => source;
        public int Calls { get; private set; }

        public Task<ProveedorIdentificacionResult> ConsultarAsync(
            ConsultaIdentificacionRequest request,
            string numeroNormalizado,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return failure switch
            {
                "HTTP" => Task.FromException<ProveedorIdentificacionResult>(
                    new HttpRequestException("fallo simulado")),
                "IO" => Task.FromException<ProveedorIdentificacionResult>(
                    new IOException("fallo simulado")),
                "CANCELACION_INTERNA" =>
                    Task.FromException<ProveedorIdentificacionResult>(
                        new OperationCanceledException("timeout simulado")),
                _ => Task.FromCanceled<ProveedorIdentificacionResult>(
                    cancellationToken)
            };
        }
    }
}

file static class ProviderResultTestExtensions
{
    public static ProveedorIdentificacionResult WithSource(
        this ProveedorIdentificacionResult result,
        string source) => new()
    {
        Estado = result.Estado,
        Fuente = source,
        RazonSocial = result.RazonSocial,
        NombreComercial = result.NombreComercial,
        Correo = result.Correo,
        Direccion = result.Direccion,
        DetalleTecnicoSeguro = result.DetalleTecnicoSeguro,
        SeccionesFaltantes = result.SeccionesFaltantes
    };
}
