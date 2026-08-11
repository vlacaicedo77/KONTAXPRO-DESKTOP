using KONTAXPRO.Domain.Entities.Catalogos;
using KONTAXPRO.Domain.Entities.Comercial;
using KONTAXPRO.Domain.Entities.Configuracion;
using KONTAXPRO.Domain.Entities.Seguridad;

namespace KONTAXPRO.Domain.Entities.Contabilidad;

public sealed class PlanCuenta
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long? CuentaPadreId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Naturaleza { get; set; } = string.Empty;
    public bool AceptaMovimientos { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public PlanCuenta? CuentaPadre { get; set; }
    public ICollection<PlanCuenta> CuentasHijas { get; set; } = [];
}

public sealed class ConfiguracionCuenta
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long TipoConfiguracionContableId { get; set; }
    public long CuentaContableId { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public TipoConfiguracionContable? TipoConfiguracionContable { get; set; }
    public PlanCuenta? CuentaContable { get; set; }
}

public sealed class PeriodoContable
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public int Anio { get; set; }
    public int Mes { get; set; }
    public DateOnly FechaInicio { get; set; }
    public DateOnly FechaFin { get; set; }
    public string Estado { get; set; } = "ABIERTO";
    public long? CerradoPorUsuarioId { get; set; }
    public DateTime? CerradoAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public Usuario? CerradoPorUsuario { get; set; }
    public ICollection<Asiento> Asientos { get; set; } = [];
}

public sealed class SecuencialAsiento
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public int Anio { get; set; }
    public long UltimoSecuencial { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
}

public sealed class Asiento
{
    public long Id { get; set; }
    public long EmpresaId { get; set; }
    public long PeriodoId { get; set; }
    public long UsuarioId { get; set; }
    public string NumeroAsiento { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public string TipoAsiento { get; set; } = string.Empty;
    public long TipoOrigenAsientoId { get; set; }
    public long? OrigenId { get; set; }
    public long? AsientoOrigenReversadoId { get; set; }
    public string Concepto { get; set; } = string.Empty;
    public string Estado { get; set; } = "CONTABILIZADO";
    public long? AnuladoPorUsuarioId { get; set; }
    public DateTime? AnuladaAt { get; set; }
    public string? MotivoAnulacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Empresa? Empresa { get; set; }
    public PeriodoContable? Periodo { get; set; }
    public Usuario? Usuario { get; set; }
    public TipoOrigenAsiento? TipoOrigenAsiento { get; set; }
    public Asiento? AsientoOrigenReversado { get; set; }
    public Asiento? AsientoReverso { get; set; }
    public Usuario? AnuladoPorUsuario { get; set; }
    public ICollection<AsientoDetalle> Detalles { get; set; } = [];

    public bool EstaBalanceado() =>
        Detalles.Count >= 2 &&
        Detalles.Sum(x => x.Debe) == Detalles.Sum(x => x.Haber);
}

public sealed class AsientoDetalle
{
    public long Id { get; set; }
    public long AsientoId { get; set; }
    public long EmpresaId { get; set; }
    public long CuentaContableId { get; set; }
    public long? EmpresaTerceroId { get; set; }
    public int Orden { get; set; }
    public string? Descripcion { get; set; }
    public decimal Debe { get; set; }
    public decimal Haber { get; set; }
    public DateTime CreatedAt { get; set; }
    public Asiento? Asiento { get; set; }
    public PlanCuenta? CuentaContable { get; set; }
    public EmpresaTercero? EmpresaTercero { get; set; }
}
