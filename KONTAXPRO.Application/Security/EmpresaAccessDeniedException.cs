namespace KONTAXPRO.Application.Security;

public sealed class EmpresaAccessDeniedException : InvalidOperationException
{
    public const string UserMessage =
        "No tienes autorización para operar con la empresa seleccionada.";

    public EmpresaAccessDeniedException()
        : base(UserMessage)
    {
    }
}
