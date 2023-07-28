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
    public string? OriginalUserName { get; set; }
    [JsonPropertyName("accountID")]
    [JsonConverter(typeof(GUIDJsonConverter))]
    public Guid AccountID { get; set; } = Guid.Empty;
    [JsonPropertyName("secret")]
    public required string Secret { get; init; }
    [JsonPropertyName("originalIssuerName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public string? OriginalIssuerName { get; set; }
    [JsonPropertyName("digits")]
    public int Digits { get; init; } = 6;
    [JsonPropertyName("issuerName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public string? IssuerName { get; set; }
    [JsonPropertyName("userName")]
    [JsonConverter(typeof(UrlEncodedStringConverter))]
    public string? UserName { get; set; }
    [JsonPropertyName("timeStep")]
    [JsonConverter(typeof(TimeSpanJsonConverter))]
    public TimeSpan TimeStep { get; init; } = TimeSpan.FromSeconds(30);
    [JsonPropertyName("creationTimestamp")]
    [JsonConverter(typeof(DateTimeOffsetJsonConverter))]
    public DateTimeOffset CreationDate { get; init; } = DateTimeOffset.UnixEpoch;
    [JsonPropertyName("lmiUserId")]
    public string? LmiUserId { get; init; }
    [JsonPropertyName("pushNotification")]
    public bool PushNotification { get; init; } = false;
    [JsonPropertyName("isFavorite")]
    public bool IsFavorite { get; init; } = false;
    [JsonPropertyName("folderData")]
    public TwoFAFolderData? FolderData { get; init; }
}