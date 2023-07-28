using System.Text.Json.Serialization;
using TwoFA.Converters;

namespace TwoFA.Models;

internal record TwoFAAccount
{
    [JsonPropertyName("algorithm")]
    [JsonConverter(typeof(AlgorithmConverter))]
    public Algorithm Algorithm { get; init; } = Algorithm.SHA1;
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
    public int Digits { get; init; } = 6;
    [JsonPropertyName("issuerName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public required string IssuerName { get; set; }
    [JsonPropertyName("userName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public required string UserName { get; set; }
    [JsonPropertyName("timeStep")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan TimeStep { get; init; } = TimeSpan.FromSeconds(30);
    [JsonPropertyName("creationTimestamp")]
    [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
    public required DateTimeOffset CreationDate { get; init; }
    [JsonPropertyName("lmiUserId")]
    public string? LmiUserId { get; init; }
    [JsonPropertyName("pushNotification")]
    public bool PushNotification { get; init; } = false;
    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; init; } = false;
    [JsonPropertyName("folderData")]
    public TwoFAFolderData? FolderData { get; init; }
}