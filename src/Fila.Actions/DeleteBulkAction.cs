namespace Fila.Actions;

/// <summary>Filament's DeleteBulkAction: deletes every selected row. Mounts into Fila's shared
/// modal for its confirmation step — see BulkAction's doc comment. See CreateAction's doc
/// comment for why this takes a delegate instead of a Resource reference.</summary>
public static class DeleteBulkAction
{
    public static BulkAction Make(string modalHeading, Func<BulkActionContext, Task> handle) =>
        new BulkAction("delete")
            .Label("Delete selected")
            .Icon("trash")
            .Color("danger")
            .ModalHeading(modalHeading)
            .ModalSubmitLabel("Delete")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Deleted", "danger");
}
