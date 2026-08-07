namespace KONTAXPRO.Application.Clientes;

public static class ClaveIdentidadTercero
{
    public const string UniqueConstraintName =
        "ux_terceros_clave_identidad";

    public static string Crear(string tipoCodigo, string? numero)
    {
        var type = (tipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
        var normalized = IdentificacionEcuadorValidator.Normalize(numero, type);
        if (string.IsNullOrWhiteSpace(normalized))
            throw new ArgumentException(
                "La identificación no puede estar vacía.",
                nameof(numero));

        var naturalPersonCedula =
            IdentificacionEcuadorValidator.GetNaturalPersonCedula(
                type,
                normalized);
        if (naturalPersonCedula is not null)
            return $"NAT:{naturalPersonCedula}";

        return type switch
        {
            IdentificacionEcuadorValidator.CedulaCodigo => $"CED:{normalized}",
            IdentificacionEcuadorValidator.RucCodigo => $"RUC:{normalized}",
            "PASAPORTE" => $"PAS:{normalized}",
            "EXTERIOR" => $"EXT:{normalized}",
            "CONSUMIDOR_FINAL" => $"CF:{normalized}",
            _ => throw new ArgumentException(
                "El tipo de identificación no admite una clave canónica.",
                nameof(tipoCodigo))
        };
    }
}
