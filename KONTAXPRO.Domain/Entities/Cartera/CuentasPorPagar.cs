using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Cartera;

public sealed class CuentaPorPagar
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public string OrigenTipo { get; set; } = string.Empty;
    public long OrigenId { get; set; }
    public DateOnly FechaOrigen { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public decimal ValorOriginal { get; set; }
    public decimal SaldoActual { get; set; }
    public string Estado { get; set; } = "PENDIENTE";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public ICollection<PagoAplicacion> Aplicaciones { get; set; } = [];
    public ICollection<CuentaPorPagarMovimiento> Movimientos { get; set; } = [];
}

public sealed class Pago
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroPago { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public decimal ValorTotal { get; set; }
    public string? Referencia { get; set; }
    public string? Observacion { get; set; }
    public string Estado { get; set; } = "CONFIRMADO";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<PagoMedio> Medios { get; set; } = [];
    public ICollection<PagoAplicacion> Aplicaciones { get; set; } = [];
}

public sealed class PagoMedio
{
    public long Id { get; set; }
    public long PagoId { get; set; }
    public long MedioPagoId { get; set; }
    public decimal Valor { get; set; }
    public string? Referencia { get; set; }
    public DateTime CreatedAt { get; set; }
    public Pago? Pago { get; set; }
    public MedioPago? MedioPago { get; set; }
}

public sealed class PagoAplicacion
{
    public long Id { get; set; }
    public long PagoId { get; set; }
    public long CuentaPorPagarId { get; set; }
    public decimal ValorAplicado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public DateTime CreatedAt { get; set; }
    public Pago? Pago { get; set; }
    public CuentaPorPagar? CuentaPorPagar { get; set; }
    public PagoAplicacionReverso? Reverso { get; set; }
}

public sealed class PagoAplicacionReverso
{
    public long Id { get; set; }
    public long PagoAplicacionId { get; set; }
    public long UsuarioId { get; set; }
    public decimal ValorReversado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public PagoAplicacion? PagoAplicacion { get; set; }
    public Usuario? Usuario { get; set; }
}

public sealed class CuentaPorPagarMovimiento
{
    public long Id { get; set; }
    public long CuentaPorPagarId { get; set; }
    public int Secuencia { get; set; }
    public long TipoMovimientoCuentaPorPagarId { get; set; }
    public string? OrigenTipo { get; set; }
    public long? OrigenId { get; set; }
    public long? PagoAplicacionId { get; set; }
    public long? ReversoAplicacionId { get; set; }
    public decimal Valor { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public string? Descripcion { get; set; }
    public long UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public CuentaPorPagar? CuentaPorPagar { get; set; }
    public TipoMovimientoCuentaPorPagar? TipoMovimientoCuentaPorPagar { get; set; }
    public PagoAplicacion? PagoAplicacion { get; set; }
    public PagoAplicacionReverso? ReversoAplicacion { get; set; }
    public Usuario? Usuario { get; set; }
}
