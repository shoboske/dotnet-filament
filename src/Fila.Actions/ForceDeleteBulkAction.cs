namespace Fila.Actions;

/// <summary>Filament's ForceDeleteBulkAction: permanently deletes every selected row.</summary>
public static class ForceDeleteBulkAction
{
    public static BulkAction Make(Func<BulkActionContext, Task> handle) =>
        new BulkAction("force-delete")
            .Label("Delete permanently")
            .Color("danger")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Deleted permanently", "danger");
}
