using Microsoft.Extensions.Logging;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Exceptions;
using MyTracker.Domain.Interfaces;

namespace MyTracker.Domain.Services;

/// <summary>Commentaire de séance façon coach, généré par le LLM local et mis en cache.</summary>
public class CoachingService(
    IActivityRepository repo,
    IActivityCommentaryRepository commentaryRepo,
    IUserProfileRepository userProfileRepo,
    IOllamaService ollama,
    OllamaSettings ollamaSettings,
    ILogger<CoachingService> logger)
{
    public async Task<string> GetOrGenerateCommentaryAsync(string activityId, bool forceRegenerate = false, CancellationToken ct = default)
    {
        if (!forceRegenerate)
        {
            var cached = await commentaryRepo.GetCommentaryAsync(activityId);
            if (cached is not null) return cached;
        }

        var activity = await repo.GetActivityAsync(activityId)
            ?? throw new ActivityNotFoundException(activityId, "cache local");
        var dataPoints = await repo.GetDataPointsAsync(activityId);
        var profile = await userProfileRepo.GetProfileAsync();

        logger.LogInformation("Génération du commentaire IA pour {ActivityId} avec {Model}", activityId, ollamaSettings.Model);
        var commentary = await ollama.GenerateCommentaryAsync(activity, dataPoints, profile, ct);
        await commentaryRepo.SaveCommentaryAsync(activityId, commentary, modelUsed: ollamaSettings.Model);
        return commentary;
    }
}
