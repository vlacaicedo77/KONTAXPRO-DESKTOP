using System.Text.Json;

namespace KONTAXPRO.Infrastructure.Interoperabilidad;

internal static class JsonResponseReader
{
    public static async Task<JsonDocument?> TryReadDocumentAsync(
        HttpContent content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var stream =
                await content.ReadAsStreamAsync(cancellationToken);
            return await JsonDocument.ParseAsync(
                stream,
                cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public static bool TryGetProperty(
        JsonElement element,
        string name,
        out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    public static string? GetString(
        JsonElement element,
        params string[] names)
    {
        foreach (var name in names)
        {
            if (!TryGetProperty(element, name, out var value))
                continue;

            var text = value.ValueKind switch
            {
                JsonValueKind.String => value.GetString(),
                JsonValueKind.Number => value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(text))
                return text.Trim();
        }

        return null;
    }

    public static JsonElement? GetFirstResult(JsonElement root)
    {
        if (!TryGetProperty(root, "resultado", out var result))
            return null;

        if (result.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in result.EnumerateArray())
                return item;
            return null;
        }

        return result.ValueKind == JsonValueKind.Object ? result : null;
    }

    public static JsonElement UnwrapObjectOrFirstArrayItem(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
            return value;

        foreach (var item in value.EnumerateArray())
            return item;

        return value;
    }

    public static bool IsJsonContentType(string? mediaType) =>
        mediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true;
}
