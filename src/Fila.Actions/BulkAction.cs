using Fila.Support;

namespace Fila.Actions;

/// <summary>An action that runs once against every selected row instead of one record —
/// Filament's packages/actions/src/BulkAction.php. A confirmation-requiring bulk action mounts
/// into Fila's shared modal exactly like a row Action does (see FilaExtensions'
/// HandleBulkActionMountAsync/_BulkActionConfirm.cshtml); the only difference from a row
/// action's confirm step is that it has no single record to key its POST off of — the modal's
/// own button submits whichever ids the table's checkboxes have checked via hx-include.</summary>
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

    public string? IconName { get; private set; }

    public bool RequiresConfirmationFlag { get; private set; }

    private Evaluated<string>? ModalHeadingValue;
    private Evaluated<string>? ModalSubmitLabelValue;

    public Func<BulkActionContext, Task>? HandleCallback { get; private set; }

    public (string Title, string Color)? Notification { get; private set; }

    public string ResolveLabel(EvaluationContext context) => LabelValue.Resolve(context) ?? Name;

    /// <summary>Falls back to the label ("Delete selected") when no modal heading was set — but
    /// the built-in bulk actions always set one, since Filament's own heading includes the
    /// resource's plural label ("Delete selected Orders") which the label alone can't carry.</summary>
    public string ResolveModalHeading(EvaluationContext context) =>
        (ModalHeadingValue ?? LabelValue).Resolve(context) ?? ResolveLabel(context);

    /// <summary>The confirm button's own text — shorter than the label on every built-in bulk
    /// action ("Delete" rather than "Delete selected"; the label already did the job of saying
    /// what's selected, once, in the toolbar trigger and the modal heading).</summary>
    public string ResolveModalSubmitLabel(EvaluationContext context) =>
        (ModalSubmitLabelValue ?? LabelValue).Resolve(context) ?? ResolveLabel(context);

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

    public BulkAction Icon(string icon)
    {
        IconName = icon;
        return this;
    }

    public BulkAction ModalHeading(string heading)
    {
        ModalHeadingValue = heading;
        return this;
    }

    public BulkAction ModalSubmitLabel(string label)
    {
        ModalSubmitLabelValue = label;
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
