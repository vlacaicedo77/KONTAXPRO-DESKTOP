using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Cartera;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;
using KONTAXPRO.Domain.Entities.Contabilidad;

namespace KONTAXPRO.Domain.Entities.Bancos;

public sealed class CuentaBancaria
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public string Banco { get; set; } = string.Empty;
    public string TipoCuenta { get; set; } = string.Empty;
    public string NumeroCuenta { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public long CuentaContableId { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public PlanCuenta? CuentaContable { get; set; }
    public ICollection<MovimientoBancario> Movimientos { get; set; } = [];
}

public sealed class MovimientoBancario
{
    public long Id { get; set; }
    public long CuentaBancariaId { get; set; }
    public long UsuarioId { get; set; }
    public long TipoMovimientoBancarioId { get; set; }
    public string? OrigenTipo { get; set; }
    public long? OrigenId { get; set; }
    public long? CobroMedioId { get; set; }
    public long? PagoMedioId { get; set; }
    public DateTime FechaMovimiento { get; set; }
    public decimal Valor { get; set; }
    public string? Referencia { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Estado { get; set; } = "CONFIRMADO";
    public long? MovimientoReversoId { get; set; }
    public DateTime CreatedAt { get; set; }
    public CuentaBancaria? CuentaBancaria { get; set; }
    public Usuario? Usuario { get; set; }
    public TipoMovimientoBancario? TipoMovimientoBancario { get; set; }
    public CobroMedio? CobroMedio { get; set; }
    public PagoMedio? PagoMedio { get; set; }
    public MovimientoBancario? MovimientoReverso { get; set; }
    public MovimientoBancario? MovimientoOrigenReversado { get; set; }
}

public sealed class TransferenciaBancaria
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long CuentaBancariaOrigenId { get; set; }
    public long CuentaBancariaDestinoId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroTransferencia { get; set; } = string.Empty;
    public DateTime FechaTransferencia { get; set; }
    public decimal Valor { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = "CONFIRMADA";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public CuentaBancaria? CuentaBancariaOrigen { get; set; }
    public CuentaBancaria? CuentaBancariaDestino { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
}
