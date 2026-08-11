namespace KONTAXPRO.Application.Models.Compras;

public sealed class ArchivoCompraGuardadoDto
{
    public string RutaRelativa { get; init; } = string.Empty;
    public string Sha256 { get; init; } = string.Empty;
    public long Tamano { get; init; }
}

