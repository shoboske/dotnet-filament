using Fila.Panels.Resources;

namespace Fila.Panels.Actions;

/// <summary>Filament's ReplicateAction, trimmed to its essentials: confirm, then insert a copy
/// of the record with a fresh primary key (Resource.ReplicateAsync). No schema — Filament's
/// version lets you edit the copy's fields inline before saving; this port keeps it to a plain
/// duplicate, which is what the general mechanism needs to demonstrate, not full feature
/// parity with Filament's own ReplicateAction.</summary>
public static class ReplicateAction
{
    public static Fila.Actions.Action Make(IResource resource) =>
        new Fila.Actions.Action("replicate")
            .Label("Replicate")
            .Icon("copy")
            .RequiresConfirmation()
            .ModalHeading($"Replicate {ResourceNaming.SingularLabel(resource.Slug)}")
            .ModalDescription("A copy of this record will be created.")
            .Handle(ctx => resource.ReplicateAsync(ctx.Db, ctx.Record!, ctx.Ct))
            .Notifies("Replicated", "success");
}
