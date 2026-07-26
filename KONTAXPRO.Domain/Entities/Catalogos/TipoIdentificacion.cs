namespace KONTAXPRO.Domain.Entities.Catalogos;

public class TipoIdentificacion
{
    public long Id { get; set; }

    public string CodigoSri { get; set; } = string.Empty;

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public int LongitudMinima { get; set; }

    public int LongitudMaxima { get; set; }

    public int Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
