using System;
using System.Linq;

namespace DotFilament.Core;

public static class TypeToFieldMapper
{
    public static string MapToFormField(Type type, string propertyName)
    {
        if (propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
            propertyName.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
        {
            return null; // Skip these fields for forms
        }

        return type switch
        {
            { } t when t == typeof(string) => $"<input type='text' name='{propertyName}' class='form-control' />",
            { } t when t == typeof(int) || t == typeof(long) => $"<input type='number' name='{propertyName}' class='form-control' />",
            { } t when t == typeof(DateTime) => $"<input type='date' name='{propertyName}' class='form-control' />",
            { } t when t == typeof(bool) => $"<input type='checkbox' name='{propertyName}' class='form-check-input' />",
            { } t when t.IsEnum => $"<select name='{propertyName}' class='form-select'>{string.Join("", Enum.GetNames(t).Select(e => $"<option value='{e}'>{e}</option>"))}</select>",
            { } t when t == typeof(decimal) || t == typeof(double) => $"<input type='number' step='0.01' name='{propertyName}' class='form-control' />",
            _ => $"<input type='text' name='{propertyName}' class='form-control' />" // Fallback for unsupported types
        };
    }

    public static string MapToTableColumn(string propertyName)
    {
        return $"<th>{propertyName}</th>";
    }
}