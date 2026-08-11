using KONTAXPRO.Application.Models.Tesoreria;
using KONTAXPRO.Application.Tesoreria;

namespace KONTAXPRO.Tests.Tesoreria;

public sealed class OperacionSinSustentoRulesTests
{
    [Fact]
    public void Validate_AcceptsExpensePaidFromCash()
    {
        var request = Base("GASTO");
        request.CajaSesionId = 9;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            CuentaContableId = 20,
            Descripcion = "Almuerzos del personal",
            CostoTotal = 18.50m
        });

        Assert.Null(OperacionSinSustentoRules.Validate(request));
    }

    [Fact]
    public void Validate_RejectsPricesForExistingProduct()
    {
        var request = Base("INVENTARIO");
        request.CajaSesionId = 9;
        request.BodegaId = 3;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            ProductoId = 11,
            ProductoPresentacionId = 12,
            Descripcion = "Producto existente",
            Cantidad = 1,
            CostoTotal = 5,
            Precios = [new OperacionSinSustentoPrecioRequest
            {
                ProductoPresentacionId = 12,
                ListaPrecioId = 4,
                MetodoCalculo = "PRECIO_FIJO",
                Precio = 8
            }]
        });

        Assert.Contains("creado dentro", OperacionSinSustentoRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_AcceptsRepeatedProductLinesForBatchCosting()
    {
        var request = Base("INVENTARIO");
        request.CajaSesionId = 9;
        request.BodegaId = 3;
        request.Detalles.AddRange(
            new OperacionSinSustentoDetalleRequest
            {
                ProductoId = 11, ProductoPresentacionId = 12,
                Descripcion = "Primera presentación", Cantidad = 2, CostoTotal = 10
            },
            new OperacionSinSustentoDetalleRequest
            {
                ProductoId = 11, ProductoPresentacionId = 13,
                Descripcion = "Segunda presentación", Cantidad = 1, CostoTotal = 8
            });

        Assert.Null(OperacionSinSustentoRules.Validate(request));
    }

    [Fact]
    public void Validate_RejectsOversizedEvidence()
    {
        var request = Base("GASTO");
        request.CajaSesionId = 9;
        request.EvidenciaNombre = "soporte.pdf";
        request.EvidenciaContenido = new byte[10 * 1024 * 1024 + 1];
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            CuentaContableId = 20, Descripcion = "Gasto", CostoTotal = 5
        });

        Assert.Contains("10 MB", OperacionSinSustentoRules.Validate(request));
    }

    [Fact]
    public void Validate_AcceptsInventoryPaidFromBank()
    {
        var request = Base("INVENTARIO");
        request.MedioSalida = "BANCO";
        request.CuentaBancariaId = 7;
        request.BodegaId = 3;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            ProductoId = 11,
            ProductoPresentacionId = 12,
            Descripcion = "Guantes veterinarios",
            Cantidad = 2,
            CostoTotal = 24
        });

        Assert.Null(OperacionSinSustentoRules.Validate(request));
    }

    [Fact]
    public void Validate_RejectsMixedCashAndBankSource()
    {
        var request = Base("GASTO");
        request.CajaSesionId = 9;
        request.CuentaBancariaId = 7;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            CuentaContableId = 20,
            Descripcion = "Limpieza",
            CostoTotal = 8
        });

        Assert.Contains("fondo", OperacionSinSustentoRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsInventoryWithoutWarehouse()
    {
        var request = Base("INVENTARIO");
        request.CajaSesionId = 9;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            ProductoId = 11,
            ProductoPresentacionId = 12,
            Descripcion = "Producto",
            Cantidad = 1,
            CostoTotal = 5
        });

        Assert.Contains("bodega", OperacionSinSustentoRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_AcceptsPendingPricesForInventory()
    {
        var request = Base("INVENTARIO");
        request.CajaSesionId = 9;
        request.BodegaId = 3;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            ProductoCreadoContextualmente = true,
            ProductoId = 11,
            ProductoPresentacionId = 12,
            Descripcion = "Producto nuevo",
            Cantidad = 2,
            CostoTotal = 15,
            Precios =
            [
                new OperacionSinSustentoPrecioRequest
                {
                    ProductoPresentacionId = 12,
                    ListaPrecioId = 4,
                    MetodoCalculo = "PORCENTAJE_COSTO",
                    Porcentaje = 30,
                    Estado = 1
                }
            ]
        });

        Assert.Null(OperacionSinSustentoRules.Validate(request));
    }

    [Fact]
    public void Validate_RejectsInvalidPendingFixedPrice()
    {
        var request = Base("INVENTARIO");
        request.CajaSesionId = 9;
        request.BodegaId = 3;
        request.Detalles.Add(new OperacionSinSustentoDetalleRequest
        {
            ProductoCreadoContextualmente = true,
            ProductoId = 11,
            ProductoPresentacionId = 12,
            Descripcion = "Producto nuevo",
            Cantidad = 2,
            CostoTotal = 15,
            Precios =
            [
                new OperacionSinSustentoPrecioRequest
                {
                    ProductoPresentacionId = 12,
                    ListaPrecioId = 4,
                    MetodoCalculo = "PRECIO_FIJO",
                    Precio = 0,
                    Estado = 1
                }
            ]
        });

        Assert.Contains("precios", OperacionSinSustentoRules.Validate(request),
            StringComparison.OrdinalIgnoreCase);
    }

    private static OperacionSinSustentoRequest Base(string type) => new()
    {
        EmpresaId = 1,
        EstablecimientoId = 2,
        UsuarioId = 3,
        TipoOperacion = type,
        MedioSalida = "CAJA",
        Beneficiario = "Proveedor informal",
        Motivo = "Egreso real sin comprobante"
    };
}
