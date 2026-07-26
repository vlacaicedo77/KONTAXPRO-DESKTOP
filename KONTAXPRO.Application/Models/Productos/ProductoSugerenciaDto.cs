namespace KONTAXPRO.Application.Models.Productos;

public class ProductoSugerenciaDto
{
    public long ProductoId { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Modelo { get; set; }

    public string? Marca { get; set; }

    public List<string> Presentaciones { get; set; }
        = new();

    public string PresentacionesTexto =>
        Presentaciones.Count == 0
            ? "Sin presentaciones registradas"
            : string.Join(" · ", Presentaciones);
}