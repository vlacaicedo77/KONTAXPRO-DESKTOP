using System.Security.Cryptography;
using System.Text;
using KONTAXPRO.Infrastructure.Compras;

namespace KONTAXPRO.Tests.Compras;

public sealed class ArchivoCompraFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(),
        "kontaxpro-compras-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task Guardar_ConservaBytesHashYRutaRelativa()
    {
        var bytes = Encoding.UTF8.GetBytes("<factura id=\"comprobante\"/>");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var storage = Create();
        var result = await storage.GuardarXmlOriginalAsync(7,
            new string('1', 49), hash, bytes);
        Assert.False(Path.IsPathRooted(result.RutaRelativa));
        Assert.Equal(hash, result.Sha256);
        await using var stream = await storage.AbrirLecturaAsync(
            result.RutaRelativa);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        Assert.Equal(bytes, memory.ToArray());
    }

    [Fact]
    public async Task Guardar_RechazaContenidoConHashDistinto()
    {
        var storage = Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.GuardarXmlOriginalAsync(7, new string('1', 49),
                new string('0', 64), Encoding.UTF8.GetBytes("<factura/>")));
    }

    [Fact]
    public async Task Guardar_Cancelado_NoDejaArchivoParcial()
    {
        var bytes = Encoding.UTF8.GetBytes("<factura id=\"comprobante\"/>");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var storage = Create();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            storage.GuardarXmlOriginalAsync(7, new string('1', 49), hash,
                bytes, cancellation.Token));

        Assert.Empty(Directory.Exists(_root)
            ? Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)
            : []);
    }

    [Fact]
    public async Task Abrir_RechazaTraversal()
    {
        var storage = Create();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.AbrirLecturaAsync("../secreto.xml"));
    }

    private ArchivoCompraFileStorage Create() => new(
        new CompraStorageOptions { DirectorioBase = _root });

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
