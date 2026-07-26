using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Cartera;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Domain.Entities.Contabilidad;

namespace KONTAXPRO.Domain.Entities.Tesoreria;

public sealed class Caja
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EstablecimientoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public long CuentaContableId { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Establecimiento? Establecimiento { get; set; }
    public PlanCuenta? CuentaContable { get; set; }
    public ICollection<CajaSesion> Sesiones { get; set; } = [];
}

public sealed class CajaSesion
{
    public long Id { get; set; }
    public long CajaId { get; set; }
    public long AbiertaPorUsuarioId { get; set; }
    public DateTime FechaApertura { get; set; }
    public decimal SaldoInicial { get; set; }
    public long? CerradaPorUsuarioId { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal? SaldoSistema { get; set; }
    public decimal? SaldoContado { get; set; }
    public decimal? Diferencia { get; set; }
    public string Estado { get; set; } = "ABIERTA";
    public string? Observacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Caja? Caja { get; set; }
    public Usuario? AbiertaPorUsuario { get; set; }
    public Usuario? CerradaPorUsuario { get; set; }
    public ICollection<MovimientoCaja> Movimientos { get; set; } = [];
}

public sealed class MovimientoCaja
{
    public long Id { get; set; }
    public long CajaSesionId { get; set; }
    public long UsuarioId { get; set; }
    public long TipoMovimientoCajaId { get; set; }
    public string? OrigenTipo { get; set; }
    public long? OrigenId { get; set; }
    public long? CobroMedioId { get; set; }
    public long? PagoMedioId { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public decimal Valor { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Estado { get; set; } = "CONFIRMADO";
    public long? MovimientoReversoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public CajaSesion? CajaSesion { get; set; }
    public Usuario? Usuario { get; set; }
    public TipoMovimientoCaja? TipoMovimientoCaja { get; set; }
    public CobroMedio? CobroMedio { get; set; }
    public PagoMedio? PagoMedio { get; set; }
    public MovimientoCaja? MovimientoReverso { get; set; }
    public MovimientoCaja? MovimientoOrigenReversado { get; set; }
}

public sealed class DepositoCajaBanco
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long CajaSesionId { get; set; }
    public long CuentaBancariaId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroDeposito { get; set; } = string.Empty;
    public DateTime FechaDeposito { get; set; }
    public decimal Valor { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public CajaSesion? CajaSesion { get; set; }
    public KONTAXPRO.Domain.Entities.Bancos.CuentaBancaria? CuentaBancaria
        { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
}
