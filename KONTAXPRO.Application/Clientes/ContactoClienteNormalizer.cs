using System.Net.Mail;

namespace KONTAXPRO.Application.Clientes;

public sealed record ContactoNormalizationResult(
    bool IsValid,
    string? Value,
    string? Error = null);

public static class ContactoClienteNormalizer
{
    public const string DefaultEmail = "cliente@kontax.com";

    public static string? FirstEmailOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : value.Split(
                    [',', ';'],
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .FirstOrDefault();

    public static ContactoNormalizationResult NormalizeEmail(string? value)
    {
        var firstEmail = FirstEmailOrNull(value);
        if (firstEmail is null)
            return new(true, DefaultEmail);

        try
        {
            var address = new MailAddress(firstEmail);
            return new(true, address.Address.ToLowerInvariant());
        }
        catch (FormatException)
        {
            return new(false, null, "El correo electrónico no es válido.");
        }
    }

    public static ContactoNormalizationResult NormalizeOptionalEmail(
        string? value)
    {
        var firstEmail = FirstEmailOrNull(value);
        if (firstEmail is null)
            return new(true, null);

        try
        {
            var address = new MailAddress(firstEmail);
            return new(true, address.Address.ToLowerInvariant());
        }
        catch (FormatException)
        {
            return new(false, null, "El correo electrónico no es válido.");
        }
    }

    public static ContactoNormalizationResult NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new(true, null);

        var trimmed = value.Trim();
        if (trimmed.Any(character =>
                !char.IsDigit(character) &&
                character is not ('+' or ' ' or '-' or '(' or ')' or '.')) ||
            (trimmed.Contains('+') && !trimmed.StartsWith('+')) ||
            trimmed.Count(character => character == '+') > 1)
            return new(false, null,
                "El teléfono contiene caracteres no permitidos.");

        var hasInternationalPrefix = trimmed.StartsWith('+') ||
                                     trimmed.StartsWith("00");
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        string canonical;

        if (hasInternationalPrefix)
        {
            if (trimmed.StartsWith("00"))
                digits = digits[2..];
            canonical = $"+{digits}";
        }
        else if (digits.StartsWith("593", StringComparison.Ordinal))
        {
            canonical = $"+{digits}";
        }
        else if (digits.StartsWith('0') && digits.Length is 9 or 10)
        {
            canonical = $"+593{digits[1..]}";
        }
        else if (digits.StartsWith('9') && digits.Length == 9)
        {
            canonical = $"+593{digits}";
        }
        else
        {
            return new(false, null,
                "Ingresa un teléfono ecuatoriano válido o un número internacional con código de país.");
        }

        var internationalDigits = canonical[1..];
        if (internationalDigits.Length is < 8 or > 15 ||
            internationalDigits[0] == '0')
            return new(false, null,
                "El teléfono debe tener entre 8 y 15 dígitos en formato internacional.");

        return new(true, canonical);
    }
}
