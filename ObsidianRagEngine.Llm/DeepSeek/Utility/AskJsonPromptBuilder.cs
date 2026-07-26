using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ObsidianRagEngine.Llm.DeepSeek.Utility;

internal static class AskJsonPromptBuilder
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string BuildSystemPrompt<T>()
    {
        var exampleJson = BuildExampleJson(typeof(T));
        return $"""
            User asks a question.
            Find the correct answer.
            Reply only in the JSON format specified below.

            EXAMPLE JSON OUTPUT:
            {exampleJson}
            """;
    }

    public const string ClarificationPrompt =
        "Think carefully and thoughtfully. Return a valid non-empty JSON object that matches the example format.";

    private static string BuildExampleJson(Type type)
    {
        return BuildNode(type).ToJsonString(JsonOptions);
    }

    private static JsonNode BuildNode(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
            return JsonValue.Create("string")!;
        if (type == typeof(bool))
            return JsonValue.Create(true);
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return JsonValue.Create(0);
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return JsonValue.Create(0);
        if (type == typeof(Guid))
            return JsonValue.Create(Guid.Empty.ToString());
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return JsonValue.Create("2020-01-01T00:00:00Z");

        if (type.IsEnum)
            return JsonValue.Create(Enum.GetNames(type).FirstOrDefault() ?? type.Name)!;

        if (type.IsArray)
            return new JsonArray(BuildNode(type.GetElementType()!));

        if (type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(List<>) ||
             type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ||
             type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            return new JsonArray(BuildNode(type.GetGenericArguments()[0]));
        }

        var obj = new JsonObject();
        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            var name = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            obj[name] = BuildNode(property.PropertyType);
        }

        return obj;
    }
}
