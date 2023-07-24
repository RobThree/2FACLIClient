using System.Text.Json.Serialization;
using TwoFA.Converters;

namespace TwoFA.Models;

internal record TwoFAAccount
{
    [JsonPropertyName("algorithm")]
    public required string Algorithm { get; init; }
    [JsonPropertyName("originalUserName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public required string OriginalUserName { get; set; }
    [JsonPropertyName("accountID")]
    [JsonConverter(typeof(GUIDJsonConverter))]
    public Guid AccountID { get; set; }
    [JsonPropertyName("secret")]
    public required string Secret { get; init; }
    [JsonPropertyName("originalIssuerName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public required string OriginalIssuerName { get; set; }
    [JsonPropertyName("digits")]
    public required int Digits { get; init; }
    [JsonPropertyName("issuerName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public required string IssuerName { get; set; }
    [JsonPropertyName("userName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public required string UserName { get; set; }
    [JsonPropertyName("timeStep")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public required TimeSpan TimeStep { get; init; } = TimeSpan.FromSeconds(30);
    [JsonPropertyName("creationTimestamp")]
    [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
    public required DateTimeOffset CreationDate { get; init; }
}