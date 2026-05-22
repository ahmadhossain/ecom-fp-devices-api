using System.Text.Json;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

public class DictionaryConverter : IPropertyConverter
{
    public DynamoDBEntry ToEntry(object value)
    {
        var dict =
            value as Dictionary<string, object?> ?? [];

        return ConvertDictionaryToDocument(dict);
    }

    public object FromEntry(DynamoDBEntry entry)
    {
        return ConvertFromEntry(entry);
    }

    // -----------------------------
    // TO DYNAMODB
    // -----------------------------

    private Document ConvertDictionaryToDocument(
        Dictionary<string, object?> dict)
    {
        var doc = new Document();

        foreach (var kv in dict)
        {
            doc[kv.Key] =
                ConvertToDynamoEntry(kv.Value);
        }

        return doc;
    }

    private DynamoDBEntry ConvertToDynamoEntry(
        object? value)
    {
        if (value == null)
            return new Primitive();

        // Handle JsonElement
        if (value is JsonElement json)
        {
            return ConvertJsonElement(json);
        }

        return value switch
        {
            string s => new Primitive(s),

            int i => new Primitive(i.ToString()),
            long l => new Primitive(l.ToString()),
            double d => new Primitive(d.ToString()),
            decimal m => new Primitive(m.ToString()),

            bool b => new DynamoDBBool(b),

            Dictionary<string, object?> dict =>
                ConvertDictionaryToDocument(dict),

            List<object?> list =>
                new DynamoDBList(
                    list.Select(ConvertToDynamoEntry)
                        .ToList()),

            _ => new Primitive(value.ToString())
        };
    }

    private DynamoDBEntry ConvertJsonElement(
        JsonElement json)
    {
        switch (json.ValueKind)
        {
            case JsonValueKind.Object:

                var doc = new Document();

                foreach (var prop in json.EnumerateObject())
                {
                    doc[prop.Name] =
                        ConvertJsonElement(prop.Value);
                }

                return doc;

            case JsonValueKind.Array:

                var list = new DynamoDBList();

                foreach (var item in json.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }

                return list;

            case JsonValueKind.String:
                return new Primitive(json.GetString());

            case JsonValueKind.Number:

                if (json.TryGetInt64(out var l))
                    return new Primitive(l.ToString());

                return new Primitive(
                    json.GetDouble().ToString());

            case JsonValueKind.True:
                return new DynamoDBBool(true);

            case JsonValueKind.False:
                return new DynamoDBBool(false);

            case JsonValueKind.Null:
                return new Primitive();

            default:
                return new Primitive(json.ToString());
        }
    }

    // -----------------------------
    // FROM DYNAMODB
    // -----------------------------

    private object? ConvertFromEntry(
        DynamoDBEntry entry)
    {
        if (entry is Document doc)
        {
            var dict =
                new Dictionary<string, object?>();

            foreach (var key in doc.Keys)
            {
                dict[key] =
                    ConvertFromEntry(doc[key]);
            }

            return dict;
        }

        if (entry is DynamoDBList list)
        {
            return list.Entries
                .Select(ConvertFromEntry)
                .ToList();
        }

        if (entry is DynamoDBBool boolValue)
        {
            return boolValue.Value;
        }

        if (entry is Primitive primitive)
        {
            var value = primitive.Value?.ToString();

            if (long.TryParse(value, out var l))
                return l;

            if (double.TryParse(value, out var d))
                return d;

            if (bool.TryParse(value, out var b))
                return b;

            return value;
        }

        return null;
    }
}