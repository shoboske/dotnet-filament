namespace Fila.Support;

/// <summary>A component setting that is either a constant or computed from the
/// <see cref="EvaluationContext"/> at the moment it is needed. Components store their settings
/// as these rather than as plain values, so making any one of them dynamic is a matter of
/// passing a closure instead of a value — no per-setting plumbing.
///
/// Stands in for Filament's <c>T | Closure</c> union property plus <c>evaluate()</c>, which C#
/// has no direct equivalent for.</summary>
public readonly struct Evaluated<TValue>
{
    private readonly TValue? _constant;
    private readonly Func<EvaluationContext, TValue>? _factory;

    private Evaluated(TValue? constant, Func<EvaluationContext, TValue>? factory)
    {
        _constant = constant;
        _factory = factory;
    }

    public static Evaluated<TValue> Constant(TValue value) => new(value, null);

    public static Evaluated<TValue> From(Func<EvaluationContext, TValue> factory) =>
        new(default, factory ?? throw new ArgumentNullException(nameof(factory)));

    /// <summary>True when the value is a closure and therefore cannot be read without a
    /// context.</summary>
    public bool IsDynamic => _factory is not null;

    public TValue Resolve(EvaluationContext context) =>
        _factory is null ? _constant! : _factory(context);

    public static implicit operator Evaluated<TValue>(TValue value) => Constant(value);

    public static implicit operator Evaluated<TValue>(Func<EvaluationContext, TValue> factory) =>
        From(factory);
}
