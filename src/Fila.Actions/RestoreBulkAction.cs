namespace Fila.Actions;

/// <summary>Filament's RestoreBulkAction: un-does a soft delete for every selected row. Mounts
/// into Fila's shared modal for its confirmation step — see BulkAction's doc comment.</summary>
public static class RestoreBulkAction
{
    public static BulkAction Make(string modalHeading, Func<BulkActionContext, Task> handle) =>
        new BulkAction("restore")
            .Label("Restore selected")
            .Icon("restore")
            .Color("success")
            .ModalHeading(modalHeading)
            .ModalSubmitLabel("Restore")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Restored", "success");
}
