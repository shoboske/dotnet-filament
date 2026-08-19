namespace Fila.Widgets;

/// <summary>A widget named for later activation — Filament's Widgets\WidgetConfiguration, and
/// the C# stand-in for the <c>class-string&lt;Widget&gt;</c> its
/// <c>Resource::getWidgets()</c> and <c>Panel::widgets()</c> traffic in.
///
/// Filament registers widgets by class name rather than by instance, and constrains that name
/// to a Widget subclass with a <c>class-string&lt;Widget&gt;</c> generic — an annotation only
/// static analysis enforces. C# has no class-string, but it can do better than the docblock:
/// <see cref="Of{TWidget}"/>'s type constraint makes a non-widget registration fail to compile,
/// so the runtime "that isn't a widget" check this replaces has nothing left to catch.
///
/// Registering a type rather than an instance is what lets a widget take constructor
/// dependencies — the dashboard activates it out of the request scope. Filament's version also
/// carries a property bag for the Livewire component; Fila has no equivalent because DI covers
/// the same ground, so this holds only the type.</summary>
public sealed class WidgetRegistration
{
    private WidgetRegistration(Type widgetType) => WidgetType = widgetType;

    /// <summary>Registers <typeparamref name="TWidget"/>. The constraint is the whole point:
    /// <c>WidgetRegistration.Of&lt;NotAWidget&gt;()</c> is a compile error.</summary>
    public static WidgetRegistration Of<TWidget>() where TWidget : Widget => new(typeof(TWidget));

    /// <summary>The registered widget's type. Guaranteed to derive from <see cref="Widget"/>.</summary>
    public Type WidgetType { get; }
}
