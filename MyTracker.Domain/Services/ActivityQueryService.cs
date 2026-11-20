using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Domain.Services;

/// <summary>Lectures pour l'UI, exclusivement depuis le cache local (aucun appel réseau).</summary>
public class ActivityQueryService(IActivityRepository repo, TimeProvider clock)
{
    public async Task<IReadOnlyList<ActivityListItem>> GetActivityListAsync()
    {
        var cached = await repo.GetCachedActivitiesAsync();
        var downloadedIds = (await repo.GetStoredActivityIdsAsync()).ToHashSet();

        return cached
            .Select(a => new ActivityListItem(a.Id, a.Name, a.Date, a.Distance, downloadedIds.Contains(a.Id)))
            .ToList();
    }

    public async Task<HomeDashboard> GetHomeDashboardAsync()
    {
        var cached = (await repo.GetCachedActivitiesAsync()).ToList();
        var downloadedIds = (await repo.GetStoredActivityIdsAsync()).ToHashSet();
        var now = clock.GetUtcNow().UtcDateTime;
        var latest = cached.MaxBy(a => a.Date);

        double DistanceKmSince(DateTime from) => cached.Where(a => a.Date >= from).Sum(a => a.Distance) / 1000.0;

        return new HomeDashboard(
            TotalActivities: cached.Count,
            TotalDistanceKm: cached.Sum(a => a.Distance) / 1000.0,
            DistanceLast7DaysKm: DistanceKmSince(now.AddDays(-7)),
            DistanceLast30DaysKm: DistanceKmSince(now.AddDays(-30)),
            LatestActivity: latest,
            LatestActivityIsDownloaded: latest is not null && downloadedIds.Contains(latest.Id));
    }

    public Task<Activity?> GetActivityAsync(string activityId) => repo.GetActivityAsync(activityId);

    public Task<IEnumerable<ActivityLap>> GetLapsAsync(string activityId) => repo.GetLapsAsync(activityId);

    public Task<IEnumerable<ActivitySplit>> GetSplitsAsync(string activityId) => repo.GetSplitsAsync(activityId);
}
