using Avalonia.Controls;
using Avalonia.Platform;
using WeathersnakeAvalonia.ViewModels;

namespace WeathersnakeAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        
        var logoUri = new Uri("avares://WeathersnakeAvalonia/Assets/logo.ico");
        var assets = Avalonia.Application.Current?.Resources;
        if (assets != null)
        {
            var stream = AssetLoader.Open(logoUri);
            Icon = new Avalonia.Media.Imaging.Bitmap(stream);
        }
    }
}