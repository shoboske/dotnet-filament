namespace Fila.Actions;

/// <summary>Filament's ReplicateAction, trimmed to its essentials: confirm, then insert a copy
/// of the record. No schema — Filament's version lets you edit the copy's fields inline before
/// saving; this port keeps it to a plain duplicate. See CreateAction's doc comment for why this
/// takes a delegate instead of a Resource reference.</summary>
public static class ReplicateAction
{
    public static Action Make(string modalHeading, Func<ActionContext, Task> handle) =>
        new Action("replicate")
            .Label("Replicate")
            .Icon("copy")
            .ModalHeading(modalHeading)
            .ModalDescription("A copy of this record will be created.")
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Replicated", "success");
}
