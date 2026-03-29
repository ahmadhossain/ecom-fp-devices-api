using System.Text.Json;

public static class JsonElementConverter
{
    public static object? Convert(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => element
                .EnumerateObject()
                .ToDictionary(p => p.Name, p => Convert(p.Value)),

            JsonValueKind.Array => element.EnumerateArray().Select(Convert).ToList(),

            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText(),
        };
    }

    public static Dictionary<string, object?> ToDictionary(JsonElement root)
    {
        return root.EnumerateObject().ToDictionary(p => p.Name, p => Convert(p.Value));
    }
}
