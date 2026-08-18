namespace Fila.Schemas;

/// <summary>Marker for anything a schema can hold — a form field, an infolist entry, a
/// layout container. Deliberately empty: Phase 2 (#4), which replaces the ColumnKind and
/// FieldKind enums with an open component model, defines the real contract. It exists now
/// only so Forms, Infolists and Widgets already sit on top of a schemas package rather than
/// having to be re-homed once that phase lands.</summary>
public interface IComponent;
