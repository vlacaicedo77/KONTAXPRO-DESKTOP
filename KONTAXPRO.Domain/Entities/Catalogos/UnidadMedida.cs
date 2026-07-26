namespace KONTAXPRO.Domain.Entities.Catalogos;

public class UnidadMedida
{
    public long Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Abreviatura { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public int Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
