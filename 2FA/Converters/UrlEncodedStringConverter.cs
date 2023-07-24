using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;

namespace TwoFA.Converters;

internal class UrlEncodedStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null ? null : HttpUtility.UrlDecode(value).Trim();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        => writer.WriteStringValue(value is null ? null : HttpUtility.UrlEncode(value.Trim()));
}
