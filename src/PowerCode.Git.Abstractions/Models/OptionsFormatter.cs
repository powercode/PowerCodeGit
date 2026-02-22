using System.Linq;

namespace PowerCode.Git.Abstractions.Models;

/// <summary>
/// Helper for consistent <see cref="object.ToString"/> on options DTOs.
/// </summary>
internal static class OptionsFormatter
{
    public static string Format(string typeName, params (string Name, object? Value)[] properties)
    {
        var parts = properties
            .Where(p => p.Value is not null && !(p.Value is string s && string.IsNullOrEmpty(s)))
            .Select(p => $"{p.Name}={p.Value}");
        var joined = string.Join(", ", parts);
        return joined.Length > 0
            ? $"{typeName}({joined})"
            : $"{typeName}()";
    }
}
