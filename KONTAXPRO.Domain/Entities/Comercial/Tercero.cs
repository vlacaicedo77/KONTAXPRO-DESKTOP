using KONTAXPRO.Domain.Entities.Catalogos;

namespace KONTAXPRO.Domain.Entities.Comercial;

public class Tercero
{
    public long Id { get; set; }
    public long TipoIdentificacionId { get; set; }
    public string NumeroIdentificacion { get; set; } = string.Empty;
    public string RazonSocial { get; set; } = string.Empty;
    public string? NombreComercial { get; set; }
    public string? Direccion { get; set; }
    public string? Correo { get; set; }
    public string? Telefono { get; set; }
    public string OrigenRegistro { get; set; } = string.Empty;
    public string EstadoVerificacion { get; set; } = string.Empty;
    public string? FuenteVerificacion { get; set; }
    public DateTime? VerificadoAt { get; set; }
    public int Estado { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public bool EsConsumidorFinal =>
        NumeroIdentificacion == TerceroEstructural.ConsumidorFinalIdentificacion;

    public TipoIdentificacion? TipoIdentificacion { get; set; }
    public ICollection<EmpresaTercero> EmpresasTerceros { get; set; } =
        new List<EmpresaTercero>();
}

public static class TerceroEstructural
{
    public const string ConsumidorFinalIdentificacion = "9999999999999";
    public const string ConsumidorFinalRazonSocial = "CONSUMIDOR FINAL";
}
