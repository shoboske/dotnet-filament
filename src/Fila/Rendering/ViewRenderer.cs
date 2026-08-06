using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Fila.Rendering;

/// <summary>Minimal APIs have no ViewResult, so this resolves ICompositeViewEngine +
/// ITempDataProvider and executes the view to a StringWriter by hand.</summary>
public sealed class ViewRenderer(ICompositeViewEngine viewEngine, ITempDataProvider tempDataProvider)
{
    public async Task<string> RenderAsync(HttpContext httpContext, string viewPath, object model)
    {
        var actionContext = new ActionContext(httpContext, httpContext.GetRouteData() ?? new RouteData(), new ActionDescriptor());

        var result = viewEngine.GetView(executingFilePath: null, viewPath: viewPath, isMainPage: true);
        if (!result.Success)
            result = viewEngine.FindView(actionContext, viewPath, isMainPage: true);

        if (!result.Success)
        {
            var locations = string.Join(Environment.NewLine, result.SearchedLocations ?? []);
            throw new InvalidOperationException(
                $"Could not find view '{viewPath}'. Searched locations:{Environment.NewLine}{locations}");
        }

        await using var writer = new StringWriter();

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = model,
        };

        var tempData = new TempDataDictionary(httpContext, tempDataProvider);
        var viewContext = new ViewContext(actionContext, result.View, viewData, tempData, writer, new HtmlHelperOptions());

        await result.View.RenderAsync(viewContext);

        return writer.ToString();
    }
}
