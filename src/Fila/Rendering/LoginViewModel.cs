using Fila.Panels;

namespace Fila.Rendering;

public sealed class LoginViewModel
{
    public required Panel Panel { get; init; }
    public required bool Error { get; init; }
}
