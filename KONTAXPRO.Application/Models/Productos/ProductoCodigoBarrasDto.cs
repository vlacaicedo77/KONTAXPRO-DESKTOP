namespace KONTAXPRO.Application.Models.Productos;

public class ProductoCodigoBarrasDto
{
    public long ProductoId { get; set; }

    public long PresentacionId { get; set; }

    public string CodigoInterno { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string? Modelo { get; set; }

    public string Presentacion { get; set; } =
        string.Empty;

    public string CodigoBarras { get; set; } =
        string.Empty;
}