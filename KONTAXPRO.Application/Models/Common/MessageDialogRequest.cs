namespace KONTAXPRO.Application.Models.Common;

public sealed record MessageDialogRequest(
    MessageDialogType Type,
    string Title,
    string Message,
    string? Detail = null,
    string PrimaryText = "Entendido",
    string? SecondaryText = null,
    bool IsDestructive = false);

