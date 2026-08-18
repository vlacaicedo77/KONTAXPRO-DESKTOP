using System.Globalization;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Application.FacturacionElectronica;

public sealed class GeneradorClaveAccesoSri : IGeneradorClaveAccesoSri
{
    private static readonly HashSet<string> TiposComprobantePermitidos =
        ["01", "03", "04", "05", "06", "07"];

    public string Generar(SolicitudClaveAccesoSri solicitud)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ValidarSolicitud(solicitud);

        var baseClave = string.Concat(
            solicitud.FechaEmision.ToString("ddMMyyyy", CultureInfo.InvariantCulture),
            solicitud.CodigoTipoComprobante,
            solicitud.Ruc,
            solicitud.Ambiente.ToString(CultureInfo.InvariantCulture),
            solicitud.Establecimiento,
            solicitud.PuntoEmision,
            solicitud.Secuencial.ToString("D9", CultureInfo.InvariantCulture),
            solicitud.CodigoNumerico,
            solicitud.TipoEmision.ToString(CultureInfo.InvariantCulture));

        if (baseClave.Length != 48)
            throw new InvalidOperationException(
                "La base de la clave de acceso SRI debe contener 48 dígitos.");

        return baseClave + CalcularDigitoVerificador(baseClave)
            .ToString(CultureInfo.InvariantCulture);
    }

    public bool EsValida(string claveAcceso)
    {
        if (string.IsNullOrWhiteSpace(claveAcceso) ||
            claveAcceso.Length != 49 ||
            !EsNumerico(claveAcceso))
            return false;

        return claveAcceso[48] - '0' ==
               CalcularDigitoVerificador(claveAcceso[..48]);
    }

    public static int CalcularDigitoVerificador(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || !EsNumerico(valor))
            throw new ArgumentException(
                "El valor para Módulo 11 debe contener únicamente dígitos.",
                nameof(valor));

        var suma = 0;
        var factor = 2;
        for (var index = valor.Length - 1; index >= 0; index--)
        {
            suma += (valor[index] - '0') * factor;
            factor = factor == 7 ? 2 : factor + 1;
        }

        var resultado = 11 - suma % 11;
        return resultado switch
        {
            11 => 0,
            10 => 1,
            _ => resultado
        };
    }

    private static void ValidarSolicitud(SolicitudClaveAccesoSri solicitud)
    {
        if (!TiposComprobantePermitidos.Contains(solicitud.CodigoTipoComprobante))
            throw new ArgumentException(
                "El código del tipo de comprobante SRI no es válido.",
                nameof(solicitud));
        ExigirDigitos(solicitud.Ruc, 13, "RUC");
        ExigirDigitos(solicitud.Establecimiento, 3, "establecimiento");
        ExigirDigitos(solicitud.PuntoEmision, 3, "punto de emisión");
        ExigirDigitos(solicitud.CodigoNumerico, 8, "código numérico");

        if (solicitud.Ambiente is not (1 or 2))
            throw new ArgumentException(
                "El ambiente SRI debe ser 1 (pruebas) o 2 (producción).",
                nameof(solicitud));
        if (solicitud.TipoEmision != 1)
            throw new ArgumentException(
                "El esquema Offline admite únicamente emisión normal (1).",
                nameof(solicitud));
        if (solicitud.Secuencial is < 1 or > 999_999_999)
            throw new ArgumentOutOfRangeException(
                nameof(solicitud),
                "El secuencial debe estar entre 1 y 999999999.");
    }

    private static void ExigirDigitos(string valor, int longitud, string campo)
    {
        if (valor.Length != longitud || !EsNumerico(valor))
            throw new ArgumentException(
                $"El {campo} debe contener exactamente {longitud} dígitos.");
    }

    private static bool EsNumerico(string valor) =>
        valor.All(character => character is >= '0' and <= '9');
}
