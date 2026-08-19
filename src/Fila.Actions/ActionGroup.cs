namespace Fila.Actions;

/// <summary>Groups several actions under one dropdown trigger instead of one button per action
/// — Filament's packages/actions/src/ActionGroup.php. A resource author reaches for this only
/// when a row has enough actions that separate icon buttons get crowded; nothing in Fila itself
/// wraps its built-in actions in one by default.</summary>
public sealed class ActionGroup : IRowAction
{
    public ActionGroup(params Action[] actions)
    {
        Actions = actions;
    }

    public IReadOnlyList<Action> Actions { get; }

    /// <summary>Icon for the dropdown trigger itself — defaults to a vertical ellipsis, matching
    /// Filament's default ActionGroup trigger.</summary>
    public string IconName { get; private set; } = "dots-vertical";

    public ActionGroup Icon(string icon)
    {
        IconName = icon;
        return this;
    }

    IReadOnlyList<Action> IRowAction.Flatten() => Actions;
}
