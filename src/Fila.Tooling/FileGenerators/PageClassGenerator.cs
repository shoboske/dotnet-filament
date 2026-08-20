namespace Fila.Tooling.FileGenerators;

/// <summary>Emits a Fila.Panels.Pages.Page subclass plus the Razor view it names — backs
/// `make:page`. The only generator here producing two files instead of one: a page is a route
/// and a view together, and unlike make:resource's Table()/Form() (config methods on a class
/// Fila already renders for you), a page's whole point is that it renders itself.</summary>
public static class PageClassGenerator
{
    public sealed record GeneratedFile(string ClassSource, string ViewSource, string ViewRelativePath);

    public static GeneratedFile Generate(string pageName, string rootNamespace)
    {
        var viewRelativePath = $"Views/Fila/Pages/_{pageName}.cshtml";
        var viewPath = $"~/{viewRelativePath}";

        var classSource = $$"""
            using Fila.Panels.Pages;

            namespace {{rootNamespace}}.Fila.Pages;

            public sealed class {{pageName}}Page : Page
            {
                public override string ViewPath => "{{viewPath}}";
            }

            """;

        var viewSource = $$"""
            @using Fila.Panels.Rendering
            @addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
            @model FilaPageViewModel
            @{
                // No _ViewStart.cshtml outside Fila.Panels' own Views folder defaults this —
                // set explicitly here rather than asking every app to add one.
                Layout = "_FilaLayout";
                ViewBag.Title = Model.Panel.Brand;
                ViewBag.PanelForLayout = Model.Panel;
            }
            <div class="fi-shell">

                <partial name="Fila/_Sidebar" model="new FilaSidebarModel(Model.Panel, Model.Page.Slug)" />

                <div class="fi-main">
                    <div class="fi-page-header-ctn">
                        <header class="fi-header">
                            <h1 class="fi-header-heading">@Model.Page.Title</h1>
                        </header>
                    </div>

                    <div class="fi-ta-card">
                        <p>Scaffolded by <code>make:page {{pageName}}</code> — replace this with the page's content.
                           Reach for Fila.Actions/htmx directly here the same way any other Fila view does.</p>
                    </div>
                </div>
            </div>

            """;

        return new GeneratedFile(classSource, viewSource, viewRelativePath);
    }
}
