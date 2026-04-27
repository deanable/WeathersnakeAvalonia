using System;
using System.IO;
using Avalonia;
using Avalonia.Styling;
using Serilog;
using Serilog.Events;

namespace WeathersnakeAvalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
            "WeathersnakeAvalonia", "weathersnake.log");
        
        Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
        
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
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}