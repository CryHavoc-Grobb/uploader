using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

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
        var dialog = new OpenFolderDialog
        {
            Title = "Choose WoW base directory",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
        };

        if (dialog.ShowDialog() == true)
        {
            FolderPath = dialog.FolderName;
        }
    }
}