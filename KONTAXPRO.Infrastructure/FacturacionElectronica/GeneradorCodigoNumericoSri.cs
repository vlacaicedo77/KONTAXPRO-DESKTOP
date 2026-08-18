using System.Globalization;
using System.Security.Cryptography;
using KONTAXPRO.Application.Interfaces;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class GeneradorCodigoNumericoSri : IGeneradorCodigoNumericoSri
{
    public string Generar() => RandomNumberGenerator
        .GetInt32(0, 100_000_000)
        .ToString("D8", CultureInfo.InvariantCulture);
}

