using System;
using System.Collections.Generic;

namespace WeathersnakeAvalonia.Models;

public class WeatherData
{
    public DateTime Date { get; set; }
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double PrecipSum { get; set; }
}

public class ProcessedWeatherData
{
    public string DateLabel { get; set; } = string.Empty;
    public double TempMax { get; set; }
    public double TempMin { get; set; }
    public double PrecipSum { get; set; }
}

public class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class FetchParameters
{
    public string City { get; set; } = string.Empty;
    public string Period { get; set; } = "Summer";
    public int Depth { get; set; } = 10;
    public string Units { get; set; } = "metric";
    public bool Monthly { get; set; } = true;
    public bool UnifyScales { get; set; } = true;
    public double PrecipThreshold { get; set; } = 5.0;
    public int? StartMonth { get; set; }
    public int? StartDay { get; set; }
    public int? EndMonth { get; set; }
    public int? EndDay { get; set; }
    public int? SelectedMonth { get; set; }
}