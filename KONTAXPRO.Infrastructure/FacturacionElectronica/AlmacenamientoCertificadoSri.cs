using System.Text;
using System.Security.Cryptography;
using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class AlmacenamientoCertificadoSri(
    IContextoInstalacion contexto,
    IProtectorSecretosLocal protector) : IAlmacenamientoCertificadoSri
{
    public async Task<string> GuardarAsync(
        long empresaId,
        ReadOnlyMemory<byte> pkcs12,
        string password,
        CancellationToken cancellationToken = default)
    {
        AsegurarServidor();
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (pkcs12.IsEmpty) throw new ArgumentException("El certificado está vacío.");
        if (pkcs12.Length > 5 * 1024 * 1024)
            throw new ArgumentException("El certificado no puede superar 5 MB.");
        var reference = Path.Combine("facturacion-electronica",
            $"empresa-{empresaId}", $"certificado-{Guid.NewGuid():N}");
        var directory = RutaSegura(reference);
        var staging = directory + ".tmp";
        Directory.CreateDirectory(staging);
        var certProtected = protector.Proteger(pkcs12.Span,
            $"KONTAXPRO:SRI:CERT:{empresaId}");
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        byte[]? passwordProtected = null;
        try
        {
            passwordProtected = protector.Proteger(passwordBytes,
                $"KONTAXPRO:SRI:PASSWORD:{empresaId}");
            await File.WriteAllBytesAsync(Path.Combine(staging, "cert.bin"),
                certProtected, cancellationToken);
            await File.WriteAllBytesAsync(Path.Combine(staging, "secret.bin"),
                passwordProtected, cancellationToken);
            Directory.Move(staging, directory);
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
            CryptographicOperations.ZeroMemory(certProtected);
            if (passwordProtected is not null)
                CryptographicOperations.ZeroMemory(passwordProtected);
        }
        return reference.Replace('\\', '/');
    }

    public async Task<MaterialCertificadoSri> LeerAsync(
        long empresaId,
        string referencia,
        CancellationToken cancellationToken = default)
    {
        AsegurarServidor();
        ValidarReferenciaEmpresa(empresaId, referencia);
        var directory = RutaSegura(referencia);
        var certProtected = await File.ReadAllBytesAsync(
            Path.Combine(directory, "cert.bin"), cancellationToken);
        var secretProtected = await File.ReadAllBytesAsync(
            Path.Combine(directory, "secret.bin"), cancellationToken);
        try
        {
            var p12 = protector.Desproteger(certProtected,
                $"KONTAXPRO:SRI:CERT:{empresaId}");
            var passwordBytes = protector.Desproteger(secretProtected,
                $"KONTAXPRO:SRI:PASSWORD:{empresaId}");
            try
            {
                return new(p12, Encoding.UTF8.GetString(passwordBytes));
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(certProtected);
            CryptographicOperations.ZeroMemory(secretProtected);
        }
    }

    public Task EliminarAsync(
        long empresaId,
        string referencia,
        CancellationToken cancellationToken = default)
    {
        AsegurarServidor();
        cancellationToken.ThrowIfCancellationRequested();
        ValidarReferenciaEmpresa(empresaId, referencia);
        var directory = RutaSegura(referencia);
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
        return Task.CompletedTask;
    }

    private void AsegurarServidor()
    {
        if (!contexto.EsServidor)
            throw new InvalidOperationException(
                "El certificado electrónico sólo puede administrarse en el nodo SERVIDOR.");
    }

    private static void ValidarReferenciaEmpresa(long empresaId, string reference)
    {
        var normalized = reference.Replace('\\', '/');
        var prefix = $"facturacion-electronica/empresa-{empresaId}/certificado-";
        if (!normalized.StartsWith(prefix, StringComparison.Ordinal) ||
            normalized.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException(
                "La referencia del certificado no corresponde a la empresa.");
    }

    private string RutaSegura(string referencia)
    {
        var root = Path.GetFullPath(contexto.DirectorioBase);
        var target = Path.GetFullPath(Path.Combine(root,
            referencia.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La referencia del certificado no es segura.");
        return target;
    }
}
