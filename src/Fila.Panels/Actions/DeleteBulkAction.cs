using Fila.Panels.Resources;

namespace Fila.Panels.Actions;

/// <summary>Filament's DeleteBulkAction: deletes every selected row. Runs over the table's
/// native hx-confirm rather than Fila's modal — see BulkAction's doc comment for why.</summary>
public static class DeleteBulkAction
{
    public static Fila.Actions.BulkAction Make(IResource resource) =>
        new Fila.Actions.BulkAction("delete")
            .Label("Delete selected")
            .Color("danger")
            .RequiresConfirmation()
            .Handle(async ctx =>
            {
                foreach (var record in ctx.Records)
                    await resource.DeleteAsync(ctx.Db, record, ctx.Ct);
            })
            .Notifies("Deleted", "danger");
}
