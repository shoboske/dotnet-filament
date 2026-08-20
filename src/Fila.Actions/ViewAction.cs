namespace Fila.Actions;

/// <summary>Filament's ViewAction opens an infolist. Fila.Actions doesn't depend on
/// Fila.Infolists (it depends only on Forms + Notifications — see this package's own
/// composer.json-equivalent), so this action carries no schema of its own the way
/// Create/EditAction do: Fila.Panels — which already depends on everything — resolves the
/// resource's infolist directly when it sees an action named "view", the same way it already
/// treats "edit" specially for relation-manager resources. No Handle either: the mount is the
/// whole action. See CreateAction's doc comment for why the built-ins take delegates instead of
/// a Resource reference.</summary>
public static class ViewAction
{
    public static Action Make(string modalHeading) =>
        new Action("view")
            .Label("View")
            .Icon("eye")
            .Color("gray")
            .ModalHeading(modalHeading);
}
