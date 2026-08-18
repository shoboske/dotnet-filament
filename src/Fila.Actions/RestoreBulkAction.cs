namespace Fila.Actions;

/// <summary>Filament's RestoreBulkAction: un-does a soft delete for every selected row. Runs
/// over the table's native hx-confirm rather than Fila's modal — see BulkAction's doc comment
/// for why.</summary>
public static class RestoreBulkAction
{
    public static BulkAction Make(Func<BulkActionContext, Task> handle) =>
        new BulkAction("restore")
            .Label("Restore selected")
            .Color("success")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Restored", "success");
}
