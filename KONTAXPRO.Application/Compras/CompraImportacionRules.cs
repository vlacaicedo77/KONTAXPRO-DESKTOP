using KONTAXPRO.Application.Clientes;
using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Compras;

public static class CompraImportacionRules
{
    public const decimal ToleranciaMonetaria = 0.01m;

    public static bool ReceptorCorrespondeEmpresa(
        string identificacionEmpresa,
        string tipoIdentificacionCompradorSri,
        string identificacionComprador)
    {
        var tipoComprador = tipoIdentificacionCompradorSri.Trim() switch
        {
            "04" => "RUC",
            "05" => "CEDULA",
            _ => string.Empty
        };
        var tipoEmpresa = SoloDigitos(identificacionEmpresa).Length switch
        {
            13 => "RUC",
            10 => "CEDULA",
            _ => string.Empty
        };
        if (tipoComprador.Length == 0 || tipoEmpresa.Length == 0)
            return false;

        try
        {
            var empresa = ClaveIdentidadTercero.Crear(
                tipoEmpresa, identificacionEmpresa);
            var receptor = ClaveIdentidadTercero.Crear(
                tipoComprador, identificacionComprador);
            return string.Equals(empresa, receptor,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static CompraCuadreDto ValidarCuadre(FacturaCompraXmlDto factura)
    {
        var subtotalDetalles = factura.Detalles.Sum(x =>
            x.PrecioTotalSinImpuesto);
        var impuestosDetalles = factura.Detalles
            .SelectMany(x => x.Impuestos).Sum(x => x.Valor);
        var diferenciaSubtotal = decimal.Round(
            subtotalDetalles - factura.TotalSinImpuestos, 2,
            MidpointRounding.AwayFromZero);
        var diferenciaTotal = decimal.Round(
            factura.TotalSinImpuestos + impuestosDetalles + factura.Propina -
            factura.ImporteTotal, 2, MidpointRounding.AwayFromZero);
        var cuadra = Math.Abs(diferenciaSubtotal) <= ToleranciaMonetaria &&
                     Math.Abs(diferenciaTotal) <= ToleranciaMonetaria;
        return new CompraCuadreDto
        {
            Cuadra = cuadra,
            SubtotalDetalles = subtotalDetalles,
            ImpuestosDetalles = impuestosDetalles,
            DiferenciaSubtotal = diferenciaSubtotal,
            DiferenciaTotal = diferenciaTotal,
            Mensaje = cuadra
                ? "Los detalles y totales del XML cuadran."
                : "Los detalles del XML no coinciden con los totales de cabecera."
        };
    }

    public static decimal ObtenerAjusteContableRedondeo(
        decimal totalDebitos,
        decimal totalCompra)
    {
        var difference = decimal.Round(totalCompra - totalDebitos, 2,
            MidpointRounding.AwayFromZero);
        if (Math.Abs(difference) > ToleranciaMonetaria)
            throw new ArgumentException(
                "La diferencia contable supera la tolerancia monetaria permitida.");
        return difference;
    }

    public static string NormalizarCodigoProveedor(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : new string(value.Trim().ToUpperInvariant()
                .Where(char.IsLetterOrDigit).ToArray());

    public static bool ClaveAccesoCorrespondeFactura(
        string claveAcceso,
        DateOnly fechaEmision,
        string codigoDocumento,
        string rucEmisor,
        string ambiente,
        string establecimiento,
        string puntoEmision,
        string secuencial,
        string tipoEmision)
    {
        var clave = SoloDigitos(claveAcceso);
        var secuencia = SoloDigitos(secuencial).PadLeft(9, '0');
        if (clave.Length != 49 || secuencia.Length != 9 ||
            SoloDigitos(rucEmisor).Length != 13 ||
            SoloDigitos(establecimiento).Length != 3 ||
            SoloDigitos(puntoEmision).Length != 3 ||
            SoloDigitos(codigoDocumento).Length != 2 ||
            SoloDigitos(ambiente).Length != 1 ||
            SoloDigitos(tipoEmision).Length != 1)
            return false;

        var expectedPrefix = fechaEmision.ToString("ddMMyyyy") +
                             SoloDigitos(codigoDocumento) +
                             SoloDigitos(rucEmisor) +
                             SoloDigitos(ambiente) +
                             SoloDigitos(establecimiento) +
                             SoloDigitos(puntoEmision) +
                             secuencia;
        if (!clave.StartsWith(expectedPrefix, StringComparison.Ordinal) ||
            clave[47].ToString() != SoloDigitos(tipoEmision))
            return false;

        var sum = 0;
        var factor = 2;
        for (var index = 47; index >= 0; index--)
        {
            sum += (clave[index] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }
        var verifier = 11 - sum % 11;
        if (verifier == 11) verifier = 0;
        else if (verifier == 10) verifier = 1;
        return clave[48] - '0' == verifier;
    }

    private static string SoloDigitos(string value) =>
        new(value.Where(char.IsDigit).ToArray());
}
