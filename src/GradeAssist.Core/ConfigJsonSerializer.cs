using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace GradeAssist.Core;

public static class ConfigJsonSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    static ConfigJsonSerializer()
    {
        Options.MakeReadOnly();
    }

    public static T Deserialize<T>(string json) where T : notnull
    {
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new JsonException($"Failed to deserialize {typeof(T).Name}: result was null.");
    }
}
