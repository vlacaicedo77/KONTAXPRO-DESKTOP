using KONTAXPRO.Application.Models.Compras;

namespace KONTAXPRO.Application.Interfaces;

public interface ICompraImportacionService
{
    Task<CompraImportacionAnalisisDto> AnalizarXmlAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default);
}
