using System.Security.Cryptography;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Tesoreria;
using KONTAXPRO.Infrastructure.Compras;

namespace KONTAXPRO.Infrastructure.Tesoreria;

public sealed class SoporteSinSustentoFileStorage(CompraStorageOptions options)
    : ISoporteSinSustentoStorage
{
    private const int MaxBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png" };
    private readonly string _root = ResolveRoot(options.DirectorioBase);

    public async Task<SoporteSinSustentoGuardadoDto> GuardarAsync(
        long empresaId,
        string nombreArchivo,
        ReadOnlyMemory<byte> contenido,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 || contenido.IsEmpty || contenido.Length > MaxBytes)
            throw new InvalidOperationException(
                "El soporte está vacío o supera el límite permitido de 10 MB.");
        var extension = Path.GetExtension(Path.GetFileName(nombreArchivo));
        if (!AllowedExtensions.Contains(extension))
            throw new InvalidOperationException(
                "El soporte debe ser PDF, JPG, JPEG o PNG.");
        var now = DateTime.UtcNow;
        var hash = Convert.ToHexString(SHA256.HashData(contenido.Span))
            .ToLowerInvariant();
        var relative = Path.Combine("sin-sustento", empresaId.ToString(),
            now.ToString("yyyy"), now.ToString("MM"),
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        var finalPath = ResolveSafePath(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        var temporary = finalPath + $".{Guid.NewGuid():N}.tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, contenido.ToArray(), cancellationToken);
            File.Move(temporary, finalPath, false);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return new(relative.Replace('\\', '/'), hash, contenido.Length);
    }

    public Task EliminarAsync(string rutaRelativa,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveSafePath(rutaRelativa);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public async Task<SoporteSinSustentoContenidoDto> LeerVerificadoAsync(
        string rutaRelativa, string nombreArchivo, string sha256Esperado,
        long tamanoEsperado, CancellationToken cancellationToken = default)
    {
        var path = ResolveSafePath(rutaRelativa);
        if (!File.Exists(path))
            throw new InvalidOperationException("El archivo de evidencia ya no existe en el almacenamiento documental.");
        var info = new FileInfo(path);
        if (info.Length != tamanoEsperado || info.Length <= 0 || info.Length > MaxBytes)
            throw new InvalidOperationException("El tamaño de la evidencia no coincide con el registro y su integridad no puede garantizarse.");
        var content = await File.ReadAllBytesAsync(path, cancellationToken);
        var hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(hash), Convert.FromHexString(sha256Esperado)))
            throw new InvalidOperationException("La evidencia fue alterada: su huella SHA-256 no coincide.");
        var safeName = Path.GetFileName(nombreArchivo);
        var extension = Path.GetExtension(safeName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
        return new SoporteSinSustentoContenidoDto(safeName, contentType, content);
    }

    private string ResolveSafePath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            throw new InvalidOperationException("La ruta documental no es válida.");
        var candidate = Path.GetFullPath(Path.Combine(_root,
            relative.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root : _root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "La ruta documental está fuera del directorio autorizado.");
        return candidate;
    }

    private static string ResolveRoot(string configured) => Path.GetFullPath(
        string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
                "KONTAXPRO", "documentos")
            : configured);
}
