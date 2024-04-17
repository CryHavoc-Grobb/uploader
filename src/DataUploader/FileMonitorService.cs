using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Refit;

namespace DataUploader;

public partial class FileMonitorService : IDisposable
{
    private FileSystemWatcher? _fileWatcher;
    private Timer? _debounceTimer;
    private readonly object _debounceLock = new();
    
    public void StartFileWatcher(string directoryPath)
    {
        _fileWatcher?.Dispose();
        _fileWatcher = new FileSystemWatcher(directoryPath)
        {
            NotifyFilter = NotifyFilters.LastWrite
        };

        _fileWatcher.Changed += OnFileChanged;
        _fileWatcher.EnableRaisingEvents = true;
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        Debounce(e);
    }

    private void Debounce(FileSystemEventArgs e)
    {
        lock (_debounceLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(FileChanged, e, 500, Timeout.Infinite);
        }
    }

    private void FileChanged(object? state)
    {
        if (state is not FileSystemEventArgs e)
            return;
        
        const string filename = "CryHavocBank.lua";
        if (!e.FullPath.EndsWith(filename))
            return;
        
        // break this up
        var variableValue = string.Empty;
        foreach (var line in File.ReadLines(e.FullPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Trim().Split('=', StringSplitOptions.TrimEntries);
            if (parts is not ["GuildBankExportData", var value])
                continue;



            variableValue = value;
            break;
        }

        if (string.IsNullOrWhiteSpace(variableValue))
            return;

        var unescaped = JsonSerializer.Deserialize<string>(variableValue);
        var parsed = JsonSerializer.Deserialize<List<List<JsonValue>>>(unescaped!)!;

        var parsedItems = parsed
            .Select(block => new GuildBankItem
            {
                Id = int.Parse(block[2].GetValue<string>()),
                Name = block[0].GetValue<string>(),
                Count = block[1].GetValue<int>()
            }).ToList();

        if (parsedItems.Count > 0)
            OnBankDataUpdated(new BankDataUpdatedEventArgs(parsedItems));
    }

    public void Dispose()
    {
        Disposing(true);
        GC.SuppressFinalize(this);
    }

    private void Disposing(bool disposing)
    {
        if (!disposing)
            return;
        
        _fileWatcher?.Dispose();
        _debounceTimer?.Dispose();
    }

    private class IntermediateContainer
    {
        [JsonPropertyName("values")] 
        public List<List<object>> Values { get; set; } = [];
    }

    private void OnBankDataUpdated(BankDataUpdatedEventArgs args)
    {
        BankDataUpdated?.Invoke(this, args);
    }

    internal event EventHandler<BankDataUpdatedEventArgs>? BankDataUpdated;
}

internal sealed class BankDataUpdatedEventArgs(IEnumerable<GuildBankItem> items) : EventArgs
{
    public IEnumerable<GuildBankItem> Items { get; } = items;
}