using Fila.Forms;

namespace Fila.Actions;

/// <summary>Filament's CreateAction, reproduced exactly: same modal markup, same htmx swap
/// targets, same "Created" toast — as a plain instance of the general <see cref="Action"/>
/// type. Takes its schema and Handle as delegates rather than depending on a Resource type:
/// Filament's own CreateAction never references a resource either — it calls generic hook
/// methods on whatever Livewire component hosts it (handleRecordCreation(), fillForm(), etc.),
/// resolved dynamically at runtime, with the resource-specific implementation living in the
/// panels package's CreateRecord page. C# has no equivalent dynamic dispatch, so the same split
/// is expressed as delegates instead — Resource&lt;TEntity&gt;.CreateAction() in Fila.Panels
/// supplies them, playing the role Filament's CreateRecord page plays.</summary>
public static class CreateAction
{
    public static Action Make(string modalHeading, Func<IForm> schema, Func<ActionContext, Task> handle) =>
        new Action("create")
            .Label("New")
            .Icon("plus")
            .ModalHeading(modalHeading)
            .NewRecord()
            .Schema(schema)
            .Handle(handle)
            .Notifies("Created", "success");
}
