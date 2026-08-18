using KONTAXPRO.Application.Interfaces;
using KONTAXPRO.Application.Models.FacturacionElectronica;

namespace KONTAXPRO.Infrastructure.FacturacionElectronica;

public sealed class AlmacenamientoDocumentosElectronicos(IContextoInstalacion contexto)
    : IAlmacenamientoDocumentosElectronicos
{
    public async Task GuardarAsync(
        ArtefactoElectronico artefacto,
        Stream contenido,
        CancellationToken cancellationToken = default)
    {
        var path = ResolverRuta(artefacto);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            await using (var output = new FileStream(temporary, FileMode.CreateNew,
                FileAccess.Write, FileShare.None, 81920,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await contenido.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }
            File.Move(temporary, path, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public Task<Stream> AbrirLecturaAsync(
        ArtefactoElectronico artefacto,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = new FileStream(ResolverRuta(artefacto), FileMode.Open,
            FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
        return Task.FromResult(stream);
    }

    private string ResolverRuta(ArtefactoElectronico artefacto)
    {
        if (artefacto.EmpresaId <= 0 ||
            artefacto.RucEmpresa.Length != 13 || !artefacto.RucEmpresa.All(char.IsDigit) ||
            artefacto.ClaveAcceso.Length != 49 || !artefacto.ClaveAcceso.All(char.IsDigit))
            throw new ArgumentException("La identidad del artefacto electrónico no es válida.");
        var type = artefacto.TipoArtefacto.ToUpperInvariant();
        if (type is not ("GENERADO" or "FIRMADO" or "AUTORIZADO" or "RIDE"))
            throw new ArgumentException("El tipo de artefacto no está permitido.");
        var extension = type == "RIDE" ? ".pdf" : ".xml";
        var root = Path.GetFullPath(Path.Combine(contexto.DirectorioBase,
            "facturacion-electronica", "documentos"));
        var target = Path.GetFullPath(Path.Combine(root,
            $"empresa-{artefacto.EmpresaId}", artefacto.RucEmpresa,
            artefacto.ClaveAcceso, type.ToLowerInvariant() + extension));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La ruta del artefacto no es segura.");
        return target;
    }
}
