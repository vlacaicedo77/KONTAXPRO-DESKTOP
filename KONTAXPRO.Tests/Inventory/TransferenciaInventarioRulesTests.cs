using KONTAXPRO.Application.Inventory;
using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Tests.Inventory;

public sealed class TransferenciaInventarioRulesTests
{
    [Fact]
    public void TransferenciaCompleta_EsValida()
    {
        var request = ValidRequest();

        Assert.Null(TransferenciaInventarioRules.Validate(request));
    }

    [Fact]
    public void MismaBodega_SeRechaza()
    {
        var request = ValidRequest();
        request.BodegaDestinoId = request.BodegaOrigenId;

        Assert.Contains("diferente",
            TransferenciaInventarioRules.Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CantidadNoPositiva_SeRechaza(decimal quantity)
    {
        var request = ValidRequest();
        request.Detalles[0].Cantidad = quantity;

        Assert.Contains("mayor que cero",
            TransferenciaInventarioRules.Validate(request));
    }

    [Fact]
    public void SinDetalles_SeRechaza()
    {
        var request = ValidRequest();
        request.Detalles.Clear();

        Assert.Contains("al menos un producto",
            TransferenciaInventarioRules.Validate(request));
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    public void MotivoInsuficiente_SeRechaza(string reason)
    {
        var request = ValidRequest();
        request.Motivo = reason;

        Assert.Contains("motivo",
            TransferenciaInventarioRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CorreccionBodegaInicialCompleta_EsValida()
    {
        Assert.Null(TransferenciaInventarioRules.Validate(
            ValidInitialWarehouseCorrection()));
    }

    [Fact]
    public void CorreccionBodegaInicialEnMismaBodega_SeRechaza()
    {
        var request = ValidInitialWarehouseCorrection();
        request.BodegaDestinoId = request.BodegaOrigenId;

        Assert.Contains("diferente",
            TransferenciaInventarioRules.Validate(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CorreccionBodegaInicialSinCantidadPositiva_SeRechaza(
        decimal quantity)
    {
        var request = ValidInitialWarehouseCorrection();
        request.CantidadBase = quantity;

        Assert.Contains("mayor que cero",
            TransferenciaInventarioRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    public void CorreccionBodegaInicialSinMotivoSuficiente_SeRechaza(
        string reason)
    {
        var request = ValidInitialWarehouseCorrection();
        request.Motivo = reason;

        Assert.Contains("corrección",
            TransferenciaInventarioRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    private static TransferenciaInventarioRequest ValidRequest() => new()
    {
        EmpresaId = 1,
        UsuarioId = 2,
        EstablecimientoOrigenId = 10,
        EstablecimientoDestinoId = 20,
        BodegaOrigenId = 100,
        BodegaDestinoId = 200,
        Motivo = "REPOSICIÓN DE SUCURSAL",
        Detalles =
        [
            new TransferenciaInventarioLineaRequest
            {
                ProductoId = 30,
                ProductoPresentacionId = 31,
                Cantidad = 2
            }
        ]
    };

    private static CorreccionBodegaInventarioInicialRequest
        ValidInitialWarehouseCorrection() => new()
        {
            EmpresaId = 1,
            UsuarioId = 2,
            ProductoId = 30,
            BodegaOrigenId = 100,
            BodegaDestinoId = 200,
            CantidadBase = 10,
            Motivo = "BODEGA EQUIVOCADA EN EL SALDO INICIAL"
        };
}
