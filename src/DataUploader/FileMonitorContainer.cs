using System.IO;
using System.Security.Cryptography;
using ABI.Windows.System;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace DataUploader;

public sealed class FileMonitorContainer : IDisposable
{
    private readonly List<WatchFile> _filesToWatch = [];
    private static readonly MD5 Hasher = MD5.Create();
    
    public IReadOnlyCollection<WatchFile> WatchFiles => _filesToWatch;

    public static FileMonitorContainer? Instance { get; private set; }

    public static void Initialize(string rootPath)
    {
        if (Instance is not null)
            return;
        
        var instance = new FileMonitorContainer();
        
        var matcher = new Matcher();
        matcher.AddInclude("**/CryHavocBank.lua");
        var directoryPath = new DirectoryInfo(rootPath);
        var result = matcher.Execute(new DirectoryInfoWrapper(directoryPath));
        
        // load current state of the files
        foreach (var file in result.Files)
        {
            var fullPath = Path.Combine(rootPath, file.Path);
            instance._filesToWatch.Add(new WatchFile
            {
                Path = fullPath,
                Hash = GetChecksum(fullPath)
            });
        }

        // overwrite with the last known state of the files so we can detect any
        // changes while we were offline.
        instance.RestoreFromSettings();
        Instance = instance;
    }

    private void RestoreFromSettings()
    {
        var settings = UserSettingsManager.LoadSettings();
        foreach (var file in settings.WatchFiles)
        {
            var match = _filesToWatch.Find(
                f => f.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase));
            
            if(match is null)
                continue;

            match.Hash = file.Hash;
        }
    }

    public void Update(WatchFile file)
    {
        var toUpdate = _filesToWatch.Find(
            f => f.Path.Equals(file.Path, StringComparison.OrdinalIgnoreCase));

        if (toUpdate is null)
            return;
        
        toUpdate.Hash = GetChecksum(file.Path);
        UpdateSettings();
    }
    
    public static string GetChecksum(string path)
    {
        using var fileStream = File.OpenRead(path);
        var hash = Hasher.ComputeHash(fileStream);
        return BitConverter
            .ToString(hash)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    private void UpdateSettings()
    {
        var settings = UserSettingsManager.LoadSettings();
        settings.WatchFiles = _filesToWatch;
        UserSettingsManager.SaveSettings(settings);
    }

    private bool _disposed;
    public void Dispose()
    {
        if (_disposed)
            return;
        
        UpdateSettings();
        Hasher.Dispose();
        Instance = null!;
        _disposed = true;
        
        
    }
}