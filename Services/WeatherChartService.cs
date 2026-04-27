using System;
using System.Collections.Generic;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using WeathersnakeAvalonia.Models;

namespace WeathersnakeAvalonia.Services;

public interface IChartService
{
    ISeries[] CreateChartSeries(List<ProcessedWeatherData> data, string units);
}

public class WeatherChartService : IChartService
{
    public ISeries[] CreateChartSeries(List<ProcessedWeatherData> data, string units)
    {
        var tempUnit = units == "imperial" ? "°F" : "°C";
        var precipUnit = units == "imperial" ? "in" : "mm";

        var tempMaxValues = data.Select(d => d.TempMax).ToArray();
        var tempMinValues = data.Select(d => d.TempMin).ToArray();
        var precipValues = data.Select(d => d.PrecipSum).ToArray();

        return new ISeries[]
        {
            new LineSeries<double>
            {
                Name = $"Max Temp ({tempUnit})",
                Values = tempMaxValues,
                Stroke = new SolidColorPaint(SKColors.Red, 2),
                GeometryStroke = new SolidColorPaint(SKColors.Red, 2),
                GeometryFill = new SolidColorPaint(SKColors.Red),
                GeometrySize = 6,
                Fill = null
            },
            new LineSeries<double>
            {
                Name = $"Min Temp ({tempUnit})",
                Values = tempMinValues,
                Stroke = new SolidColorPaint(SKColors.Orange, 2),
                GeometryStroke = new SolidColorPaint(SKColors.Orange, 2),
                GeometryFill = new SolidColorPaint(SKColors.Orange),
                GeometrySize = 6,
                Fill = null
            },
            new ColumnSeries<double>
            {
                Name = $"Precip ({precipUnit})",
                Values = precipValues,
                Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(80)),
                MaxBarWidth = 20
            }
        };
    }
}