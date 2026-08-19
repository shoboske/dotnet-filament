namespace Fila.Actions;

/// <summary>Filament's ForceDeleteAction: permanently removes a record, bypassing (or standing
/// in for, on a resource with no soft-delete support at all) the ordinary soft delete. See
/// CreateAction's doc comment for why this takes a delegate instead of a Resource reference.</summary>
public static class ForceDeleteAction
{
    public static Action Make(string modalHeading, Func<ActionContext, Task> handle) =>
        new Action("force-delete")
            .Label("Delete permanently")
            .Icon("trash")
            .Color("danger")
            .ModalHeading(modalHeading)
            .ModalDescription("This cannot be undone.")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Deleted permanently", "danger");
}
