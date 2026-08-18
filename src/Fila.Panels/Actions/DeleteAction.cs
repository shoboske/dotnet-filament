using Fila.Panels.Resources;

namespace Fila.Panels.Actions;

/// <summary>Filament's DeleteAction, reproduced exactly: same confirmation modal, same "Deleted"
/// toast — but as an instance of the general <see cref="Fila.Actions.Action"/> type. No schema
/// (there's nothing to fill in) — just a confirmation step before Handle runs.</summary>
public static class DeleteAction
{
    public static Fila.Actions.Action Make(IResource resource) =>
        new Fila.Actions.Action("delete")
            .Icon("trash")
            .Color("danger")
            .RequiresConfirmation()
            .Handle(ctx => resource.DeleteAsync(ctx.Db, ctx.Record!, ctx.Ct))
            .Notifies("Deleted", "danger");
}
