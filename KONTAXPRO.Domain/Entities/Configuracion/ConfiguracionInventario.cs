namespace KONTAXPRO.Domain.Entities.Configuracion;

public class ConfiguracionInventario
{
    public long Id { get; set; }

    public long EmpresaId { get; set; }

    public string MetodoCosteo { get; set; } = "PROMEDIO_PONDERADO";

    public string BaseCalculoPrecio { get; set; } = "COSTO_PROMEDIO";

    public string PoliticaActualizacionPrecio { get; set; } = "SUGERIR";

    public bool IvaCompraFormaParteCosto { get; set; }

    public bool PreciosVentaIncluyenImpuesto { get; set; } = true;

    public bool PermitirVentaSinStock { get; set; }

    public bool UsarControlLotes { get; set; } = true;

    public bool UsarControlSeries { get; set; } = true;

    public bool UsarFechaCaducidad { get; set; } = true;

    public short Estado { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
}