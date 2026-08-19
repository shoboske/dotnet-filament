using Fila.Forms;
using Fila.Notifications;
using Fila.Support;

namespace Fila.Actions;

/// <summary>Where an action's record comes from when it is mounted. <see cref="Existing"/>
/// (the default) resolves it by id from the route — what every row action needs. <see
/// cref="New"/> asks the resource for a blank instance instead — what a header action like
/// CreateAction needs, since there is no id yet.</summary>
public enum ActionRecordSource
{
    Existing,
    New,
}

/// <summary>Something that resolves to one or more <see cref="Action"/>s when a row's actions
/// are rendered — implemented by <see cref="Action"/> itself (resolves to itself) and by
/// <see cref="ActionGroup"/> (resolves to the actions it groups), so table code can treat a
/// flat action and a grouped dropdown of actions the same way when it needs to walk them all
/// (e.g. to find one by name for routing).</summary>
public interface IRowAction
{
    IReadOnlyList<Action> Flatten();
}

/// <summary>Fila's single reusable concept for a button, modal, or row/header operation — the
/// generalization of what used to be bespoke MapGet/MapPost handlers per CRUD verb in
/// FilaExtensions. Mirrors Filament's packages/actions/src/Action.php, trimmed to what this
/// port needs: a name, label, icon/color, an optional confirmation step, an optional form
/// schema it mounts, and a Handle callback that does the actual work.
///
/// Deliberately non-generic and resource-agnostic — it knows nothing about IResource or
/// DbContext beyond what <see cref="ActionContext"/> hands it. The built-in CreateAction,
/// EditAction, DeleteAction etc. live in Fila.Panels instead of here, since they close over an
/// IResource to know how to save/delete/replicate — keeping this package free of that
/// dependency is what lets a resource author reference Fila.Actions.Action directly for a
/// wholly custom action (see samples/Demo's "Mark shipped") without pulling in panel plumbing.</summary>
public sealed class Action : IRowAction
{
    public Action(string name)
    {
        Name = name;
        LabelValue = ComponentText.Humanize(name);
    }

    public string Name { get; }

    private Evaluated<string> LabelValue { get; set; }

    private Evaluated<bool> VisibleValue { get; set; } = true;

    /// <summary>Icon name, resolved the same way as Resource.NavigationIcon — via
    /// Fila.Support.IconRegistry. Null renders no icon.</summary>
    public string? IconName { get; private set; }

    /// <summary>Tone name (primary | danger | success | ...) — drives which CSS modifier class
    /// the action's button/icon-button renders with.</summary>
    public string ColorValue { get; private set; } = "primary";

    public bool RequiresConfirmationFlag { get; private set; }

    private Evaluated<string>? ModalHeadingValue;
    private Evaluated<string>? ModalDescriptionValue;

    /// <summary>Builds the form this action mounts, or null for an action with no form (a
    /// confirmation-only action like Delete or Mark shipped). A factory rather than a built
    /// IForm so it's rebuilt fresh per request — the same pattern Resource.BuildForm() already
    /// follows.</summary>
    public Func<IForm>? SchemaFactory { get; private set; }

    /// <summary>True for an action whose schema is shown but never submitted — ViewAction's
    /// fields render disabled and the mount has no working submit button.</summary>
    public bool ReadOnlyFlag { get; private set; }

    public ActionRecordSource RecordSource { get; private set; } = ActionRecordSource.Existing;

    public Func<ActionContext, Task>? HandleCallback { get; private set; }

    /// <summary>The notification sent once Handle completes successfully, or null to send none.
    /// Built through Fila.Notifications' fluent builder, so a resource author configuring a
    /// custom action reaches for the same API Fila's own built-ins use.</summary>
    public Notification? Notification { get; private set; }

    public string ResolveLabel(EvaluationContext context) => LabelValue.Resolve(context) ?? Name;

    public string ResolveModalHeading(EvaluationContext context) =>
        (ModalHeadingValue ?? LabelValue).Resolve(context) ?? ResolveLabel(context);

    public string? ResolveModalDescription(EvaluationContext context) => ModalDescriptionValue?.Resolve(context);

    /// <summary>False hides this action for the given record — e.g. a "Mark shipped" row
    /// action hiding itself once the order already has that status.</summary>
    public bool ResolveVisible(EvaluationContext context) => VisibleValue.Resolve(context);

    public Action Label(string label)
    {
        LabelValue = label;
        return this;
    }

    public Action Icon(string icon)
    {
        IconName = icon;
        return this;
    }

    public Action Color(string color)
    {
        ColorValue = color;
        return this;
    }

    public Action RequiresConfirmation(bool value = true)
    {
        RequiresConfirmationFlag = value;
        return this;
    }

    public Action ModalHeading(string heading)
    {
        ModalHeadingValue = heading;
        return this;
    }

    public Action ModalDescription(string description)
    {
        ModalDescriptionValue = description;
        return this;
    }

    public Action Schema(Func<IForm> factory)
    {
        SchemaFactory = factory;
        return this;
    }

    public Action ReadOnly(bool value = true)
    {
        ReadOnlyFlag = value;
        return this;
    }

    /// <summary>Marks this a header-style action whose record is a fresh blank instance rather
    /// than one resolved by id — what CreateAction needs.</summary>
    public Action NewRecord()
    {
        RecordSource = ActionRecordSource.New;
        return this;
    }

    public Action Handle(Func<ActionContext, Task> handle)
    {
        HandleCallback = handle;
        return this;
    }

    /// <summary>Shorthand for the common case. Equivalent to
    /// <c>Notify(Notification.Make().Title(title).Color(color))</c>.</summary>
    public Action Notifies(string title, string color = "success") =>
        Notify(Fila.Notifications.Notification.Make().Title(title).Color(color));

    /// <summary>Sends a notification built however the caller likes — the escape hatch from
    /// <see cref="Notifies"/>'s two positional strings.</summary>
    public Action Notify(Notification notification)
    {
        Notification = notification;
        return this;
    }

    public Action Visible(bool value = true)
    {
        VisibleValue = value;
        return this;
    }

    public Action Visible(Func<EvaluationContext, bool> value)
    {
        VisibleValue = Evaluated<bool>.From(value);
        return this;
    }

    public Action Hidden(bool value = true) => Visible(!value);

    public Action Hidden(Func<EvaluationContext, bool> value) => Visible(context => !value(context));

    IReadOnlyList<Action> IRowAction.Flatten() => [this];
}
