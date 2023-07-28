using System.Text.Json;
using System.Text.Json.Serialization;
using TwoFA.ResourceFiles;

namespace TwoFA.Converters;
internal class AlgorithmConverter : JsonConverter<Algorithm>
{
    public override Algorithm Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var algoritm = reader.GetString();
        return Enum.TryParse<Algorithm>(algoritm, out var result) ? result : throw new NotSupportedException(string.Format(Translations.EX_HASHALGORITHM_NOT_SUPPORTED, algoritm));
    }
    public override void Write(Utf8JsonWriter writer, Algorithm value, JsonSerializerOptions options)
        => writer.WriteStringValue(Enum.GetName(value));
}