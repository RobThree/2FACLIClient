using System.Text.Json.Serialization;
using TwoFA.Converters;

namespace TwoFA.Models;

internal record TwoFASecretsFile
{
    [JsonPropertyName("localDeviceId")]
    [JsonConverter(typeof(GUIDJsonConverter))]
    public Guid LocalDeviceId { get; init; } = Guid.Empty;
    [JsonPropertyName("deviceSecret")]
    public string? DeviceSecret { get; init; }
    [JsonPropertyName("deviceName")]
    public string? DeviceName { get; init; }
    [JsonPropertyName("accounts")]
    public IEnumerable<TwoFAAccount> Accounts { get; init; } = Enumerable.Empty<TwoFAAccount>();
    [JsonPropertyName("version")]
    public required int Version { get; init; }
    [JsonPropertyName("deviceId")]
    [JsonConverter(typeof(GUIDJsonConverter))]
    public Guid DeviceId { get; init; } = Guid.Empty;
    [JsonPropertyName("folders")]
    public IEnumerable<TwoFAFolder> Folders { get; init; } = Enumerable.Empty<TwoFAFolder>();
}