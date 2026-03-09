using System.Net.Http.Json;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Domain.Configurations;
using MyTracker.Infrastructure.Dto;
using System.Text.Json;

namespace MyTracker.Infrastructure.External;

public class StravaProvider : IActivityProvider
{
    private readonly HttpClient _httpClient;
    private readonly StravaSettings _settings;

    public StravaProvider(HttpClient httpClient, StravaSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _httpClient.BaseAddress = new Uri("https://www.strava.com/api/v3/");
    }

    public async Task<IEnumerable<Activity>> GetActivitiesAsync(string accessToken)
    {
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.GetFromJsonAsync<List<StravaActivityDto>>("athlete/activities");

        return response?.Select(dto => new Activity(
                dto.id.ToString(),
                dto.name,
                dto.start_date,
                dto.type,
                dto.distance,
                dto.moving_time,
                dto.total_elevation_gain,
                dto.average_heartrate,
                dto.max_heartrate,
                dto.average_watts,
                dto.max_watts,
                dto.average_cadence,
                dto.calories,
                dto.suffer_score
            )) ?? [];
    }

    public async Task<string> RefreshAccessTokenAsync()
    {
        var payload = new
        {
            client_id = _settings.ClientId,
            client_secret = _settings.ClientSecret,
            grant_type = "refresh_token",
            refresh_token = _settings.RefreshToken
        };

        var response = await _httpClient.PostAsJsonAsync("https://www.strava.com/oauth/token", payload);
        response.EnsureSuccessStatusCode(); // Vérifie si l'appel a réussi

        var data = await response.Content.ReadFromJsonAsync<StravaTokenResponse>();
        return data?.Access_token ?? throw new Exception("Impossible de récupérer le token.");
    }

    public async Task<StravaTokenResponse> ExchangeCodeForTokenAsync(string code)
{
    var values = new Dictionary<string, string>
    {
        { "client_id", _settings.ClientId },
        { "client_secret", _settings.ClientSecret },
        { "code", code },
        { "grant_type", "authorization_code" }
    };

    var content = new FormUrlEncodedContent(values);
    
    var response = await _httpClient.PostAsync("https://www.strava.com/oauth/token", content);
    
    if (!response.IsSuccessStatusCode)
    {
        var errorBody = await response.Content.ReadAsStringAsync();
        throw new Exception($"Erreur Strava ({response.StatusCode}) : {errorBody}");
    }

    return await response.Content.ReadFromJsonAsync<StravaTokenResponse>() 
           ?? throw new Exception("Réponse vide de Strava");
}

    public async Task<string> GetValidAccessTokenAsync()
    {
        var tokenPath = "tokens.json";
        StravaToken? currentToken = null;

        if (File.Exists(tokenPath))
        {
            var json = await File.ReadAllTextAsync(tokenPath);
            currentToken = JsonSerializer.Deserialize<StravaToken>(json);
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (currentToken != null && currentToken.ExpiresAt > (now + 60))
        {
            return currentToken.AccessToken;
        }

        Console.WriteLine("🔄 Token expiré ou absent, rafraîchissement...");
        var refreshToken = currentToken?.RefreshToken ?? _settings.RefreshToken;

        var payload = new Dictionary<string, string>
        {
            { "client_id", _settings.ClientId },
            { "client_secret", _settings.ClientSecret },
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        var response = await _httpClient.PostAsync("https://www.strava.com/oauth/token", new FormUrlEncodedContent(payload));
        
        if (!response.IsSuccessStatusCode) throw new Exception("Erreur de rafraîchissement Strava.");

        var result = await response.Content.ReadFromJsonAsync<StravaTokenResponse>();

        var newToken = new StravaToken
        {
            AccessToken = result!.Access_token,
            RefreshToken = result!.Refresh_token,
            ExpiresAt = result!.Expires_at
        };

        await File.WriteAllTextAsync(tokenPath, JsonSerializer.Serialize(newToken));
        
        return newToken.AccessToken;
    }

    public async Task<IEnumerable<ActivityDataPoint>> GetActivityStreamsAsync(string activityId, string accessToken)
    {
        var streamKeys = "time,distance,heartrate,watts,cadence,altitude,grade_smooth,velocity_smooth";
        var url = $"activities/{activityId}/streams?keys={streamKeys}&key_by_type=true";

        _httpClient.DefaultRequestHeaders.Authorization = new ("Bearer", accessToken);
        
        var response = await _httpClient.GetAsync(url);
        if (!response.IsSuccessStatusCode) return [];

        var streams = await response.Content.ReadFromJsonAsync<Dictionary<string, StravaStreamDto>>();

        if (streams == null || !streams.TryGetValue("time", out StravaStreamDto? value)) return [];

        var timeList = value.Data;
        var dataPoints = new List<ActivityDataPoint>();

        for (int i = 0; i < timeList.Count; i++)
        {
            // Correction ici : on extrait l'entier du JsonElement
            int timeOffset = timeList[i].GetInt32(); 

            dataPoints.Add(new ActivityDataPoint(
                TimeOffset: timeOffset,
                Distance: GetStreamValue(streams, "distance", i),
                HeartRate: GetStreamValue(streams, "heartrate", i),
                Watts: GetStreamValue(streams, "watts", i),
                Cadence: GetStreamValue(streams, "cadence", i),
                Altitude: GetStreamValue(streams, "altitude", i),
                Grade: GetStreamValue(streams, "grade_smooth", i),
                Velocity: GetStreamValue(streams, "velocity_smooth", i)
            ));
        }

        return dataPoints;
    }

    private static double? GetStreamValue(Dictionary<string, StravaStreamDto> streams, string key, int index)
    {
        if (streams.TryGetValue(key, out var stream) && index < stream.Data.Count)
            {
                if (stream.Data[index] is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        return element.GetDouble();
                    }
                    
                    // Cas particulier : si Strava envoie un booléen (ex: "moving")
                    if (element.ValueKind == JsonValueKind.True) return 1.0;
                    if (element.ValueKind == JsonValueKind.False) return 0.0;
                }
            }
        return null;
    }

// DTO pour désérialiser les listes de Strava
    public record StravaStreamDto(List<JsonElement> Data);
// DTO pour la réponse du token
    public record StravaTokenResponse(string Access_token, string Refresh_token, long Expires_at);
}