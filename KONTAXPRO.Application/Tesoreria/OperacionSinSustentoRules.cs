using KONTAXPRO.Application.Models.Tesoreria;

namespace KONTAXPRO.Application.Tesoreria;

public static class OperacionSinSustentoRules
{
    public static string? Validate(OperacionSinSustentoRequest request)
    {
        var type = request.TipoOperacion.Trim().ToUpperInvariant();
        var medium = request.MedioSalida.Trim().ToUpperInvariant();
        if (request.EmpresaId <= 0 || request.EstablecimientoId <= 0 ||
            request.UsuarioId <= 0 || type is not ("GASTO" or "INVENTARIO") ||
            medium is not ("CAJA" or "BANCO"))
            return "Empresa, establecimiento, usuario, tipo y medio de salida son obligatorios.";
        if (string.IsNullOrWhiteSpace(request.Beneficiario) ||
            string.IsNullOrWhiteSpace(request.Motivo))
            return "Beneficiario y motivo son obligatorios.";
        if (request.Beneficiario.Trim().Length > 250 ||
            request.Motivo.Trim().Length > 1000 ||
            request.Referencia?.Trim().Length > 255)
            return "Beneficiario, motivo o referencia supera la longitud permitida.";
        if ((medium == "CAJA") != request.CajaSesionId.HasValue ||
            (medium == "BANCO") != request.CuentaBancariaId.HasValue)
            return "Selecciona únicamente el fondo correspondiente al medio de salida.";
        if ((type == "INVENTARIO") != request.BodegaId.HasValue)
            return "La bodega solo es obligatoria para una adquisición de inventario.";
        if (request.Detalles.Count == 0 || request.Detalles.Any(x =>
                string.IsNullOrWhiteSpace(x.Descripcion) || x.CostoTotal <= 0))
            return "Agrega al menos un detalle con descripción y valor mayor que cero.";
        if (type == "GASTO" && request.Detalles.Any(x =>
                !x.CuentaContableId.HasValue || x.ProductoId.HasValue ||
                x.ProductoPresentacionId.HasValue))
            return "Cada gasto debe indicar una cuenta contable y no puede contener productos.";
        if (type == "INVENTARIO" && request.Detalles.Any(x =>
                x.CuentaContableId.HasValue || !x.ProductoId.HasValue ||
                !x.ProductoPresentacionId.HasValue || x.Cantidad <= 0))
            return "Cada línea de inventario debe indicar producto, presentación y cantidad válida.";
        if (type != "INVENTARIO" && request.Detalles.Any(x => x.Precios.Count > 0))
            return "Los precios pendientes solo pueden acompañar líneas de inventario.";
        if (request.Detalles.Any(x => x.Precios.Count > 0 &&
                !x.ProductoCreadoContextualmente))
            return "Los precios pendientes solo se permiten para un producto creado dentro de esta operación.";
        var prices = request.Detalles.SelectMany(x => x.Precios).ToList();
        if (prices.Any(x => x.ProductoPresentacionId <= 0 ||
                x.ListaPrecioId <= 0 || x.Estado != 1 ||
                x.MetodoCalculo is not ("PORCENTAJE_COSTO" or
                    "DESCUENTO_PORCENTAJE" or "PRECIO_FIJO") ||
                (x.MetodoCalculo == "PRECIO_FIJO"
                    ? !x.Precio.HasValue || x.Precio.Value <= 0 ||
                      x.Porcentaje.HasValue
                    : !x.Porcentaje.HasValue || x.Porcentaje.Value < 0 ||
                      x.Precio.HasValue)))
            return "La configuración pendiente de precios contiene valores inválidos.";
        if (prices.GroupBy(x => new
            {
                x.ProductoPresentacionId,
                x.ListaPrecioId
            }).Any(x => x.Count() > 1))
            return "No repitas una lista de precios para la misma presentación.";
        if ((request.EvidenciaContenido is { Length: > 0 }) !=
            !string.IsNullOrWhiteSpace(request.EvidenciaNombre))
            return "El nombre y el contenido del soporte deben enviarse juntos.";
        if (request.EvidenciaContenido is { Length: > 10 * 1024 * 1024 })
            return "El soporte supera el límite permitido de 10 MB.";
        return null;
    }
}
