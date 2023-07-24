using System.Text.Json;
using System.Text.Json.Serialization;

namespace TwoFA.Converters;

internal class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    private static readonly long _maxseconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetInt64();
        // Apparently there are timestamps in seconds and in milliseconds format (see https://github.com/RobThree/2FACLIClient/issues/1)
        // This 'detects' which format is most likely in use and parses the value accordingly as milliseconds or seconds
        return (value > _maxseconds) ? DateTimeOffset.FromUnixTimeMilliseconds(value) : DateTimeOffset.FromUnixTimeSeconds(value);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
}
