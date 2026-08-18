using Fila.Forms;

namespace Fila.Actions;

/// <summary>Filament's ViewAction normally opens an infolist; Fila.Infolists doesn't exist yet
/// (a later phase), so this reuses the same schema an EditAction would mount, in read-only mode
/// — the fields render disabled and there's no working submit. No Handle: the mount is the
/// whole action. See CreateAction's doc comment for why this takes a delegate instead of a
/// Resource reference.</summary>
public static class ViewAction
{
    public static Action Make(string modalHeading, Func<IForm> schema) =>
        new Action("view")
            .Label("View")
            .Icon("eye")
            .Color("gray")
            .ModalHeading(modalHeading)
            .ReadOnly()
            .Schema(schema);
}
