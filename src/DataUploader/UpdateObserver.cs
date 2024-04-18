namespace DataUploader;

internal static class UpdateObserver
{
    public static event EventHandler<GbankUpdatedEventArgs>? GbankUpdated;

    public static void OnGbankUpdated(object sender, GbankUpdatedEventArgs e)
    {
        GbankUpdated?.Invoke(sender, e);
    }
}

internal sealed class GbankUpdatedEventArgs(DateTimeOffset updatedDate) : EventArgs
{
    public DateTimeOffset LastUpdated { get; set; } = updatedDate;
}