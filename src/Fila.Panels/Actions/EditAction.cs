using Fila.Panels.Resources;

namespace Fila.Panels.Actions;

/// <summary>Filament's EditAction, reproduced exactly: same modal markup, same htmx swap
/// targets, same "Saved" toast — but as an instance of the general <see
/// cref="Fila.Actions.Action"/> type. A row action: its record is resolved by the id in the
/// route, matching Resource.FindAsync.</summary>
public static class EditAction
{
    public static Fila.Actions.Action Make(IResource resource) =>
        new Fila.Actions.Action("edit")
            .Icon("pencil")
            .Schema(() => resource.BuildForm()
                ?? throw new InvalidOperationException("EditAction requires the resource to define a form."))
            .Handle(ctx => resource.SaveAsync(ctx.Db, ctx.Record!, isNew: false, ctx.Ct))
            .Notifies("Saved", "success");
}
