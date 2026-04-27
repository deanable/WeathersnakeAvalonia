using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Serilog;
using WeathersnakeAvalonia.Models;

namespace WeathersnakeAvalonia.Services;

public interface IWeatherApiClient
{
    Task<(double Lat, double Lon)> GetCoordinatesAsync(string cityName);
    Task<RawWeatherResponse> FetchHistoricalWeatherAsync(double lat, double lon, string startDate, string endDate);
}

public class WeatherApiClient : IWeatherApiClient
{
    private readonly HttpClient _httpClient;
    private const int RequestTimeout = 30;

    public WeatherApiClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(RequestTimeout)
        };
    }

    public async Task<(double Lat, double Lon)> GetCoordinatesAsync(string cityName)
    {
        var url = "https://geocoding-api.open-meteo.com/v1/search";
        var city = Uri.EscapeDataString(cityName);
        var requestUrl = $"{url}?name={city}&count=1&format=json";
        
        Log.Debug("Geocoding request: {Url}", requestUrl);
        
        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Geocoding request failed: {StatusCode} - {Content}", response.StatusCode, content);
                throw new HttpRequestException($"Geocoding API error: {response.StatusCode}");
            }
            
            var data = JsonSerializer.Deserialize<GeocodingResponse>(content);
            
            if (data?.Results == null || data.Results.Count == 0)
            {
                Log.Error("No coordinates found for city: {City}", cityName);
                throw new ValueErrorException($"Could not find coordinates for city: {cityName}");
            }
            
            var result = data.Results[0];
            Log.Debug("Resolved '{City}' to lat={Lat:F4}, lon={Lon:F4}", cityName, result.Latitude, result.Longitude);
            
            return (result.Latitude, result.Longitude);
        }
        catch (Exception ex) when (ex is not HttpRequestException && ex is not ValueErrorException)
        {
            Log.Error(ex, "Geocoding request failed for city '{City}'", cityName);
            throw;
        }
    }

    public async Task<RawWeatherResponse> FetchHistoricalWeatherAsync(double lat, double lon, string startDate, string endDate)
    {
        var url = "https://archive-api.open-meteo.com/v1/archive";
        var latStr = lat.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lonStr = lon.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var requestUrl = $"{url}?latitude={latStr}&longitude={lonStr}&start_date={startDate}&end_date={endDate}" +
                       $"&daily=temperature_2m_max,temperature_2m_min,precipitation_sum&timezone=auto";
        
        Log.Debug("Weather request: {Url}", requestUrl);
        
        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            var content = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Weather request failed: {StatusCode} - {Content}", response.StatusCode, content);
                throw new HttpRequestException($"Weather API error: {response.StatusCode}");
            }
            
            var data = JsonSerializer.Deserialize<RawWeatherResponse>(content);
            
            if (data?.Daily?.Time == null)
            {
                Log.Error("No daily data in weather response");
                throw new InvalidOperationException("No daily data found in API response.");
            }
            
            Log.Debug("Weather response: {Count} daily records", data.Daily.Time.Count);
            return data;
        }
        catch (Exception ex) when (ex is not HttpRequestException && ex is not InvalidOperationException)
        {
            Log.Error(ex, "Weather request failed for lat={Lat}, lon={Lon}, {Start} to {End}", lat, lon, startDate, endDate);
            throw;
        }
    }
}

public class ValueErrorException : Exception
{
    public ValueErrorException(string message) : base(message) { }
}

public class GeocodingResponse
{
    [JsonPropertyName("results")]
    public List<GeocodingResult> Results { get; set; } = new();
}

public class GeocodingResult
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }
    
    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

public class RawWeatherResponse
{
    [JsonPropertyName("daily")]
    public DailyWeatherData Daily { get; set; } = new();
}

public class DailyWeatherData
{
    [JsonPropertyName("time")]
    public List<string> Time { get; set; } = new();
    
    [JsonPropertyName("temperature_2m_max")]
    public List<double> Temperature2mMax { get; set; } = new();
    
    [JsonPropertyName("temperature_2m_min")]
    public List<double> Temperature2mMin { get; set; } = new();
    
    [JsonPropertyName("precipitation_sum")]
    public List<double> PrecipitationSum { get; set; } = new();
}