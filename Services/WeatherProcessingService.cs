using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using WeathersnakeAvalonia.Models;

namespace WeathersnakeAvalonia.Services;

public interface IWeatherProcessingService
{
    List<ProcessedWeatherData> ProcessWeatherData(
        RawWeatherResponse rawData,
        string period,
        string units,
        bool monthly,
        (int month, int day)? customStart,
        (int month, int day)? customEnd,
        double precipThreshold = 0.0,
        bool rainOnly = false);
    
    int CustomRangeDays(int startMonth, int startDay, int endMonth, int endDay);
}

public class WeatherProcessingService : IWeatherProcessingService
{
    private static readonly Dictionary<string, (int startMonth, int endMonth)> SeasonMonths = new()
    {
        ["Summer"] = (12, 2),
        ["Autumn"] = (3, 5),
        ["Winter"] = (6, 8),
        ["Spring"] = (9, 11),
        ["Full Year"] = (1, 12)
    };
    
    private static readonly string[] MonthNames = { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

    public List<ProcessedWeatherData> ProcessWeatherData(
        RawWeatherResponse rawData,
        string period,
        string units,
        bool monthly,
        (int month, int day)? customStart,
        (int month, int day)? customEnd,
        double precipThreshold = 0.0,
        bool rainOnly = false)
    {
        var isCustom = customStart.HasValue && customEnd.HasValue;
        Log.Debug("Processing weather data: period={Period}, units={Units}, monthly={Monthly}, custom={IsCustom}, rainOnly={RainOnly}", period, units, monthly, isCustom, rainOnly);

        var daily = rawData.Daily;
        var count = daily.Time.Count;
        
        var records = new List<WeatherData>(count);
        for (int i = 0; i < count; i++)
        {
            records.Add(new WeatherData
            {
                Date = DateTime.Parse(daily.Time[i]),
                TempMax = daily.Temperature2mMax[i],
                TempMin = daily.Temperature2mMin[i],
                PrecipSum = daily.PrecipitationSum[i]
            });
        }
        
        Log.Debug("Raw dataframe: {Count} rows", records.Count);

        if (precipThreshold > 0)
        {
            var zeroed = 0;
            foreach (var r in records)
            {
                if (r.PrecipSum < precipThreshold)
                {
                    r.PrecipSum = 0;
                    zeroed++;
                }
            }
            Log.Debug("Zeroed {Zeroed} precipitation values below {Threshold} mm threshold", zeroed, precipThreshold);
        }

        records = records.Where(r => !(r.Date.Month == 2 && r.Date.Day == 29)).ToList();
        Log.Debug("Excluded leap day rows, {Count} rows remaining", records.Count);

        int startMonth;
        if (isCustom && customStart.HasValue && customEnd.HasValue)
        {
            var (sm, sd) = customStart.Value;
            var (em, ed) = customEnd.Value;
            
            records = records.Where(r => IsDateInCustomRange(r.Date, sm, sd, em, ed)).ToList();
            Log.Debug("After custom range filter ({Month}/{Day} - {Em}/{Ed}): {Count} rows", sm, sd, em, ed, records.Count);
            
            var span = CustomRangeDays(sm, sd, em, ed);
            monthly = span > 31;
            startMonth = sm;
        }
        else
        {
            var (sm, em) = GetSeasonMonths(period);
            records = records.Where(r => IsDateInSeason(r.Date, sm, em)).ToList();
            Log.Debug("After season filter ({Period}): {Count} rows", period, records.Count);
            startMonth = sm;
        }

        var result = new List<ProcessedWeatherData>();
        
        if (monthly)
        {
            List<(int Month, double TempMax, double TempMin, double PrecipSum, int Days)> grouped;
            
            if (rainOnly)
            {
                grouped = records
                    .Where(r => r.PrecipSum > 0)
                    .GroupBy(r => r.Date.Month)
                    .Select(g => (
                        Month: g.Key,
                        TempMax: g.Average(r => r.TempMax),
                        TempMin: g.Average(r => r.TempMin),
                        PrecipSum: g.Average(r => r.PrecipSum),
                        Days: g.Count()
                    ))
                    .OrderBy(x => x.Month >= startMonth || startMonth > 12 ? x.Month : x.Month + 12)
                    .ToList();
            }
            else
            {
                grouped = records
                    .GroupBy(r => r.Date.Month)
                    .Select(g => (
                        Month: g.Key,
                        TempMax: g.Average(r => r.TempMax),
                        TempMin: g.Average(r => r.TempMin),
                        PrecipSum: g.Average(r => r.PrecipSum),
                        Days: g.Count()
                    ))
                    .OrderBy(x => x.Month >= startMonth || startMonth > 12 ? x.Month : x.Month + 12)
                    .ToList();
            }
            
            foreach (var g in grouped)
            {
                var label = rainOnly && g.Days > 0 ? $"{MonthNames[g.Month]} ({g.Days}d)" : MonthNames[g.Month];
                result.Add(new ProcessedWeatherData
                {
                    DateLabel = label,
                    TempMax = g.TempMax,
                    TempMin = g.TempMin,
                    PrecipSum = g.PrecipSum
                });
            }
        }
        else
        {
            var customEndMonthValue = customEnd?.month ?? 12;
            var seasonEndMonth = isCustom ? customEndMonthValue : SeasonMonths[period].endMonth;
            var grouped = records
                .GroupBy(r => (r.Date.Month, r.Date.Day))
                .Select(g => new
                {
                    Md = g.Key,
                    TempMax = g.Average(r => r.TempMax),
                    TempMin = g.Average(r => r.TempMin),
                    PrecipSum = g.Average(r => r.PrecipSum)
                })
                .OrderBy(x => GetSortKey(x.Md, startMonth, isCustom ? customEndMonthValue : seasonEndMonth))
                .ToList();
            
            foreach (var g in grouped)
            {
                result.Add(new ProcessedWeatherData
                {
                    DateLabel = $"{g.Md.Month:D2}-{g.Md.Day:D2}",
                    TempMax = g.TempMax,
                    TempMin = g.TempMin,
                    PrecipSum = g.PrecipSum
                });
            }
        }

        if (precipThreshold > 0)
        {
            foreach (var r in result.Where(x => x.PrecipSum < precipThreshold))
            {
                r.PrecipSum = 0;
            }
        }

        if (units == "imperial")
        {
            foreach (var r in result)
            {
                r.TempMax = r.TempMax * 9.0 / 5.0 + 32;
                r.TempMin = r.TempMin * 9.0 / 5.0 + 32;
                r.PrecipSum = r.PrecipSum / 25.4;
            }
        }

        Log.Debug("Processing complete: {Count} output rows", result.Count);
        return result;
    }

    public int CustomRangeDays(int startMonth, int startDay, int endMonth, int endDay)
    {
        var start = new DateTime(2001, startMonth, Math.Min(startDay, 28));
        var end = new DateTime(2001, endMonth, Math.Min(endDay, 28));
        
        if (start <= end)
        {
            return (end - start).Days;
        }
        
        return (new DateTime(2001, 12, 31) - start).Days + (end - new DateTime(2001, 1, 1)).Days + 1;
    }

    private (int startMonth, int endMonth) GetSeasonMonths(string period)
    {
        if (SeasonMonths.TryGetValue(period, out var months))
        {
            return months;
        }
        
        Log.Error("Unknown period requested: {Period}", period);
        throw new ValueErrorException($"Unknown period: {period}");
    }

    private bool IsDateInSeason(DateTime date, int startMonth, int endMonth)
    {
        var month = date.Month;
        if (startMonth <= endMonth)
        {
            return startMonth <= month && month <= endMonth;
        }
        return month >= startMonth || month <= endMonth;
    }

    private bool IsDateInCustomRange(DateTime date, int startMonth, int startDay, int endMonth, int endDay)
    {
        if (date.Month == 2 && date.Day == 29)
            return false;
        
        int mdMonth = date.Month;
        int mdDay = date.Day;
        int startM = startMonth;
        int startD = startDay;
        int endM = endMonth;
        int endD = endDay;
        
        bool startLteMd = startM < mdMonth || (startM == mdMonth && startD <= mdDay);
        bool mdLteEnd = mdMonth < endM || (mdMonth == endM && mdDay <= endD);
        bool mdGteStart = mdMonth >= startM || (mdMonth == startM && mdDay >= startD);
        bool startLteEnd = startM < endM || (startM == endM && startD <= endD);
        
        if (startLteEnd)
        {
            return startLteMd && mdLteEnd;
        }
        
        return mdGteStart || mdLteEnd;
    }

    private string GetSortKey((int month, int day) md, int startMonth, int endMonth)
    {
        var m = md.month;
        if (startMonth > endMonth && m >= startMonth)
        {
            return $"0000-{m:D2}-{md.day:D2}";
        }
        return $"0001-{m:D2}-{md.day:D2}";
    }
}