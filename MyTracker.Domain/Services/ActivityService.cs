using MyTracker.Domain.Configurations;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Domain.Services;
public class ActivityService(
    IActivityProvider provider,
    IActivityRepository repo,
    ICsvExportService csvExportService,
    IActivityCommentaryRepository commentaryRepo,
    IOllamaService ollama,
    OllamaSettings ollamaSettings,
    IUserProfileRepository userProfileRepo)
{
    public async Task<IEnumerable<ActivityDataPoint>> GetActivityDataPointsAsync(string activityId, bool forceReimport = false)
    {
        // 1. Vérification du cache local
        if (!forceReimport && await repo.ExistsAsync(activityId))
        {
            Console.WriteLine($"📦 Chargement depuis le cache local : activité {activityId}");
            return await repo.GetDataPointsAsync(activityId);
        }

        // 2. Si pas en cache (ou ré-import forcé), appel API
        Console.WriteLine($"🌐 Appel API Strava pour l'activité {activityId}...");
        var token = await provider.GetValidAccessTokenAsync();
        var activities = await provider.GetActivitiesAsync(token);
        var activity = activities.FirstOrDefault(a => a.Id == activityId)
            ?? throw new Exception("Activité introuvable sur Strava.");
        var detail = await provider.GetActivityDetailAsync(activityId, token);
        var streams = await provider.GetActivityStreamsAsync(activityId, token);

        // 3. Sauvegarde pour la prochaine fois
        await repo.SaveActivityAsync(activity);
        await repo.SaveActivityDetailAsync(detail);
        await repo.SaveDataPointsAsync(activityId, streams);

        return streams;
    }

    public Task<Activity?> GetCachedActivityAsync(string activityId) => repo.GetActivityAsync(activityId);

    public Task<IEnumerable<ActivityLap>> GetActivityLapsAsync(string activityId) => repo.GetLapsAsync(activityId);

    public Task<IEnumerable<ActivitySplit>> GetActivitySplitsAsync(string activityId) => repo.GetSplitsAsync(activityId);

    public async Task<(string FileName, byte[] Content)> GetActivityCsvAsync(string activityId)
    {
        var dataPoints = await GetActivityDataPointsAsync(activityId);
        var bytes = csvExportService.GetCsvBytes(dataPoints);
        return ($"activity_{activityId}.csv", bytes);
    }

    // Pour l'onglet "Mes Séances"
    public async Task<IEnumerable<Activity>> GetRecentActivitiesAsync()
    {
        var token = await provider.GetValidAccessTokenAsync();
        return await provider.GetActivitiesAsync(token);
    }

    public async Task<(string FileName, byte[] Content)> GetLatestActivityCsvAsync()
    {
        var token = await provider.GetValidAccessTokenAsync();
        var activities = await provider.GetActivitiesAsync(token);
        var latest = activities.FirstOrDefault() ?? throw new Exception("Aucune activité.");

        return await GetActivityCsvAsync(latest.Id);
    }

    public async Task<List<ActivityViewModel>> GetActivitiesDashboardAsync()
    {
        var cachedActivities = await repo.GetCachedActivitiesAsync();
        var localIds = (await repo.GetStoredActivityIdsAsync()).ToHashSet();

        return cachedActivities.Select(a => new ActivityViewModel
        {
            Id = a.Id.ToString(),
            Name = a.Name,
            Date = a.Date,
            Distance = a.Distance,
            IsDownloaded = localIds.Contains(a.Id.ToString())
        }).ToList();
    }

    public async Task SyncActivitiesListAsync()
    {
        var token = await provider.GetValidAccessTokenAsync();
        var activities = await provider.GetActivitiesAsync(token);
        await repo.SaveActivitySummariesAsync(activities);
    }

    public async Task<HomeDashboardViewModel> GetHomeDashboardAsync()
    {
        var cached = (await repo.GetCachedActivitiesAsync()).ToList();
        var downloadedIds = (await repo.GetStoredActivityIdsAsync()).ToHashSet();
        var now = DateTime.UtcNow;
        var latest = cached.OrderByDescending(a => a.Date).FirstOrDefault();

        return new HomeDashboardViewModel
        {
            TotalActivities = cached.Count,
            TotalDistanceKm = cached.Sum(a => a.Distance) / 1000.0,
            DistanceLast7DaysKm = cached.Where(a => a.Date >= now.AddDays(-7)).Sum(a => a.Distance) / 1000.0,
            DistanceLast30DaysKm = cached.Where(a => a.Date >= now.AddDays(-30)).Sum(a => a.Distance) / 1000.0,
            LatestActivity = latest,
            LatestActivityIsDownloaded = latest != null && downloadedIds.Contains(latest.Id)
        };
    }

    public Task<UserProfile?> GetUserProfileAsync() => userProfileRepo.GetProfileAsync();

    public Task SaveUserProfileAsync(UserProfile profile) => userProfileRepo.SaveProfileAsync(profile);

    public async Task<string> GetOrGenerateCommentaryAsync(string activityId, bool forceRegenerate = false)
    {
        if (!forceRegenerate)
        {
            var cached = await commentaryRepo.GetCommentaryAsync(activityId);
            if (cached != null) return cached;
        }

        var activity = await repo.GetActivityAsync(activityId)
            ?? throw new Exception("Activité introuvable en cache.");
        var dataPoints = await repo.GetDataPointsAsync(activityId);
        var profile = await userProfileRepo.GetProfileAsync();

        var commentary = await ollama.GenerateCommentaryAsync(activity, dataPoints, profile);
        await commentaryRepo.SaveCommentaryAsync(activityId, commentary, modelUsed: ollamaSettings.Model);
        return commentary;
    }

// Petit modèle de vue pour l'UI
    public class ActivityViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public double Distance { get; set; }
        public bool IsDownloaded { get; set; }
    }

    public class HomeDashboardViewModel
    {
        public int TotalActivities { get; set; }
        public double TotalDistanceKm { get; set; }
        public double DistanceLast7DaysKm { get; set; }
        public double DistanceLast30DaysKm { get; set; }
        public Activity? LatestActivity { get; set; }
        public bool LatestActivityIsDownloaded { get; set; }
    }
}
