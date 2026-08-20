namespace Fila.Actions;

/// <summary>Filament's RestoreAction: un-does a soft delete. Only meaningful for a resource
/// whose entity implements Fila.Support.ISoftDeletable — see Resource&lt;TEntity&gt;.RestoreAction()
/// in Fila.Panels, which is what guards that and supplies the delegate. See CreateAction's doc
/// comment for why this takes a delegate instead of a Resource reference.</summary>
public static class RestoreAction
{
    public static Action Make(string modalHeading, Func<ActionContext, Task> handle) =>
        new Action("restore")
            .Label("Restore")
            .Icon("restore")
            .Color("gray")
            .ModalHeading(modalHeading)
            .RequiresConfirmation()
            .Handle(handle)
            .Notifies("Restored", "success");
}
