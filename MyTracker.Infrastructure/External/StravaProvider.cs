using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Exceptions;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.Dto;
using MyTracker.Infrastructure.Persistence;

namespace MyTracker.Infrastructure.External;

/// <summary>
/// Client de l'API Strava v3. Chaque appel construit son propre <see cref="HttpRequestMessage"/> :
/// aucun état (en-tête d'autorisation) n'est partagé sur le HttpClient, qui peut donc être mutualisé.
/// </summary>
public class StravaProvider : IActivityProvider
{
    private const string ServiceName = "Strava";
    private const string TokenEndpoint = "https://www.strava.com/oauth/token";
    private const string StreamKeys = "time,distance,heartrate,watts,cadence,altitude,grade_smooth,velocity_smooth,latlng,moving,temp";

    // Empêche deux rafraîchissements simultanés du token (ex. deux composants Blazor qui chargent en même temps).
    private static readonly SemaphoreSlim TokenRefreshLock = new(1, 1);

    private readonly HttpClient _httpClient;
    private readonly StravaSettings _settings;
    private readonly ILogger<StravaProvider> _logger;
    private readonly string _tokenFilePath;

    public StravaProvider(HttpClient httpClient, StravaSettings settings, ILogger<StravaProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;
        _tokenFilePath = string.IsNullOrWhiteSpace(settings.TokenFilePath)
            ? TrainingTrackerDbPath.DefaultTokenFilePath
            : settings.TokenFilePath;
        _httpClient.BaseAddress ??= new Uri("https://www.strava.com/api/v3/");
    }

    public async Task<IEnumerable<Activity>> GetActivitiesAsync(string accessToken)
    {
        var activities = new List<Activity>();
        var pageSize = Math.Clamp(_settings.ActivitiesPageSize, 1, 200);

        for (var page = 1; page <= _settings.MaxActivityPages; page++)
        {
            using var response = await SendAsync(HttpMethod.Get, $"athlete/activities?page={page}&per_page={pageSize}", accessToken);
            await EnsureSuccessAsync(response, "liste des activités");

            var dtos = await response.Content.ReadFromJsonAsync<List<StravaActivityDto>>() ?? [];
            activities.AddRange(dtos.Select(MapActivity));

            if (dtos.Count < pageSize) break; // dernière page atteinte
        }

        _logger.LogDebug("{Count} activités récupérées depuis Strava", activities.Count);
        return activities;
    }

    public async Task<string> GetValidAccessTokenAsync()
    {
        var cached = await ReadCachedTokenAsync();
        if (IsStillValid(cached)) return cached!.AccessToken;

        await TokenRefreshLock.WaitAsync();
        try
        {
            // Un autre appel a pu rafraîchir le token pendant l'attente du verrou.
            cached = await ReadCachedTokenAsync();
            if (IsStillValid(cached)) return cached!.AccessToken;

            _logger.LogInformation("Token Strava expiré ou absent, rafraîchissement");
            var payload = new Dictionary<string, string>
            {
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ClientSecret,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = cached?.RefreshToken ?? _settings.RefreshToken
            };

            using var response = await SendAsync(HttpMethod.Post, TokenEndpoint, accessToken: null, new FormUrlEncodedContent(payload));
            await EnsureSuccessAsync(response, "rafraîchissement du token");

            var result = await response.Content.ReadFromJsonAsync<StravaTokenResponse>()
                ?? throw new ExternalServiceException(ServiceName, "réponse vide au rafraîchissement du token");

            var newToken = new StravaToken
            {
                AccessToken = result.Access_token,
                RefreshToken = result.Refresh_token,
                ExpiresAt = result.Expires_at
            };
            await File.WriteAllTextAsync(_tokenFilePath, JsonSerializer.Serialize(newToken));
            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(_tokenFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite); // chmod 600
            return newToken.AccessToken;
        }
        finally
        {
            TokenRefreshLock.Release();
        }
    }

    public async Task<IEnumerable<ActivityDataPoint>> GetActivityStreamsAsync(string activityId, string accessToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"activities/{activityId}/streams?keys={StreamKeys}&key_by_type=true", accessToken);

        // Une activité saisie manuellement n'a pas de streams : ce n'est pas une erreur bloquante.
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Streams indisponibles pour {ActivityId} (HTTP {Status})", activityId, (int)response.StatusCode);
            return [];
        }

        var streams = await response.Content.ReadFromJsonAsync<Dictionary<string, StravaStreamDto>>();
        if (streams == null || !streams.TryGetValue("time", out var time)) return [];

        var dataPoints = new List<ActivityDataPoint>(time.Data.Count);
        for (var i = 0; i < time.Data.Count; i++)
        {
            var latLng = GetLatLngValue(streams, "latlng", i);
            var moving = GetStreamValue(streams, "moving", i);

            dataPoints.Add(new ActivityDataPoint(
                TimeOffset: time.Data[i].GetInt32(),
                Distance: GetStreamValue(streams, "distance", i),
                HeartRate: GetStreamValue(streams, "heartrate", i),
                Watts: GetStreamValue(streams, "watts", i),
                Cadence: GetStreamValue(streams, "cadence", i),
                Altitude: GetStreamValue(streams, "altitude", i),
                Grade: GetStreamValue(streams, "grade_smooth", i),
                Velocity: GetStreamValue(streams, "velocity_smooth", i),
                Lat: latLng?.Lat,
                Lng: latLng?.Lng,
                Moving: moving.HasValue ? moving == 1.0 : null,
                Temperature: GetStreamValue(streams, "temp", i)
            ));
        }

        return dataPoints;
    }

    public async Task<ActivityDetail> GetActivityDetailAsync(string activityId, string accessToken)
    {
        using var response = await SendAsync(HttpMethod.Get, $"activities/{activityId}", accessToken);
        if (response.StatusCode == HttpStatusCode.NotFound) throw new ActivityNotFoundException(activityId, ServiceName);
        await EnsureSuccessAsync(response, $"détail de l'activité {activityId}");

        var dto = await response.Content.ReadFromJsonAsync<StravaDetailedActivityDto>()
            ?? throw new ExternalServiceException(ServiceName, $"réponse vide pour le détail de l'activité {activityId}");

        var laps = (dto.laps ?? []).Select(l => new ActivityLap(
            l.id, activityId, l.lap_index, l.name, l.elapsed_time, l.moving_time, l.start_date,
            l.distance, l.average_speed, l.max_speed, l.average_heartrate, l.max_heartrate, l.total_elevation_gain
        )).ToList();

        var splits = (dto.splits_metric ?? []).Select(s => new ActivitySplit(
            activityId, s.split, s.distance, s.elapsed_time, s.elevation_difference,
            s.moving_time, s.average_speed, s.average_heartrate, s.pace_zone
        )).ToList();

        return new ActivityDetail(
            activityId,
            dto.average_speed,
            dto.max_speed,
            dto.start_latlng?.Count == 2 ? dto.start_latlng[0] : null,
            dto.start_latlng?.Count == 2 ? dto.start_latlng[1] : null,
            dto.end_latlng?.Count == 2 ? dto.end_latlng[0] : null,
            dto.end_latlng?.Count == 2 ? dto.end_latlng[1] : null,
            dto.map?.summary_polyline,
            laps,
            splits
        );
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string? accessToken, HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            return await _httpClient.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalServiceException(ServiceName, "service injoignable (vérifier la connexion réseau)", innerException: ex);
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync();
        _logger.LogWarning("Échec Strava ({Operation}) : HTTP {Status} {Body}", operation, (int)response.StatusCode, body);

        var reason = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized => "accès refusé (token ou identifiants invalides)",
            HttpStatusCode.TooManyRequests => "quota d'appels API dépassé, réessayer dans quelques minutes",
            _ => $"erreur HTTP {(int)response.StatusCode}"
        };
        throw new ExternalServiceException(ServiceName, $"{operation} : {reason}", response.StatusCode);
    }

    private async Task<StravaToken?> ReadCachedTokenAsync()
    {
        if (!File.Exists(_tokenFilePath)) return null;
        try
        {
            return JsonSerializer.Deserialize<StravaToken>(await File.ReadAllTextAsync(_tokenFilePath));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Fichier token illisible ({Path}), il sera régénéré", _tokenFilePath);
            return null;
        }
    }

    private static bool IsStillValid(StravaToken? token)
        => token is not null && token.ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;

    private static Activity MapActivity(StravaActivityDto dto) => new(
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
        dto.suffer_score);

    private static double? GetStreamValue(Dictionary<string, StravaStreamDto> streams, string key, int index)
    {
        if (!streams.TryGetValue(key, out var stream) || index >= stream.Data.Count) return null;

        return stream.Data[index].ValueKind switch
        {
            JsonValueKind.Number => stream.Data[index].GetDouble(),
            JsonValueKind.True => 1.0,   // ex. stream "moving"
            JsonValueKind.False => 0.0,
            _ => null
        };
    }

    // Le stream "latlng" renvoie des paires [lat, lng] par point, contrairement aux autres streams scalaires.
    private static (double Lat, double Lng)? GetLatLngValue(Dictionary<string, StravaStreamDto> streams, string key, int index)
    {
        if (!streams.TryGetValue(key, out var stream) || index >= stream.Data.Count) return null;

        var element = stream.Data[index];
        return element.ValueKind == JsonValueKind.Array && element.GetArrayLength() == 2
            ? (element[0].GetDouble(), element[1].GetDouble())
            : null;
    }

    public record StravaStreamDto(List<JsonElement> Data);

    public record StravaTokenResponse(string Access_token, string Refresh_token, long Expires_at);
}
