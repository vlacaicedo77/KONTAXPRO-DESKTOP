namespace KONTAXPRO.Application.Models.Inventario;

public class IngresoInventarioRequest
{
    public long EmpresaId { get; set; }

    public long BodegaId { get; set; }

    public long? UsuarioId { get; set; }

    public DateTime FechaMovimiento { get; set; } = DateTime.Now;

    public string? NumeroDocumento { get; set; }

    public string? Referencia { get; set; }

    public string? Observacion { get; set; }

    public List<IngresoInventarioDetalleRequest> Detalles { get; set; }
        = [];
}