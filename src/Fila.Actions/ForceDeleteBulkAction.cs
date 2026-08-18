namespace Fila.Actions;

/// <summary>Filament's ForceDeleteBulkAction: permanently deletes every selected row. Mounts
/// into Fila's shared modal for its confirmation step — see BulkAction's doc comment.</summary>
public static class ForceDeleteBulkAction
{
    public static BulkAction Make(string modalHeading, Func<BulkActionContext, Task> handle) =>
        new BulkAction("force-delete")
            .Label("Delete permanently")
            .Icon("trash")
            .Color("danger")
            .ModalHeading(modalHeading)
            .ModalSubmitLabel("Delete")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Deleted permanently", "danger");
}
