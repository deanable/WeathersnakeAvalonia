using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;
using WeathersnakeAvalonia.Models;
using WeathersnakeAvalonia.Services;

namespace WeathersnakeAvalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IWeatherApiClient _apiClient;
    private readonly IWeatherProcessingService _processingService;
    private readonly IChartService _chartService;
    
    [ObservableProperty]
    private string _selectedLocation = "Cape Town";
    
    [ObservableProperty]
    private string _selectedPeriod = "Summer";
    
    [ObservableProperty]
    private bool _showCustomCityInput;
    
    [ObservableProperty]
    private string _selectedDepth = "10";
    
    [ObservableProperty]
    private string _selectedUnits = "metric";
    
    [ObservableProperty]
    private bool _monthlyAverage = true;
    
    [ObservableProperty]
    private bool _rainOnly;
    
    [ObservableProperty]
    private bool _sharedYAxis = true;
    
    [ObservableProperty]
    private double _precipThreshold = 5.0;
    
    [ObservableProperty]
    private string _statusText = "Ready.";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private bool _canSave;
    
    [ObservableProperty]
    private bool _showCustomRange;
    
    [ObservableProperty]
    private bool _showMonthSelector;
    
    [ObservableProperty]
    private string _customStartDay = "1";
    
    [ObservableProperty]
    private string _customStartMonth = "Jan";
    
    [ObservableProperty]
    private string _customEndDay = "31";
    
    [ObservableProperty]
    private string _customEndMonth = "Mar";
    
    [ObservableProperty]
    private string _selectedMonth = "Jan";
    
    [ObservableProperty]
    private string _customCityInput = "";
    
    [ObservableProperty]
    private ObservableCollection<ProcessedWeatherData> _weatherResults = new();
    
    private string _lastCity = "";
    private string _lastPeriod = "";
    private int _lastDepth;
    private string _lastTempUnit = "°C";
    private string _lastPrecipUnit = "mm";

    [ObservableProperty]
    private ISeries[] _chartSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private Axis[] _xAxes = Array.Empty<Axis>();
    
    [ObservableProperty]
    private Axis[] _yAxes = Array.Empty<Axis>();

    public string[] LocationPresets { get; } = { "Cape Town", "Johannesburg", "Durban", "Custom..." };
    public string[] PeriodOptions { get; } = { "Summer", "Autumn", "Winter", "Spring", "Full Year", "Month", "Custom Range" };
    public string[] DepthOptions { get; } = { "1", "5", "10", "20" };
    public string[] UnitOptions { get; } = { "metric", "imperial" };
    public string[] MonthNames { get; } = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
    public string[] DayValues { get; } = Enumerable.Range(1, 31).Select(x => x.ToString()).ToArray();
    
    public string FormattedTempUnit => SelectedUnits == "imperial" ? "°F" : "°C";
    public string FormattedPrecipUnit => SelectedUnits == "imperial" ? "in" : "mm";

    public MainViewModel()
    {
        _apiClient = new WeatherApiClient();
        _processingService = new WeatherProcessingService();
        _chartService = new WeatherChartService();
    }

    partial void OnSelectedPeriodChanged(string value)
    {
        ShowCustomRange = value == "Custom Range";
        ShowMonthSelector = value == "Month";
    }

    partial void OnSelectedLocationChanged(string value)
    {
        ShowCustomCityInput = value == "Custom...";
    }

    partial void OnSelectedUnitsChanged(string value)
    {
        OnPropertyChanged(nameof(FormattedTempUnit));
        OnPropertyChanged(nameof(FormattedPrecipUnit));
    }

    [RelayCommand]
    private async Task FetchDataAsync()
    {
        var city = SelectedLocation == "Custom..." ? CustomCityInput : SelectedLocation;
        if (string.IsNullOrWhiteSpace(city))
        {
            StatusText = "Please enter a city or location name.";
            return;
        }

        IsLoading = true;
        CanSave = false;
        StatusText = "Fetching coordinates...";
        WeatherResults.Clear();

        try
        {
            var (lat, lon) = await _apiClient.GetCoordinatesAsync(city);
            StatusText = $"Fetching historical data for {city}...";
            
            var currentYear = DateTime.Now.Year;
            var endYear = currentYear - 1;
            var startYear = endYear - int.Parse(SelectedDepth) + 1;
            var startDate = $"{startYear - 1}-01-01";
            var endDate = $"{endYear}-12-31";

            (int month, int day)? customStart = null;
            (int month, int day)? customEnd = null;
            string displayPeriod = SelectedPeriod;
            bool monthly = MonthlyAverage;
            bool isCustom = SelectedPeriod == "Custom Range" || SelectedPeriod == "Month";

            if (SelectedPeriod == "Custom Range")
            {
                var sm = Array.IndexOf(MonthNames, CustomStartMonth) + 1;
                var sd = int.Parse(CustomStartDay);
                var em = Array.IndexOf(MonthNames, CustomEndMonth) + 1;
                var ed = int.Parse(CustomEndDay);
                customStart = (sm, sd);
                customEnd = (em, ed);
                displayPeriod = $"Custom: {sd} {MonthNames[sm - 1]} - {ed} {MonthNames[em - 1]}";
            }
            else if (SelectedPeriod == "Month")
            {
                var m = Array.IndexOf(MonthNames, SelectedMonth) + 1;
                var lastDay = new DateTime(2001, m, 1).AddMonths(1).AddDays(-1).Day;
                customStart = (m, 1);
                customEnd = (m, lastDay);
                displayPeriod = SelectedMonth;
            }

            if (isCustom && customStart.HasValue && customEnd.HasValue)
            {
                monthly = _processingService.CustomRangeDays(customStart.Value.month, customStart.Value.day,
                    customEnd.Value.month, customEnd.Value.day) > 31;
            }

            var rawData = await _apiClient.FetchHistoricalWeatherAsync(lat, lon, startDate, endDate);
            StatusText = "Processing...";

            var processedData = _processingService.ProcessWeatherData(
                rawData, SelectedPeriod, SelectedUnits, monthly, customStart, customEnd, PrecipThreshold, RainOnly);

            if (processedData.Count == 0)
            {
                throw new InvalidOperationException("No data available for the given timeframe.");
            }

            _lastCity = city;
            _lastPeriod = displayPeriod;
            _lastDepth = int.Parse(SelectedDepth);

            WeatherResults.Clear();
            var tempUnit = SelectedUnits == "imperial" ? "°F" : "°C";
            var precipUnit = SelectedUnits == "imperial" ? "in" : "mm";

            foreach (var row in processedData)
            {
                WeatherResults.Add(new ProcessedWeatherData
                {
                    DateLabel = row.DateLabel,
                    TempMax = row.TempMax,
                    TempMin = row.TempMin,
                    PrecipSum = row.PrecipSum
                });
            }
            
            _lastCity = city;
            _lastPeriod = displayPeriod;
            _lastDepth = int.Parse(SelectedDepth);
            _lastTempUnit = tempUnit;
            _lastPrecipUnit = precipUnit;
            
            Log.Information("Added {Count} rows to WeatherResults", WeatherResults.Count);
            
            ChartSeries = _chartService.CreateChartSeries(processedData, SelectedUnits);
            XAxes = new Axis[]
            {
                new Axis
                {
                    Labels = processedData.Select(d => d.DateLabel).ToArray(),
                    LabelsRotation = 45
                }
            };
            YAxes = new Axis[] { new Axis() };
            
            StatusText = "Ready.";
            CanSave = true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during data fetch/processing");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SaveChartAsync()
    {
        if (ChartSeries.Length == 0) return;

        var storage = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        if (storage?.MainWindow == null) return;

        var file = await storage.MainWindow.StorageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
        {
            FileTypeChoices = new[]
            {
                new Avalonia.Platform.Storage.FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg" } }
            },
            SuggestedFileName = GenerateFilename()
        });

        if (file == null) return;

        var path = file.Path.LocalPath;
        StatusText = "Saving...";
        
        try
        {
            SaveChartImage(path, 1200, 800);
            StatusText = "Saved.";
            Log.Information("Chart saved to {Path}", path);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save chart");
            StatusText = "Save failed";
        }
    }

    private void SaveChartImage(string path, int width, int height)
    {
        var lastCity = _lastCity;
        var lastPeriod = _lastPeriod;
        var tempUnit = _lastTempUnit;
        var precipUnit = _lastPrecipUnit;

        using var bitmap = new SkiaSharp.SKBitmap(width, height);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        canvas.Clear(SkiaSharp.SKColors.White);

        var titlePaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Black,
            TextSize = 28,
            IsAntialias = true,
            Typeface = SkiaSharp.SKTypeface.FromFamilyName("Arial", SkiaSharp.SKFontStyle.Bold)
        };
        canvas.DrawText($"Weather for {lastCity} ({lastPeriod})", 50, 50, titlePaint);

        var subtitlePaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Gray,
            TextSize = 18,
            IsAntialias = true
        };
        canvas.DrawText($"Average over {_lastDepth} years", 50, 85, subtitlePaint);

        if (WeatherResults.Count == 0) return;

        var marginTop = 120;
        var marginBottom = 100;
        var margin = 80;
        var chartWidth = width - margin * 2;
        var chartHeight = height - marginTop - marginBottom;
        var maxTemp = WeatherResults.Max(r => r.TempMax);
        var minTemp = WeatherResults.Min(r => r.TempMin);
        var maxPrecip = Math.Max(WeatherResults.Max(r => r.PrecipSum), 0.1);
        
        var xStep = (float)chartWidth / WeatherResults.Count;
        
        var yMinAll = Math.Min(minTemp, 0);
        var yMaxAll = Math.Max(maxTemp, maxPrecip * 2);
        var yRange = yMaxAll - yMinAll;
        if (yRange < 5) yRange = 5;
        
        var yPadding = yRange * 0.1;
        yMinAll -= yPadding;
        yMaxAll += yPadding;
        yRange = yMaxAll - yMinAll;

        var axisPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Gray,
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SkiaSharp.SKPaintStyle.Stroke
        };

        canvas.DrawLine(margin, height - marginBottom, width - margin, height - marginBottom, axisPaint);
        canvas.DrawLine(margin, marginTop, margin, height - marginBottom, axisPaint);

        var redPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Red,
            StrokeWidth = 3,
            IsAntialias = true,
            Style = SkiaSharp.SKPaintStyle.Stroke
        };
        var orangePaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Orange,
            StrokeWidth = 3,
            IsAntialias = true,
            Style = SkiaSharp.SKPaintStyle.Stroke
        };
        var bluePaint = new SkiaSharp.SKPaint
        {
            Color = new SkiaSharp.SKColor(80, 80, 255, 128),
            IsAntialias = true,
            Style = SkiaSharp.SKPaintStyle.Fill
        };

        for (int i = 0; i < WeatherResults.Count; i++)
        {
            var data = WeatherResults[i];
            var x = margin + i * xStep;
            var yMax = height - marginBottom - (float)((data.TempMax - yMinAll) / yRange * chartHeight);
            var yMin = height - marginBottom - (float)((data.TempMin - yMinAll) / yRange * chartHeight);
            
            if (i > 0)
            {
                var prev = WeatherResults[i - 1];
                var prevX = margin + (i - 1) * xStep;
                var prevYMax = height - marginBottom - (float)((prev.TempMax - yMinAll) / yRange * chartHeight);
                var prevYMin = height - marginBottom - (float)((prev.TempMin - yMinAll) / yRange * chartHeight);
                canvas.DrawLine(prevX, prevYMax, x, yMax, redPaint);
                canvas.DrawLine(prevX, prevYMin, x, yMin, orangePaint);
            }

            var barHeight = (float)(data.PrecipSum / yRange * chartHeight);
            var barWidth = Math.Max(xStep * 0.6f, 4);
            canvas.DrawRect(x + xStep * 0.2f, height - marginBottom - barHeight, barWidth, barHeight, bluePaint);
        }

        var labelPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Gray,
            TextSize = 14,
            IsAntialias = true
        };
        
        var numLabels = 5;
        for (int i = 0; i <= numLabels; i++)
        {
            var value = yMinAll + yRange * i / numLabels;
            var y = height - marginBottom - (float)((value - yMinAll) / yRange * chartHeight);
            canvas.DrawText(value.ToString("F1"), margin - 50, y + 5, labelPaint);
            axisPaint.StrokeWidth = 0.5f;
            canvas.DrawLine(margin, y, width - margin, y, axisPaint);
        }

        var xLabelPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Gray,
            TextSize = 12,
            IsAntialias = true
        };
        
        var xLabelStep = Math.Max(1, (int)(WeatherResults.Count / 14));
        for (int i = 0; i < WeatherResults.Count; i += xLabelStep)
        {
            var label = WeatherResults[i].DateLabel;
            var xPos = margin + (i * xStep) + xStep * 0.3f;
            canvas.DrawText(label, xPos, height - marginBottom + 25, xLabelPaint);
        }

        var legendPaint = new SkiaSharp.SKPaint
        {
            TextSize = 16,
            IsAntialias = true
        };
        legendPaint.Color = SkiaSharp.SKColors.Red;
        canvas.DrawText("—— Max Temp (" + tempUnit + ")", margin, height - 50, legendPaint);
        legendPaint.Color = SkiaSharp.SKColors.Orange;
        canvas.DrawText("—— Min Temp (" + tempUnit + ")", margin + 150, height - 50, legendPaint);
        legendPaint.Color = new SkiaSharp.SKColor(80, 80, 255);
        canvas.DrawText("▮ Precip (" + precipUnit + ")", margin + 320, height - 50, legendPaint);

        // Watermark in bottom-left corner
        var watermarkPaint = new SkiaSharp.SKPaint
        {
            Color = new SkiaSharp.SKColor(200, 200, 200, 128),
            TextSize = 10,
            IsAntialias = true,
            Typeface = SkiaSharp.SKTypeface.FromFamilyName("Arial")
        };
        var watermarkText = "Weather Juice v1.0.24";
        canvas.DrawText(watermarkText, margin, height - 10, watermarkPaint);

        var logoUri = new Uri("avares://WeathersnakeAvalonia/Assets/logo.png");
        using var logoStream = AssetLoader.Open(logoUri);
        using var logoBitmap = SkiaSharp.SKBitmap.Decode(logoStream);
        if (logoBitmap != null)
        {
            var logoDest = new SkiaSharp.SKRect(0, height - 20, 20, height);
            using var resizedLogo = logoBitmap.Resize(new SkiaSharp.SKImageInfo(20, 20), SkiaSharp.SKFilterQuality.Medium);
            canvas.DrawBitmap(resizedLogo, logoDest);
        }

        var data_jpeg = bitmap.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 90);
        var bytes = data_jpeg.ToArray();
        System.IO.File.WriteAllBytes(path, bytes);
    }

    private string GenerateFilename()
    {
        var period = _lastPeriod.Replace(" ", "_").Replace(":", "");
        return $"{_lastDepth}yr_{period}_{_lastCity}.jpg";
    }
}