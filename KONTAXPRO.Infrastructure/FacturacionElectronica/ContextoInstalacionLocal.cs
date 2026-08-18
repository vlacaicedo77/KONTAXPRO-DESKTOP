using KONTAXPRO.Application.Interfaces;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class ContextoInstalacionLocal : IContextoInstalacion
{
    private const string InstallationIdFile = ".kontaxpro-installation-id";

    public ContextoInstalacionLocal(
        string directorioBase,
        string tipoInstalacion,
        string? instalacionId = null)
    {
        DirectorioBase = Path.GetFullPath(directorioBase);
        TipoInstalacion = NormalizarTipo(tipoInstalacion);
        InstalacionId = ObtenerInstalacionId(instalacionId);
    }

    public Guid InstalacionId { get; }
    public string TipoInstalacion { get; }
    public string DirectorioBase { get; }

    private static string NormalizarTipo(string value)
    {
        if (string.Equals(value, "SERVIDOR", StringComparison.OrdinalIgnoreCase))
            return "SERVIDOR";
        if (string.Equals(value, "CLIENTE", StringComparison.OrdinalIgnoreCase))
            return "CLIENTE";
        throw new InvalidOperationException(
            "Installation:Type debe ser SERVIDOR o CLIENTE.");
    }

    private Guid ObtenerInstalacionId(string? configuredValue)
    {
        if (!string.IsNullOrWhiteSpace(configuredValue))
        {
            if (Guid.TryParse(configuredValue, out var configured) &&
                configured != Guid.Empty)
                return configured;
            throw new InvalidOperationException(
                "Installation:Id debe contener un UUID válido.");
        }

        Directory.CreateDirectory(DirectorioBase);
        var path = Path.Combine(DirectorioBase, InstallationIdFile);
        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (Guid.TryParse(existing, out var parsed) && parsed != Guid.Empty)
                return parsed;
            throw new InvalidOperationException(
                "El identificador local de instalación está dañado.");
        }

        var created = Guid.NewGuid();
        var temporary = path + $".{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, created.ToString("D"));
        try
        {
            File.Move(temporary, path, overwrite: false);
            return created;
        }
        catch (IOException) when (File.Exists(path))
        {
            File.Delete(temporary);
            var existing = File.ReadAllText(path).Trim();
            if (Guid.TryParse(existing, out var parsed) && parsed != Guid.Empty)
                return parsed;
            throw;
        }
    }
}
