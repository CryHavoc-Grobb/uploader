using System.IO;
using Prism.Mvvm;

namespace DataUploader;

public class UploaderViewModel : BindableBase, IDisposable
{
    private string _wowFolderPath = string.Empty;

    public string WowFolderPath
    {
        get => _wowFolderPath;
        set
        {
            SetProperty(ref _wowFolderPath, value);
            StartFileWatcher();
        }
    }

    private bool _notifyOnUpload;

    public bool NotifyOnUpload
    {
        get => _notifyOnUpload;
        set => SetProperty(ref _notifyOnUpload, value);
    }

    private readonly FileMonitorService? _fileMonitorService = new();

    public UploaderViewModel()
    {
        //WowFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        WowFolderPath = Path.GetDirectoryName(@"C:\source\CryHavoc\uploader\src\TestData\CryHavocBank.lua")!;
        StartFileWatcher();
    }

    private void StartFileWatcher()
        => _fileMonitorService!.StartFileWatcher(WowFolderPath);

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