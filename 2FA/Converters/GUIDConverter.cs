using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwoFA.Converters;

internal class GUIDConverter : JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Guid.TryParse(reader.GetString(), out var result) ? result : Guid.Empty;
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
        => value.ToString("D");
}
