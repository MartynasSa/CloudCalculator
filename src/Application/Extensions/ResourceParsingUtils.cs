namespace Application.Extensions;

public static class ResourceParsingUtils
{
    public static double? ParseMemory(string? memory)
    {
        if (string.IsNullOrWhiteSpace(memory))
        {
            return null;
        }

        var parts = memory.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0 && double.TryParse(parts[0], out var value))
        {
            return value;
        }

        return null;
    }
}
