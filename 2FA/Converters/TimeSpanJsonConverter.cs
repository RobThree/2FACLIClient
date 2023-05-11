using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwoFA.Converters;

internal class TimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromSeconds(reader.GetInt32());
    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue((int)value.TotalSeconds);
}
