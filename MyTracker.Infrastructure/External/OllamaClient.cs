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

    public async Task<string> GenerateCommentaryAsync(Activity activity, IEnumerable<ActivityDataPoint> dataPoints, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(activity, dataPoints);
        var payload = new { model = _settings.Model, prompt, stream = false };

        var response = await _httpClient.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken: ct);
        return result?.response?.Trim() ?? throw new Exception("Réponse vide d'Ollama.");
    }

    private static string BuildPrompt(Activity activity, IEnumerable<ActivityDataPoint> dataPoints)
    {
        var points = dataPoints.ToList();
        var hrDrift = ComputeHeartRateDrift(points);

        var sb = new StringBuilder();
        sb.AppendLine("Tu es un coach sportif expérimenté. Analyse la séance d'entraînement suivante et donne un retour en français, structuré en trois parties : points forts, points à travailler, conseil pour la prochaine séance. Sois concret et concis.");
        sb.AppendLine();
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
