namespace KONTAXPRO.Application.Models;

public sealed class EstablecimientoDisponible
{
    public long EstablecimientoId { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string Nombre { get; init; } = string.Empty;
    public string NombreComercial { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
    public bool EsMatriz { get; init; }

    public string EtiquetaTipo => EsMatriz ? "MATRIZ" : "ADICIONAL";
}
