using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Cartera;

public sealed class CuentaPorCobrar
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
    public ICollection<CobroAplicacion> Aplicaciones { get; set; } = [];
    public ICollection<CuentaPorCobrarMovimiento> Movimientos { get; set; } = [];
}

public sealed class Cobro
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long EmpresaTerceroId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroCobro { get; set; } = string.Empty;
    public DateTime FechaCobro { get; set; }
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
    public ICollection<CobroMedio> Medios { get; set; } = [];
    public ICollection<CobroAplicacion> Aplicaciones { get; set; } = [];
}

public sealed class CobroMedio
{
    public long Id { get; set; }
    public long CobroId { get; set; }
    public long MedioPagoId { get; set; }
    public decimal Valor { get; set; }
    public string? Referencia { get; set; }
    public DateTime CreatedAt { get; set; }
    public Cobro? Cobro { get; set; }
    public MedioPago? MedioPago { get; set; }
}

public sealed class CobroAplicacion
{
    public long Id { get; set; }
    public long CobroId { get; set; }
    public long CuentaPorCobrarId { get; set; }
    public decimal ValorAplicado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public DateTime CreatedAt { get; set; }
    public Cobro? Cobro { get; set; }
    public CuentaPorCobrar? CuentaPorCobrar { get; set; }
    public CobroAplicacionReverso? Reverso { get; set; }
}

public sealed class CobroAplicacionReverso
{
    public long Id { get; set; }
    public long CobroAplicacionId { get; set; }
    public long UsuarioId { get; set; }
    public decimal ValorReversado { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public CobroAplicacion? CobroAplicacion { get; set; }
    public Usuario? Usuario { get; set; }
}

public sealed class CuentaPorCobrarMovimiento
{
    public long Id { get; set; }
    public long CuentaPorCobrarId { get; set; }
    public int Secuencia { get; set; }
    public long TipoMovimientoCarteraId { get; set; }
    public string? OrigenTipo { get; set; }
    public long? OrigenId { get; set; }
    public long? CobroAplicacionId { get; set; }
    public long? ReversoAplicacionId { get; set; }
    public decimal Valor { get; set; }
    public decimal SaldoAnterior { get; set; }
    public decimal SaldoNuevo { get; set; }
    public string? Descripcion { get; set; }
    public long UsuarioId { get; set; }
    public DateTime CreatedAt { get; set; }
    public CuentaPorCobrar? CuentaPorCobrar { get; set; }
    public TipoMovimientoCartera? TipoMovimientoCartera { get; set; }
    public CobroAplicacion? CobroAplicacion { get; set; }
    public CobroAplicacionReverso? ReversoAplicacion { get; set; }
    public Usuario? Usuario { get; set; }
}
