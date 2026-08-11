using System.Security.Cryptography;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Infrastructure.Compras;

public sealed class ArchivoCompraFileStorage(CompraStorageOptions options)
    : IArchivoCompraStorage
{
    private const int MaxBytes = 5 * 1024 * 1024;
    private readonly string _root = ResolveRoot(options.DirectorioBase);

    public async Task<ArchivoCompraGuardadoDto> GuardarXmlOriginalAsync(
        long empresaId,
        string claveAcceso,
        string sha256Esperado,
        ReadOnlyMemory<byte> contenido,
        CancellationToken cancellationToken = default)
    {
        if (empresaId <= 0 || claveAcceso.Length != 49 ||
            claveAcceso.Any(x => !char.IsDigit(x)))
            throw new InvalidOperationException(
                "No se puede almacenar el XML sin empresa y clave de acceso válidas.");
        if (contenido.IsEmpty || contenido.Length > MaxBytes)
            throw new InvalidOperationException(
                "El XML está vacío o supera el límite permitido de 5 MB.");

        var hash = Convert.ToHexString(SHA256.HashData(contenido.Span))
            .ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(hash),
                ParseExpectedHash(sha256Esperado)))
            throw new InvalidOperationException(
                "El contenido del XML cambió después de ser validado.");

        var now = DateTime.UtcNow;
        var relative = Path.Combine(
            "compras",
            empresaId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            now.ToString("yyyy", System.Globalization.CultureInfo.InvariantCulture),
            now.ToString("MM", System.Globalization.CultureInfo.InvariantCulture),
            $"{claveAcceso}_{Guid.NewGuid():N}.xml");
        var finalPath = ResolveSafePath(relative);
        var directory = Path.GetDirectoryName(finalPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath, FileMode.CreateNew,
                             FileAccess.Write, FileShare.None, 81920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(contenido, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }
            File.Move(temporaryPath, finalPath, false);
            return new ArchivoCompraGuardadoDto
            {
                RutaRelativa = relative.Replace('\\', '/'),
                Sha256 = hash,
                Tamano = contenido.Length
            };
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public Task EliminarAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolveSafePath(rutaRelativa);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<Stream> AbrirLecturaAsync(
        string rutaRelativa,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(ResolveSafePath(rutaRelativa),
            FileMode.Open, FileAccess.Read, FileShare.Read, 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return Task.FromResult(stream);
    }

    private string ResolveSafePath(string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
            throw new InvalidOperationException("La ruta documental no es válida.");
        var candidate = Path.GetFullPath(Path.Combine(
            _root, relative.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = _root.EndsWith(Path.DirectorySeparatorChar)
            ? _root
            : _root + Path.DirectorySeparatorChar;
        if (!candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "La ruta documental está fuera del directorio autorizado.");
        return candidate;
    }

    private static string ResolveRoot(string configured)
    {
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData),
                "KONTAXPRO", "documentos")
            : configured;
        return Path.GetFullPath(root);
    }

    private static byte[] ParseExpectedHash(string value)
    {
        try
        {
            var parsed = Convert.FromHexString(value);
            if (parsed.Length == 32) return parsed;
        }
        catch (FormatException)
        {
        }
        throw new InvalidOperationException("El hash esperado del XML no es válido.");
    }
}
