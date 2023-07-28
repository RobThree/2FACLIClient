using System.Text.Json.Serialization;

namespace TwoFA.Models;

internal record TwoFAFolder
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    [JsonPropertyName("isOpened")]
    public required bool IsOpened { get; init; }
}