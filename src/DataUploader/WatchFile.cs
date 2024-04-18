using System.Text.Json.Serialization;

namespace DataUploader;

public sealed class WatchFile
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;
}