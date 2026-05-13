using System.Text.Json;

public static class JsonElementConverter
{
    public static Dictionary<string, object> ConvertJsonElements(
    Dictionary<string, object> dict)
    {
        var result = new Dictionary<string, object>();

        foreach (var kvp in dict)
        {
            result[kvp.Key] = ConvertValue(kvp.Value);
        }

        return result;
    }

    private static object ConvertValue(object value)
    {
        if (value is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString();

                case JsonValueKind.Number:
                    if (jsonElement.TryGetInt64(out long l))
                        return l;

                    return jsonElement.GetDouble();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return jsonElement.GetBoolean();

                case JsonValueKind.Object:
                    var obj = new Dictionary<string, object>();

                    foreach (var prop in jsonElement.EnumerateObject())
                    {
                        obj[prop.Name] = ConvertValue(prop.Value);
                    }

                    return obj;

                case JsonValueKind.Array:
                    var list = new List<object>();

                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        list.Add(ConvertValue(item));
                    }

                    return list;

                case JsonValueKind.Null:
                    return null!;
            }
        }

        return value;
    }

}