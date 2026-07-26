using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Inventario;

public class MovimientoInventario
{
    public long Id { get; set; }

    public long EmpresaId { get; set; }

    public long? BodegaOrigenId { get; set; }

    public long? BodegaDestinoId { get; set; }

    public string TipoMovimiento { get; set; } = string.Empty;

    public DateTime FechaMovimiento { get; set; }

    public string? NumeroDocumento { get; set; }

    public string? Referencia { get; set; }

    public string? Observacion { get; set; }

    public long? UsuarioId { get; set; }

    public string Estado { get; set; } = "PROCESADO";

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }

    public Bodega? BodegaOrigen { get; set; }

    public Bodega? BodegaDestino { get; set; }

    public Usuario? Usuario { get; set; }

    public ICollection<MovimientoInventarioDetalle> Detalles { get; set; }
        = new List<MovimientoInventarioDetalle>();
}