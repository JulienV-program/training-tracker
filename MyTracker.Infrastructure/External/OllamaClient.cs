using System.Net.Http.Json;
using System.Text;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Infrastructure.External;

public class OllamaClient : IOllamaService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;

    public OllamaClient(HttpClient httpClient, OllamaSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<string> GenerateCommentaryAsync(Activity activity, IEnumerable<ActivityDataPoint> dataPoints, UserProfile? profile, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(activity, dataPoints, profile);
        var payload = new { model = _settings.Model, prompt, stream = false };

        var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
        return result?.response?.Trim() ?? throw new Exception("Réponse vide d'Ollama.");
    }

    private static string BuildPrompt(Activity activity, IEnumerable<ActivityDataPoint> dataPoints, UserProfile? profile)
    {
        var points = dataPoints.ToList();
        var hrDrift = ComputeHeartRateDrift(points);

        var sb = new StringBuilder();
        sb.AppendLine("Tu es un coach sportif expérimenté. Analyse la séance d'entraînement suivante et donne un retour en français, structuré en trois parties : points forts, points à travailler, conseil pour la prochaine séance. Sois concret et concis.");
        sb.AppendLine();

        if (profile != null)
        {
            sb.AppendLine("Profil de l'athlète :");
            sb.AppendLine($"Âge : {profile.Age} ans, Sexe : {profile.Sex}");
            sb.AppendLine($"Taille : {profile.HeightCm:F0} cm, Poids : {profile.WeightKg:F1} kg");
            sb.AppendLine($"FC max : {profile.MaxHeartRate} bpm, FC repos : {profile.RestingHeartRate} bpm");
            if (profile.FtpWatts.HasValue) sb.AppendLine($"FTP vélo : {profile.FtpWatts:F0} W");
            if (profile.VmaMinPerKm.HasValue) sb.AppendLine($"VMA course à pied : {FormatMinSec(profile.VmaMinPerKm.Value)} min/km");
            if (profile.CriticalSwimSpeedMinPer100m.HasValue) sb.AppendLine($"Vitesse de nage critique : {FormatMinSec(profile.CriticalSwimSpeedMinPer100m.Value)} min/100m");

            if (activity.AverageHeartRate.HasValue)
                sb.AppendLine($"FC moyenne de la séance = {activity.AverageHeartRate / profile.MaxHeartRate * 100:F0}% de la FC max");
            if (activity.MaxHeartRate.HasValue)
                sb.AppendLine($"FC max atteinte pendant la séance = {activity.MaxHeartRate / profile.MaxHeartRate * 100:F0}% de la FC max");
            sb.AppendLine();
        }

        sb.AppendLine($"Type d'activité : {activity.Type}");
        sb.AppendLine($"Nom : {activity.Name}");
        sb.AppendLine($"Distance : {activity.Distance / 1000.0:F2} km");
        sb.AppendLine($"Durée : {activity.FormattedDuration}");
        sb.AppendLine($"Dénivelé positif : {activity.ElevationGain:F0} m");
        sb.AppendLine($"Allure moyenne : {activity.AveragePace:F2} min/km");
        if (activity.AverageHeartRate.HasValue) sb.AppendLine($"FC moyenne : {activity.AverageHeartRate:F0} bpm");
        if (activity.MaxHeartRate.HasValue) sb.AppendLine($"FC max : {activity.MaxHeartRate:F0} bpm");
        if (activity.AverageWatts.HasValue) sb.AppendLine($"Puissance moyenne : {activity.AverageWatts:F0} W");
        if (activity.MaxWatts.HasValue) sb.AppendLine($"Puissance max : {activity.MaxWatts:F0} W");
        if (activity.AverageCadence.HasValue) sb.AppendLine($"Cadence moyenne : {activity.AverageCadence:F0} rpm");
        if (activity.Calories.HasValue) sb.AppendLine($"Calories : {activity.Calories:F0}");
        if (activity.SufferScore.HasValue) sb.AppendLine($"Indice d'effort (suffer score) : {activity.SufferScore:F0}");
        if (hrDrift.HasValue) sb.AppendLine($"Dérive de FC (1er tiers vs dernier tiers de la séance) : {hrDrift:F1} bpm");

        return sb.ToString();
    }

    private static string FormatMinSec(double decimalMinutes)
    {
        var minutes = (int)decimalMinutes;
        var seconds = (int)Math.Round((decimalMinutes - minutes) * 60);
        return $"{minutes}:{seconds:D2}";
    }

    private static double? ComputeHeartRateDrift(List<ActivityDataPoint> points)
    {
        var withHr = points.Where(p => p.HeartRate.HasValue).ToList();
        if (withHr.Count < 6) return null;

        var third = withHr.Count / 3;
        var firstThird = withHr.Take(third).Average(p => p.HeartRate!.Value);
        var lastThird = withHr.Skip(withHr.Count - third).Average(p => p.HeartRate!.Value);

        return lastThird - firstThird;
    }

    private record OllamaGenerateResponse(string model, string response, bool done);
}
