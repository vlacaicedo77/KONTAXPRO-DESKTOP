using KONTAXPRO.Domain.Entities.Bancos;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Contabilidad;
using KONTAXPRO.Domain.Entities.Inventario;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Tesoreria;

public sealed class OperacionSinSustento
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroOperacion { get; set; } = string.Empty;
    public string TipoOperacion { get; set; } = string.Empty;
    public string MedioSalida { get; set; } = string.Empty;
    public long? CajaSesionId { get; set; }
    public long? CuentaBancariaId { get; set; }
    public long? BodegaId { get; set; }
    public DateOnly Fecha { get; set; }
    public string Beneficiario { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public string? EvidenciaRutaRelativa { get; set; }
    public string? EvidenciaNombre { get; set; }
    public string? EvidenciaSha256 { get; set; }
    public long? EvidenciaTamano { get; set; }
    public decimal Total { get; set; }
    public bool EsDeducible { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public long? MovimientoCajaId { get; set; }
    public long? MovimientoBancarioId { get; set; }
    public long? MovimientoInventarioId { get; set; }
    public long? AsientoId { get; set; }
    public long? OperacionSustituidaId { get; set; }
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public Usuario? Usuario { get; set; }
    public CajaSesion? CajaSesion { get; set; }
    public CuentaBancaria? CuentaBancaria { get; set; }
    public Bodega? Bodega { get; set; }
    public MovimientoCaja? MovimientoCaja { get; set; }
    public MovimientoBancario? MovimientoBancario { get; set; }
    public MovimientoInventario? MovimientoInventario { get; set; }
    public Asiento? Asiento { get; set; }
    public OperacionSinSustento? OperacionSustituida { get; set; }
    public OperacionSinSustento? OperacionSustituta { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<OperacionSinSustentoDetalle> Detalles { get; set; } = [];
}

public sealed class OperacionSinSustentoDetalle
{
    public long Id { get; set; }
    public long OperacionSinSustentoId { get; set; }
    public long EmpresaId { get; set; }
    public long? CuentaContableId { get; set; }
    public long? ProductoId { get; set; }
    public long? ProductoPresentacionId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public decimal CantidadPresentacion { get; set; }
    public decimal FactorConversion { get; set; }
    public decimal CantidadBase { get; set; }
    public decimal CostoUnitarioBase { get; set; }
    public decimal CostoTotal { get; set; }
    public DateTime CreatedAt { get; set; }

    public OperacionSinSustento? OperacionSinSustento { get; set; }
    public PlanCuenta? CuentaContable { get; set; }
    public Producto? Producto { get; set; }
    public ProductoPresentacion? ProductoPresentacion { get; set; }
}
