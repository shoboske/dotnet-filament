using Fila.Support;

namespace Fila.Actions;

/// <summary>An action that runs once against every selected row instead of one record —
/// Filament's packages/actions/src/BulkAction.php. Executes over hx-confirm rather than Fila's
/// modal (no mount/GET step) — a bulk action either has enough at stake to want the browser's
/// native confirm, or it doesn't need confirmation at all; the modal machinery every
/// single-record Action gets isn't worth the extra round trip for a selection that already has
/// its own "N selected" affordance in the table toolbar.</summary>
public sealed class BulkAction
{
    public BulkAction(string name)
    {
        Name = name;
        LabelValue = ComponentText.Humanize(name);
    }

    public string Name { get; }

    private Evaluated<string> LabelValue { get; set; }

    public string ColorValue { get; private set; } = "primary";

    public bool RequiresConfirmationFlag { get; private set; }

    public Func<BulkActionContext, Task>? HandleCallback { get; private set; }

    public (string Title, string Color)? Notification { get; private set; }

    public string ResolveLabel(EvaluationContext context) => LabelValue.Resolve(context) ?? Name;

    public BulkAction Label(string label)
    {
        LabelValue = label;
        return this;
    }

    public BulkAction Color(string color)
    {
        ColorValue = color;
        return this;
    }

    public BulkAction RequiresConfirmation(bool value = true)
    {
        RequiresConfirmationFlag = value;
        return this;
    }

    public BulkAction Handle(Func<BulkActionContext, Task> handle)
    {
        HandleCallback = handle;
        return this;
    }

    public BulkAction Notifies(string title, string color = "success")
    {
        Notification = (title, color);
        return this;
    }
}
