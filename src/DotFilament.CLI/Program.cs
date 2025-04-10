using System.Reflection;
using System.Text;
using DotFilament.Core;

var args = Environment.GetCommandLineArgs();

if (args.Length > 2 && args[1] == "generate" && args[2] == "resource")
{
    var resourceName = args[3];
    var modelType = Assembly.GetExecutingAssembly().GetType($"Models.{resourceName}");

    if (modelType == null)
    {
        Console.WriteLine($"Model {resourceName} not found.");
        return;
    }

    var properties = modelType.GetProperties();
    var formBuilder = new StringBuilder("new FormBuilder()");
    var tableBuilder = new StringBuilder("new TableBuilder()");

    foreach (var property in properties)
    {
        var formField = TypeToFieldMapper.MapToFormField(property.PropertyType, property.Name);
        if (formField != null)
        {
            formBuilder.AppendLine($"    {formField}");
        }

        var tableColumn = TypeToFieldMapper.MapToTableColumn(property.Name);
        tableBuilder.AppendLine($"    {tableColumn}");
    }

    var resourceContent = $@"
using DotFilament.Core;

public class {resourceName}Resource : IResource<{resourceName}>
{{
    public string Label => \"{resourceName}\";

    public FormBuilder Form() => {formBuilder};

    public TableBuilder Table() => {tableBuilder};
}}
";

    var outputPath = Path.Combine("GeneratedResources", $"{resourceName}Resource.cs");
    Directory.CreateDirectory("GeneratedResources");
    File.WriteAllText(outputPath, resourceContent);

    Console.WriteLine($"Resource {resourceName}Resource.cs generated at {outputPath}");
}
else
{
    Console.WriteLine("Invalid command. Usage: dotnet run -- generate resource <ResourceName>");
}