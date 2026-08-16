using KONTAXPRO.Application.Models.Inventario;

namespace KONTAXPRO.Application.Inventory;

public static class TransferenciaInventarioRules
{
    public const string InitialWarehouseCorrectionMarker =
        "CORRECCION_BODEGA_INVENTARIO_INICIAL";

    public static string? Validate(
        CorreccionBodegaInventarioInicialRequest request)
    {
        if (request.EmpresaId <= 0 || request.UsuarioId <= 0 ||
            request.ProductoId <= 0)
            return "La empresa, el usuario y el producto son obligatorios.";
        if (request.BodegaOrigenId <= 0 || request.BodegaDestinoId <= 0)
            return "Selecciona las bodegas de origen y destino.";
        if (request.BodegaOrigenId == request.BodegaDestinoId)
            return "La bodega correcta debe ser diferente de la bodega actual.";
        if (request.CantidadBase <= 0)
            return "La cantidad a corregir debe ser mayor que cero.";
        if (string.IsNullOrWhiteSpace(request.Motivo) ||
            request.Motivo.Trim().Length < 5)
            return "Explica la corrección con al menos 5 caracteres.";
        if (request.Motivo.Trim().Length > 500)
            return "El motivo no puede superar 500 caracteres.";
        return null;
    }

    public static string? Validate(TransferenciaInventarioRequest request)
    {
        if (request.EmpresaId <= 0 || request.UsuarioId <= 0)
            return "La empresa y el usuario son obligatorios.";
        if (request.EstablecimientoOrigenId <= 0 ||
            request.EstablecimientoDestinoId <= 0 ||
            request.BodegaOrigenId <= 0 || request.BodegaDestinoId <= 0)
            return "Selecciona las bodegas de origen y destino.";
        if (request.BodegaOrigenId == request.BodegaDestinoId)
            return "La bodega de destino debe ser diferente de la bodega de origen.";
        if (string.IsNullOrWhiteSpace(request.Motivo) ||
            request.Motivo.Trim().Length < 5)
            return "Indica un motivo de al menos 5 caracteres.";
        if (request.Motivo.Trim().Length > 500)
            return "El motivo no puede superar 500 caracteres.";
        if (request.Observacion?.Trim().Length > 1000)
            return "La observación no puede superar 1000 caracteres.";
        if (request.Detalles.Count == 0)
            return "Agrega al menos un producto a la transferencia.";
        if (request.Detalles.Any(x => x.ProductoId <= 0 ||
            x.ProductoPresentacionId <= 0 || x.Cantidad <= 0))
            return "Cada línea requiere producto, presentación y cantidad mayor que cero.";
        return null;
    }
}
