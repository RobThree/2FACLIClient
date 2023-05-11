using System.Text.Json.Serialization;
using TwoFA.Converters;

namespace TwoFA.Models;

internal record TwoFASecretsFile
{
    [JsonPropertyName("localDeviceId")]
    [JsonConverter(typeof(GUIDJsonConverter))]
    public required Guid LocalDeviceId { get; init; }
    [JsonPropertyName("deviceSecret")]
    public required string DeviceSecret { get; init; }
    [JsonPropertyName("deviceName")]
    public required string DeviceName { get; init; }
    [JsonPropertyName("accounts")]
    public required IEnumerable<TwoFAAccount> Accounts { get; init; } = Enumerable.Empty<TwoFAAccount>();
    [JsonPropertyName("version")]
    public required int Version { get; init; }
    [JsonPropertyName("deviceId")]
    [JsonConverter(typeof(GUIDJsonConverter))]
    public required Guid DeviceId { get; init; }
}
