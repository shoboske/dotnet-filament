namespace DotFilament.Core;

public interface IResource<T>
{
    string Label { get; }
    FormBuilder Form();
    TableBuilder Table();
}
