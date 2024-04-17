using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Microsoft.Toolkit.Uwp.Notifications;
using Prism.Mvvm;
using Refit;

namespace DataUploader;

public class UploaderViewModel : BindableBase, IDisposable
{
    private string _wowFolderPath = string.Empty;
    private bool _initialized;

    private bool _canRun;

    public bool CanRun
    {
        get => _canRun;
        set => SetProperty(ref _canRun, value);
    }

    public string WowFolderPath
    {
        get => _wowFolderPath;
        set
        {
            SetProperty(ref _wowFolderPath, value);
            OnPathChanged();
        }
    }

    private bool _notifyOnUpload;

    public bool NotifyOnUpload
    {
        get => _notifyOnUpload;
        set
        {
            SetProperty(ref _notifyOnUpload, value);
            SaveSettings();
        }
    }

    private bool _startWithWindows;
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            SetProperty(ref _startWithWindows, value);
            OnStartWithWindowsChanged(value);
        }
    }

    private readonly FileMonitorService? _fileMonitorService = new();
    private UserSettings _settings = default!;

    public UploaderViewModel()
    {
        LoadSettings();
        AutoDetectBaseFolder();
        
        var matcher = new Matcher();
        matcher.AddInclude("**/CryHavocBank.lua");

        var directoryPath = new DirectoryInfo(WowFolderPath);
        var file = matcher.Execute(new DirectoryInfoWrapper(directoryPath));
        
        if (!file.Files.Any())
            return;
        
        // at this point we should have files to watch and an api key
        CanRun = Environment.GetEnvironmentVariable("CH_API_KEY") is not null;
        
        _fileMonitorService!.BankDataUpdated += FileUpdatedHandler;
        StartFileWatcher();
        _initialized = true;
    }

    private void LoadSettings()
    {
        _settings = UserSettingsManager.LoadSettings();
        WowFolderPath = _settings.GameRoot ?? string.Empty;
        NotifyOnUpload = _settings.NotifyOnUpload;
        StartWithWindows = _settings.StartWithWindows;
    }

    private void OnPathChanged()
    {
        SaveSettings();   
        StartFileWatcher();
    }

    private void SaveSettings()
    {
        if (!_initialized)
            return;
        
        _settings.GameRoot = WowFolderPath;
        _settings.StartWithWindows = StartWithWindows;
        _settings.NotifyOnUpload = NotifyOnUpload;
        UserSettingsManager.SaveSettings(_settings);
    }
    
    private void StartFileWatcher()
    {
        if (!CanRun)
            return;
        
        _fileMonitorService!.StartFileWatcher(WowFolderPath);
    }

    private void OnStartWithWindowsChanged(bool startWithWindows)
    {
        SetRunAtStartup.RunAtStartup(startWithWindows);
        SaveSettings();
    }

    private async void FileUpdatedHandler(object? sender, BankDataUpdatedEventArgs e)
    {
        var request = new UpdateGuildBankRequest { Items = e.Items.ToList() };
        var client = RestService.For<IGuildApi>("https://guild-api-production.up.railway.app");
        await client.UpdateGuildBankAsync(
            request, 
            Environment.GetEnvironmentVariable("CH_API_KEY")!);
        
        Console.WriteLine("Updated guild bank!");
        
        if(NotifyOnUpload)
            Notify();
    }

    private void AutoDetectBaseFolder()
    {
        if (_settings.GameRoot is not null)
        {
            WowFolderPath = _settings.GameRoot;
            return;
        }

        WowFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
    
    private static void Notify()
    {
        new ToastContentBuilder()
            .AddArgument("action", "viewConversation")
            .AddArgument("conversationId", 9813)
            .AddText("Guild bank updated!")
            .AddText("The guild bank has been updated with your latest data pulled from the addon.")            
            .Show();
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
        
        _fileMonitorService?.Dispose();
    }
}