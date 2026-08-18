using Fila.Forms;

namespace Fila.Actions;

/// <summary>Filament's EditAction, reproduced exactly: same modal markup, same htmx swap
/// targets, same "Saved" toast — as a plain instance of the general <see cref="Action"/> type.
/// See CreateAction's doc comment for why this takes delegates instead of a Resource
/// reference.</summary>
public static class EditAction
{
    public static Action Make(string modalHeading, Func<IForm> schema, Func<ActionContext, Task> handle) =>
        new Action("edit")
            .Label("Edit")
            .Icon("pencil")
            .ModalHeading(modalHeading)
            .Schema(schema)
            .Handle(handle)
            .Notifies("Saved", "success");
}
