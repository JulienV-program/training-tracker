using Microsoft.Extensions.Logging;
using MyTracker.Domain.Exceptions;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Domain.Services;

/// <summary>
/// Synchronisation Strava → cache local : liste des activités et import détaillé
/// (streams seconde par seconde, laps, splits) selon une stratégie cache-first.
/// </summary>
public class ActivityImportService(
    IActivityProvider provider,
    IActivityRepository repo,
    ILogger<ActivityImportService> logger)
{
    /// <summary>Récupère la liste des activités Strava et la fusionne dans le cache local.</summary>
    public async Task SyncActivitiesListAsync()
    {
        var token = await provider.GetValidAccessTokenAsync();
        var activities = (await provider.GetActivitiesAsync(token)).ToList();
        await repo.SaveActivitySummariesAsync(activities);
        logger.LogInformation("Synchronisation Strava : {Count} activités fusionnées dans le cache", activities.Count);
    }

    /// <summary>
    /// Retourne les points de mesure d'une activité depuis le cache local, ou l'importe
    /// depuis Strava si elle n'a jamais été importée en détail (ou si <paramref name="forceReimport"/>).
    /// </summary>
    public async Task<IEnumerable<ActivityDataPoint>> GetOrImportDataPointsAsync(string activityId, bool forceReimport = false)
    {
        if (!forceReimport && await repo.ExistsAsync(activityId))
        {
            logger.LogDebug("Activité {ActivityId} servie depuis le cache local", activityId);
            return await repo.GetDataPointsAsync(activityId);
        }

        logger.LogInformation("Import de l'activité {ActivityId} depuis Strava (ré-import forcé : {Force})", activityId, forceReimport);
        var token = await provider.GetValidAccessTokenAsync();

        // Le résumé déjà synchronisé évite de re-paginer toute la liste Strava ;
        // un ré-import forcé repart de Strava pour rafraîchir aussi le résumé.
        var activity = (forceReimport ? null : await repo.GetActivityAsync(activityId))
            ?? (await provider.GetActivitiesAsync(token)).FirstOrDefault(a => a.Id == activityId)
            ?? throw new ActivityNotFoundException(activityId, "Strava");

        // Détail et streams sont indépendants : appels en parallèle.
        var detailTask = provider.GetActivityDetailAsync(activityId, token);
        var streamsTask = provider.GetActivityStreamsAsync(activityId, token);
        await Task.WhenAll(detailTask, streamsTask);
        var streams = (await streamsTask).ToList();

        await repo.SaveActivityAsync(activity);
        await repo.SaveActivityDetailAsync(await detailTask);
        await repo.SaveDataPointsAsync(activityId, streams);

        logger.LogInformation("Activité {ActivityId} importée : {PointCount} points", activityId, streams.Count);
        return streams;
    }

    /// <summary>Identifiant de la dernière activité publiée sur Strava.</summary>
    public async Task<string> GetLatestStravaActivityIdAsync()
    {
        var token = await provider.GetValidAccessTokenAsync();
        var latest = (await provider.GetActivitiesAsync(token)).MaxBy(a => a.Date)
            ?? throw new ActivityNotFoundException("(dernière)", "aucune activité sur Strava");
        return latest.Id;
    }
}
