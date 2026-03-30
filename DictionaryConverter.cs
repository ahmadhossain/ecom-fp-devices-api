using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;

public class DictionaryConverter : IPropertyConverter
{
    // ✅ Called when SAVING to DynamoDB — converts Dictionary → DynamoDBEntry
    public DynamoDBEntry ToEntry(object value)
    {
        if (value is not Dictionary<string, object?> dict)
            return new DynamoDBNull();

        var document = new Document();
        foreach (var kvp in dict)
            document[kvp.Key] = ConvertToEntry(kvp.Value);

        return document;
    }

    // ✅ Called when READING from DynamoDB — converts DynamoDBEntry → Dictionary
    public object FromEntry(DynamoDBEntry entry)
    {
        return entry switch
        {
            Document document => DocumentToDictionary(document),
            DynamoDBList list => list.Entries.Select(e => FromEntry(e)).ToList(),
            Primitive prim => prim.Value,
            DynamoDBNull => null!,
            _ => entry.ToString()!,
        };
    }

    // ✅ Recursively converts DynamoDB Document → Dictionary<string, object?>
    private Dictionary<string, object?> DocumentToDictionary(Document document)
    {
        var dict = new Dictionary<string, object?>();

        foreach (var kvp in document)
        {
            dict[kvp.Key] = kvp.Value switch
            {
                Document nested => DocumentToDictionary(nested),
                DynamoDBList list => list.Entries.Select(e => FromEntry(e)).ToList(),
                Primitive prim => prim.Value,
                DynamoDBNull => null,
                _ => kvp.Value.ToString(),
            };
        }

        return dict;
    }

    // ✅ Recursively converts .NET object → DynamoDBEntry
    private DynamoDBEntry ConvertToEntry(object? value)
    {
        return value switch
        {
            null => new DynamoDBNull(),
            string s => new Primitive(s),
            bool b => new Primitive(b.ToString().ToLower()),
            int i => new Primitive(i.ToString(), true),
            long l => new Primitive(l.ToString(), true),
            float f => new Primitive(f.ToString(), true),
            double d => new Primitive(d.ToString(), true),
            decimal dec => new Primitive(dec.ToString(), true),
            Dictionary<string, object?> d => ToEntry(d),
            List<object?> list => new DynamoDBList(list.Select(ConvertToEntry)),
            IEnumerable<object?> enumerable => new DynamoDBList(enumerable.Select(ConvertToEntry)),
            _ => new Primitive(value.ToString()),
        };
    }
}
