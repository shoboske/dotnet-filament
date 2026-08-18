using Fila.Panels.Resources;

namespace Fila.Panels.Actions;

/// <summary>Filament's CreateAction, reproduced exactly: same modal markup, same htmx swap
/// targets, same "Created" toast — but as an instance of the general <see
/// cref="Fila.Actions.Action"/> type rather than a bespoke MapGet/MapPost pair. A header action
/// (its record is a fresh blank instance, not one resolved by id), so it mounts at
/// <c>/{panel}/{resource}/actions/create</c> rather than under a row's <c>{id}</c>.</summary>
public static class CreateAction
{
    public static Fila.Actions.Action Make(IResource resource) =>
        new Fila.Actions.Action("create")
            .Label("New")
            .Icon("plus")
            .ModalHeading($"Create {resource.Slug.TrimEnd('s').Replace('-', ' ')}")
            .NewRecord()
            .Schema(() => resource.BuildForm()
                ?? throw new InvalidOperationException("CreateAction requires the resource to define a form."))
            .Handle(ctx => resource.SaveAsync(ctx.Db, ctx.Record!, isNew: true, ctx.Ct))
            .Notifies("Created", "success");
}
