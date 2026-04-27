using Avalonia.Controls;
using WeathersnakeAvalonia.ViewModels;

namespace WeathersnakeAvalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}