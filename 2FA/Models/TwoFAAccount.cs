using System.Text.Json.Serialization;
using TwoFA.Converters;

namespace TwoFA.Models;

internal record TwoFAAccount
{
    [JsonPropertyName("algorithm")]
    public required string Algorithm { get; init; }
    [JsonPropertyName("originalUserName")]
    public required string OriginalUserName { get; set; }
    [JsonPropertyName("accountID")]
    [JsonConverter(typeof(GUIDConverter))]
    public Guid AccountID { get; set; }
    [JsonPropertyName("secret")]
    public required string Secret { get; init; }
    [JsonPropertyName("originalIssuerName")]
    public required string OriginalIssuerName { get; set; }
    [JsonPropertyName("digits")]
    public required int Digits { get; init; }
    [JsonPropertyName("issuerName")]
    public required string IssuerName { get; set; }
    [JsonPropertyName("userName")]
    public required string UserName { get; set; }
    [JsonPropertyName("timeStep")]
    [JsonConverter(typeof(TimeSpanConverter))]
    public required TimeSpan TimeStep { get; init; } = TimeSpan.FromSeconds(30);
    [JsonPropertyName("creationTimestamp")]
    [JsonConverter(typeof(DateTimeOffsetConverter))]
    public required DateTimeOffset CreationDate { get; init; }
}