namespace KONTAXPRO.Application.Security;

public static class ComprasPermissions
{
    public const string Ver = "COMPRAS_VER";
    public const string Crear = "COMPRAS_CREAR";
    public const string Editar = "COMPRAS_EDITAR";
    public const string ImportarXml = "COMPRAS_IMPORTAR_XML";
    public const string Recibir = "COMPRAS_RECIBIR";
    public const string Anular = "COMPRAS_ANULAR";
    public const string VerCostos = "COMPRAS_VER_COSTOS";
}

public static class ComprasAuditActions
{
    public const string CompraCreada = "COMPRA_CREADA";
    public const string CompraAnulada = "COMPRA_ANULADA";
    public const string CompraSustituida = "COMPRA_SUSTITUIDA";
    public const string EquivalenciaCreada =
        "PROVEEDOR_PRODUCTO_EQUIVALENCIA_CREADA";
    public const string EquivalenciaActualizada =
        "PROVEEDOR_PRODUCTO_EQUIVALENCIA_ACTUALIZADA";
    public const string RecepcionConfirmada = "COMPRA_RECEPCION_CONFIRMADA";
    public const string RecepcionAnulada = "COMPRA_RECEPCION_ANULADA";
}

public static class ComprasConcurrency
{
    public const string UserMessage =
        "La compra o recepción fue modificada por otro usuario. Recarga la información antes de continuar.";
}
