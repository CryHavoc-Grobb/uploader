using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace DataUploader;

public partial class FolderPicker : UserControl
{
    public static readonly DependencyProperty FolderPathProperty = DependencyProperty.Register(
        nameof(FolderPath),
        typeof(string),
        typeof(FolderPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public string FolderPath
    {
        get => (string)GetValue(FolderPathProperty);
        set => SetValue(FolderPathProperty, value);
    }
    
    public FolderPicker()
    {
        InitializeComponent();
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonOpenFileDialog();
        dialog.IsFolderPicker = true;
        dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
        {
            FolderPath = dialog.FileName;
        }
    }
}