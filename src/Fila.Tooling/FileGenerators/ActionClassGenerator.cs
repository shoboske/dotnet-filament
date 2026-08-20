using Fila.Panels.Resources;

namespace Fila.Tooling.FileGenerators;

/// <summary>Emits a custom Action — backs `make:action`. Fila.Actions.Action is never
/// subclassed, only instantiated fluently (see samples/Demo's MarkShippedAction), so this
/// generates a static field holding one rather than a class hierarchy — the shape every
/// resource-author-defined action in this codebase already takes.</summary>
public static class ActionClassGenerator
{
    public sealed record GeneratedFile(string Source);

    public static GeneratedFile Generate(string actionName, string rootNamespace)
    {
        var slug = ResourceNaming.ToKebabCase(actionName);

        var source = $$"""
            using Fila.Notifications;
            using Action = Fila.Actions.Action;

            namespace {{rootNamespace}}.Fila.Actions;

            // Add this to a resource's .Actions(...) or .BulkActions(...) call. Confirmation-only,
            // no schema — see samples/Demo's MarkShippedAction for one that also inspects/updates
            // the record it's given.
            public static class {{actionName}}Action
            {
                public static readonly Action Instance = new Action("{{slug}}")
                    .Label("{{actionName}}")
                    .RequiresConfirmation()
                    .Handle(async ctx =>
                    {
                        // TODO: implement {{actionName}}.
                        await ctx.Db.SaveChangesAsync(ctx.Ct);
                    })
                    .Notifies("{{actionName}} complete", "success");
            }

            """;

        return new GeneratedFile(source);
    }
}
