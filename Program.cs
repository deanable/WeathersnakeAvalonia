using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Serilog;

namespace WeathersnakeAvalonia;

class Program
{
#if WINDOWS
    [STAThread]
#endif
    public static void Main(string[] args)
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WeathersnakeAvalonia");
        
        Directory.CreateDirectory(logDir);
        var logFile = Path.Combine(logDir, "weathersnake.log");
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logFile, 
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("Starting Weather Juice Avalonia");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

        var iconPath = OperatingSystem.IsMacOS()
            ? "avares://WeathersnakeAvalonia/Assets/logo.icns"
            : "avares://WeathersnakeAvalonia/Assets/logo.ico";
        
        try
        {
            var iconStream = AssetLoader.Open(new Uri(iconPath));
            if (iconStream != null)
            {
                builder.With(new WindowIcon(iconStream));
            }
        }
        catch { }

        return builder;
    }
}