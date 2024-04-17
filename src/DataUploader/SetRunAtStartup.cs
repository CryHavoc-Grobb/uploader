using System.Reflection;
using Microsoft.Win32;

namespace DataUploader;

public class SetRunAtStartup
{
    public static void RunAtStartup(bool runAtStartup)
    {
        const string appName = "GbankUploader";
        var appPath = Assembly.GetExecutingAssembly().Location;

        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", 
            true);

        if (key is null)
            return;

        if (runAtStartup)
        {
            key.SetValue(appName, $"\"{appPath}\"");
        }
        else
        {
            if (key.GetValue(appName) is not null)
            {
                key.DeleteValue(appName);
            }
        }
    }
}