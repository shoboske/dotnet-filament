namespace Fila.Actions;

/// <summary>Filament's DeleteBulkAction: deletes every selected row. Runs over the table's
/// native hx-confirm rather than Fila's modal — see BulkAction's doc comment for why. See
/// CreateAction's doc comment for why this takes a delegate instead of a Resource reference.</summary>
public static class DeleteBulkAction
{
    public static BulkAction Make(Func<BulkActionContext, Task> handle) =>
        new BulkAction("delete")
            .Label("Delete selected")
            .Color("danger")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Deleted", "danger");
}
