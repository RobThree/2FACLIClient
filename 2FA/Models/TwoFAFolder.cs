using System.Text.Json.Serialization;

namespace TwoFA.Models;

internal record TwoFAFolder
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
    [JsonPropertyName("name")]
    public string? Name { get; init; }
    [JsonPropertyName("isOpened")]
    public bool IsOpened { get; init; } = false;
}