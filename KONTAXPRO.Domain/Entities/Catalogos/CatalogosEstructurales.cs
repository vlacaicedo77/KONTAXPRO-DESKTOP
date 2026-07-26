namespace KONTAXPRO.Domain.Entities.Catalogos;

public interface ICatalogoBasico
{
    long Id { get; set; }
    string Codigo { get; set; }
    string Nombre { get; set; }
    string? Descripcion { get; set; }
    int Estado { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

public interface ICatalogoConCodigoSri : ICatalogoBasico
{
    string CodigoSri { get; set; }
}

public interface ICatalogoConNaturaleza : ICatalogoBasico
{
    string Naturaleza { get; set; }
}

public interface ICatalogoNumerico
{
    long Id { get; set; }
    int Codigo { get; set; }
    string Nombre { get; set; }
    int Estado { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
}

public class Impuesto
{
    public long Id { get; set; }
    public string CodigoSri { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<TarifaImpuesto> Tarifas { get; set; } =
        new List<TarifaImpuesto>();
    public ICollection<ConceptoRetencion> ConceptosRetencion { get; set; } =
        new List<ConceptoRetencion>();
}

public class TipoComprobante : ICatalogoConCodigoSri
{
    public long Id { get; set; }
    public string CodigoSri { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class FormaPago : ICatalogoConCodigoSri
{
    public long Id { get; set; }
    public string CodigoSri { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class MedioPago : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public long FormaPagoSriId { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public FormaPago? FormaPagoSri { get; set; }
}

public class TipoAmbiente : ICatalogoNumerico
{
    public long Id { get; set; }
    public int Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoEmision : ICatalogoNumerico
{
    public long Id { get; set; }
    public int Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EstadoSerie : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoDocumentoInterno : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string PrefijoDefault { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoMovimientoInventario : ICatalogoConNaturaleza
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenMovimientoInventario : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoMovimientoCartera : ICatalogoConNaturaleza
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoMovimientoCuentaPorPagar : ICatalogoConNaturaleza
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoConfiguracionContable : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenAsiento : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoMovimientoCaja : ICatalogoConNaturaleza
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoMovimientoBancario : ICatalogoConNaturaleza
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenGuiaRemision : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenDevolucionVenta : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenDevolucionCompra : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenRetencionEmitida : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TipoOrigenComprobanteElectronico : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class EstadoComprobanteElectronico : ICatalogoBasico
{
    public long Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ConceptoRetencion
{
    public long Id { get; set; }
    public long ImpuestoId { get; set; }
    public string CodigoSri { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Porcentaje { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public DateOnly? VigenteHasta { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Impuesto? Impuesto { get; set; }
}
