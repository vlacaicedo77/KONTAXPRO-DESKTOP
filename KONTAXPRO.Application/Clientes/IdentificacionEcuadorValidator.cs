namespace KONTAXPRO.Application.Clientes;

public sealed record IdentificacionValidationResult(
    bool IsValid,
    string Normalized,
    string? Error = null);

public static class IdentificacionEcuadorValidator
{
    public const string CedulaCodigo = "CEDULA";
    public const string RucCodigo = "RUC";

    public static IdentificacionValidationResult Validate(
        string tipoCodigo,
        string? numero)
    {
        var tipo = (tipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
        var normalizado = Normalize(numero, tipo);

        return tipo switch
        {
            "CEDULA" => ValidateCedula(normalizado),
            "RUC" => ValidateRuc(normalizado),
            "PASAPORTE" or "EXTERIOR" => ValidateExternal(normalizado),
            _ => new(false, normalizado,
                "El tipo de identificación no es válido para clientes.")
        };
    }

    public static string Normalize(string? numero, string tipoCodigo)
    {
        var value = (numero ?? string.Empty).Trim().ToUpperInvariant();
        if (tipoCodigo is "CEDULA" or "RUC")
            return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());

        return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public static string? DetectNationalType(string? numero)
    {
        var value = new string((numero ?? string.Empty)
            .Where(c => !char.IsWhiteSpace(c))
            .ToArray());
        if (!value.All(char.IsDigit))
            return null;

        return value.Length switch
        {
            10 => CedulaCodigo,
            13 => RucCodigo,
            _ => null
        };
    }

    public static string? GetNaturalPersonCedula(
        string tipoCodigo,
        string? numero)
    {
        var type = (tipoCodigo ?? string.Empty).Trim().ToUpperInvariant();
        var normalized = Normalize(numero, type);
        if (type == CedulaCodigo)
            return ValidateCedula(normalized).IsValid ? normalized : null;

        if (type != RucCodigo ||
            normalized.Length != 13 ||
            normalized[10..] != "001" ||
            normalized[2] - '0' >= 6 ||
            !ValidateRuc(normalized).IsValid ||
            !ValidateCedula(normalized[..10]).IsValid)
            return null;

        return normalized[..10];
    }

    public static bool BelongToSameNaturalPerson(
        string firstType,
        string? firstNumber,
        string secondType,
        string? secondNumber)
    {
        var firstCedula = GetNaturalPersonCedula(firstType, firstNumber);
        var secondCedula = GetNaturalPersonCedula(secondType, secondNumber);
        return firstCedula is not null &&
               string.Equals(firstCedula, secondCedula, StringComparison.Ordinal);
    }

    private static IdentificacionValidationResult ValidateCedula(string value)
    {
        if (!OnlyDigits(value, 10, out var error))
            return new(false, value, error);

        var province = int.Parse(value[..2]);
        var third = value[2] - '0';
        var regularProvince = province is >= 1 and <= 24;
        var consularOrForeignCedula = province == 30 && third is 4 or 5;
        if (!regularProvince && !consularOrForeignCedula)
            return new(false, value, "El código de provincia de la cédula no es válido.");

        if (regularProvince && third >= 7)
            return new(false, value, "El tercer dígito de la cédula no es válido.");

        var sum = 0;
        for (var index = 0; index < 9; index++)
        {
            var digit = value[index] - '0';
            var product = digit * (index % 2 == 0 ? 2 : 1);
            sum += product > 9 ? product - 9 : product;
        }

        var verifier = (10 - sum % 10) % 10;
        return verifier == value[9] - '0'
            ? new(true, value)
            : new(false, value, "El dígito verificador de la cédula no es válido.");
    }

    private static IdentificacionValidationResult ValidateRuc(string value)
    {
        if (!OnlyDigits(value, 13, out var error))
            return new(false, value, error);

        // El SRI no define un algoritmo universal para todos los RUC,
        // especialmente para personas naturales extranjeras. La existencia
        // se confirma contra las fuentes oficiales.
        return new(true, value);
    }

    private static IdentificacionValidationResult ValidateExternal(string value)
    {
        if (value.Length is < 3 or > 20)
            return new(false, value,
                "La identificación debe contener entre 3 y 20 caracteres.");

        return value.All(c => char.IsLetterOrDigit(c) || c is '-' or '_')
            ? new(true, value)
            : new(false, value,
                "La identificación contiene caracteres no permitidos.");
    }

    private static bool OnlyDigits(
        string value,
        int expectedLength,
        out string? error)
    {
        if (value.Length != expectedLength)
        {
            error = $"La identificación debe contener {expectedLength} dígitos.";
            return false;
        }

        if (!value.All(char.IsDigit))
        {
            error = "La identificación solo puede contener números.";
            return false;
        }

        error = null;
        return true;
    }
}
