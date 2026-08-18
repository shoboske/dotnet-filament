namespace Fila.Actions;

/// <summary>Filament's DeleteAction, reproduced exactly: same confirmation modal, same "Deleted"
/// toast — as a plain instance of the general <see cref="Action"/> type. No schema — just a
/// confirmation step before Handle runs. See CreateAction's doc comment for why this takes a
/// delegate instead of a Resource reference.</summary>
public static class DeleteAction
{
    public static Action Make(string modalHeading, Func<ActionContext, Task> handle) =>
        new Action("delete")
            .Label("Delete")
            .Icon("trash")
            .Color("danger")
            .ModalHeading(modalHeading)
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Deleted", "danger");
}
