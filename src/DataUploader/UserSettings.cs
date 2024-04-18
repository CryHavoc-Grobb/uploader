using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DateTimeOffset = ABI.System.DateTimeOffset;

namespace DataUploader;

internal sealed class UserSettings
{
    [JsonPropertyName("gameRoot")]
    public string? GameRoot { get; set; } = @"c:\Program Files (x86)\World of Warcraft\_classic_";

    [JsonPropertyName("notifyOnUpload")] 
    public bool NotifyOnUpload { get; set; } = true;

    [JsonPropertyName("startWithWindows")] 
    public bool StartWithWindows { get; set; }

    [JsonPropertyName("pollIntervalSeconds")]
    public int PollingIntervalSeconds { get; set; } = 1800;

    [JsonPropertyName("watchFiles")]
    public List<WatchFile> WatchFiles { get; set; } = [];
}

internal static class UserSettingsManager
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GbankUploader",
        "user_settings.json");
    
    public static UserSettings LoadSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(FilePath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            if (!File.Exists(FilePath))
            {
                var settings = new UserSettings();
                File.WriteAllText(FilePath, JsonSerializer.Serialize(settings), Encoding.UTF8);
            }

            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<UserSettings>(json)!;
        }
        catch (Exception ex)
        {
            return new UserSettings();
        }
    }

    public static void SaveSettings(UserSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath, json, Encoding.UTF8);
        }
        catch(Exception ex){}
    }
}