using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Toolkit.Uwp.Notifications;
using Quartz;
using Refit;

namespace DataUploader;

public class CheckFileJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        if (FileMonitorContainer.Instance is null)
        {
            throw new Exception($"{nameof(FileMonitorContainer)} was not properly initialized!");
        }
        
        var settings = UserSettingsManager.LoadSettings();
        
        // of the files we're watching, find the file that was updated most recently
        var latestUpdated = FileMonitorContainer.Instance.WatchFiles
            .Select(p => new FileInfo(p.Path))
            .OrderBy(i => i.LastWriteTime)
            .Last();
        
        // find the matching file that we were watching
        var record = FileMonitorContainer.Instance.WatchFiles
            .ToList()
            .Find(r => r.Path == latestUpdated.FullName);

        if (record is null)
            return;

        // if the file hasn't changed since last we updated, ignore.
        if (record.Hash == FileMonitorContainer.GetChecksum(latestUpdated.FullName))
            return;
        
        // process the changed file
        await Process(record.Path, settings.NotifyOnUpload);
        
        // update the file states
        FileMonitorContainer.Instance.Update(record);
    }

    private async Task Process(string filePath, bool notify)
    {
        var variableValue = string.Empty;
        foreach (var line in File.ReadLines(filePath))
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

        var fileInfo = new FileInfo(filePath);
        if (parsedItems.Count > 0)
        {
            await UpdateGbank(fileInfo.LastWriteTime, parsedItems);
            UpdateObserver.OnGbankUpdated(this, new GbankUpdatedEventArgs(DateTimeOffset.Now));
            if(notify)
                Notify();
        }
    }

    private static async Task UpdateGbank(DateTimeOffset fileDate, List<GuildBankItem> items)
    {
        var request = new UpdateGuildBankRequest { Items = items, FileDate = fileDate };
        var client = RestService.For<IGuildApi>("https://guild-api-production.up.railway.app");
        await client.UpdateGuildBankAsync(
            request, 
            Environment.GetEnvironmentVariable("CH_API_KEY")!);
        
        Console.WriteLine("Updated guild bank!");
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
}