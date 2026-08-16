namespace KONTAXPRO.Application.Models.Inventario;

public sealed class InventoryCancellationRequest
{
    public long EmpresaId { get; set; }
    public long UsuarioId { get; set; }
    public long OperacionId { get; set; }
    public string Motivo { get; set; } = string.Empty;
}
