using System.Text.Json.Serialization;

namespace TwoFA.Models;

internal record TwoFAFolderData
{
    [JsonPropertyName("folderId")]
    public required int FolderId { get; init; }
    [JsonPropertyName("position")]
    public int Position { get; init; } = 0;
}