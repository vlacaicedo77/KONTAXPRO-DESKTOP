namespace KONTAXPRO.Application.Models.Common;

public class CatalogCreateResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public CatalogItemDto? Item { get; set; }

    public static CatalogCreateResult Ok(
        CatalogItemDto item,
        string message)
    {
        return new CatalogCreateResult
        {
            Success = true,
            Item = item,
            Message = message
        };
    }

    public static CatalogCreateResult Fail(
        string message)
    {
        return new CatalogCreateResult
        {
            Success = false,
            Message = message
        };
    }
}