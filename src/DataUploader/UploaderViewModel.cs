using System.IO;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using Prism.Mvvm;
using Quartz;
using Quartz.Impl;

namespace DataUploader;

public class UploaderViewModel : BindableBase, IDisposable
{
    private string _wowFolderPath = string.Empty;
    private readonly bool _initialized;

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
            SaveSettings();
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

    private string _lastUpdatedText = "Last update: Hasn't run yet!";

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        set => SetProperty(ref _lastUpdatedText, value);
    }

    private UserSettings _settings = default!;
    private readonly IScheduler _scheduler = default!;

    public UploaderViewModel()
    {
        LoadSettings();
        AutoDetectBaseFolder();
        FileMonitorContainer.Initialize(WowFolderPath);
        
        var matcher = new Matcher();
        matcher.AddInclude("**/CryHavocBank.lua");

        var directoryPath = new DirectoryInfo(WowFolderPath);
        var file = matcher.Execute(new DirectoryInfoWrapper(directoryPath));
        
        if (!file.Files.Any())
            return;
        
        // at this point we should have files to watch and an api key
        CanRun = Environment.GetEnvironmentVariable("CH_API_KEY") is not null;
        
        _scheduler = StdSchedulerFactory.GetDefaultScheduler().Result;
        _scheduler.Start().Wait();
        
        var job = JobBuilder.Create<CheckFileJob>()
            .WithIdentity("PollFileJob")
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity("PollFileJobTrigger")
            .StartNow()
            .WithSimpleSchedule(s => s
                .WithIntervalInSeconds(_settings.PollingIntervalSeconds)
                .RepeatForever())
            .Build();

        UpdateObserver.GbankUpdated += (_, args) 
            => LastUpdatedText = $"Last update: {args.LastUpdated:MM/dd/yyyy hh\\:mm tt}";

        _ = _scheduler.ScheduleJob(job, trigger).Result;
        
        _initialized = true;
    }

    private void LoadSettings()
    {
        _settings = UserSettingsManager.LoadSettings();
        WowFolderPath = _settings.GameRoot ?? string.Empty;
        NotifyOnUpload = _settings.NotifyOnUpload;
        StartWithWindows = _settings.StartWithWindows;
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

    private void OnStartWithWindowsChanged(bool startWithWindows)
    {
        SetRunAtStartup.RunAtStartup(startWithWindows);
        SaveSettings();
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
    
    public void Dispose()
    {
        Disposing(true);
        GC.SuppressFinalize(this);
    }

    private void Disposing(bool disposing)
    {
        if (!disposing)
            return;

        _scheduler.Shutdown().Wait();
        FileMonitorContainer.Instance.Dispose();
    }
}