using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using WeathersnakeAvalonia.ViewModels;

namespace WeathersnakeAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        
        var logoUri = new Uri("avares://WeathersnakeAvalonia/Assets/logo.ico");
        var stream = AssetLoader.Open(logoUri);
        Icon = new WindowIcon(stream);
    }
}