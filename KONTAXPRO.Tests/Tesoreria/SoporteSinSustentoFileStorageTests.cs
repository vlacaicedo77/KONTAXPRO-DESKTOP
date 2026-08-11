using KONTAXPRO.Infrastructure.Compras;
using KONTAXPRO.Infrastructure.Tesoreria;

namespace KONTAXPRO.Tests.Tesoreria;

public sealed class SoporteSinSustentoFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(),
        "kontaxpro-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ReadVerified_ReturnsOriginalContent()
    {
        var storage = CreateStorage();
        byte[] content = [1, 2, 3, 4, 5];
        var saved = await storage.GuardarAsync(7, "respaldo.pdf", content);

        var result = await storage.LeerVerificadoAsync(saved.RutaRelativa,
            "respaldo.pdf", saved.Sha256, saved.Tamano);

        Assert.Equal("respaldo.pdf", result.NombreArchivo);
        Assert.Equal("application/pdf", result.TipoContenido);
        Assert.Equal(content, result.Contenido);
    }

    [Fact]
    public async Task ReadVerified_RejectsTamperedContent()
    {
        var storage = CreateStorage();
        var saved = await storage.GuardarAsync(7, "respaldo.png",
            new byte[] { 10, 20, 30 });
        var physical = Path.Combine(_root,
            saved.RutaRelativa.Replace('/', Path.DirectorySeparatorChar));
        await File.WriteAllBytesAsync(physical, new byte[] { 30, 20, 10 });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            storage.LeerVerificadoAsync(saved.RutaRelativa, "respaldo.png",
                saved.Sha256, saved.Tamano));

        Assert.Contains("SHA-256", error.Message);
    }

    private SoporteSinSustentoFileStorage CreateStorage() => new(
        new CompraStorageOptions { DirectorioBase = _root });

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, true);
    }
}
