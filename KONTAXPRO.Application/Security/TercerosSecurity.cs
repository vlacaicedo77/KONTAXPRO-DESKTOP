namespace KONTAXPRO.Application.Security;

public static class TercerosPermissions
{
    public const string Gestionar = "TERCEROS_GESTIONAR";
}

public sealed class TercerosAccessDeniedException : InvalidOperationException
{
    public const string UserMessage =
        "No tienes autorización para crear, editar o cambiar el estado de clientes y proveedores.";

    public TercerosAccessDeniedException()
        : base(UserMessage)
    {
    }
}

public static class TercerosConcurrency
{
    public const string UserMessage =
        "Este registro fue modificado por otro usuario. Recarga la información y revisa los cambios antes de guardar.";
}

public static class TercerosAuditActions
{
    public const string ClienteCreado = "CLIENTE_CREADO";
    public const string ClienteRolAsignado = "CLIENTE_ROL_ASIGNADO";
    public const string ClienteActualizado = "CLIENTE_ACTUALIZADO";
    public const string ClienteVerificado = "CLIENTE_VERIFICADO";
    public const string ClienteActivado = "CLIENTE_ACTIVADO";
    public const string ClienteInactivado = "CLIENTE_INACTIVADO";
    public const string ProveedorCreado = "PROVEEDOR_CREADO";
    public const string ProveedorRolAsignado = "PROVEEDOR_ROL_ASIGNADO";
    public const string ProveedorActualizado = "PROVEEDOR_ACTUALIZADO";
    public const string ProveedorVerificado = "PROVEEDOR_VERIFICADO";
    public const string ProveedorActivado = "PROVEEDOR_ACTIVADO";
    public const string ProveedorInactivado = "PROVEEDOR_INACTIVADO";
}
