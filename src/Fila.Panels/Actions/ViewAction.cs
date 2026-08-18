using Fila.Panels.Resources;

namespace Fila.Panels.Actions;

/// <summary>Filament's ViewAction normally opens an infolist; Fila.Infolists doesn't exist yet
/// (it's a later phase), so this reuses the resource's own form schema in read-only mode
/// instead — the fields render disabled and the mount has no working submit. No Handle: the
/// modal is mount-only, and htmx never has anything to POST to it.</summary>
public static class ViewAction
{
    public static Fila.Actions.Action Make(IResource resource) =>
        new Fila.Actions.Action("view")
            .Label("View")
            .Icon("eye")
            .Color("gray")
            .ModalHeading($"View {ResourceNaming.SingularLabel(resource.Slug)}")
            .ReadOnly()
            .Schema(() => resource.BuildForm()
                ?? throw new InvalidOperationException("ViewAction requires the resource to define a form."));
}
