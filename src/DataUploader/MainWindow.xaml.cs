using System.Windows;

namespace DataUploader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new UploaderViewModel();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var token = Environment.GetEnvironmentVariable("CH_API_KEY");
        if (!string.IsNullOrEmpty(token))
            return;

        MessageBox.Show("Missing API Key (CH_API_KEY)!", "Cry Havoc Bank Uploader");
        Environment.Exit(1);
    }
}