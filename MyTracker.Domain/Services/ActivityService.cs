using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Domain.Services;
public class ActivityService(IActivityProvider provider, IActivityRepository repo)
{
    private readonly IActivityProvider _provider = provider;
    private readonly IActivityRepository _repo = repo;

    public async Task<(string FileName, byte[] Content)> GetActivityCsvAsync(string activityId)
    {
        string fileName = $"activity_{activityId}.csv";

        // 1. Vérification du cache local
        if (_repo.Exists(activityId))
        {
            Console.WriteLine($"📦 Chargement depuis le cache Mac : {fileName}");
            return (fileName, _repo.Read(activityId));
        }

        // 2. Si pas en cache, appel API
        Console.WriteLine($"🌐 Appel API Strava pour l'activité {activityId}...");
        var token = await _provider.GetValidAccessTokenAsync();
        var streams = await _provider.GetActivityStreamsAsync(activityId, token);
        
        var bytes = _repo.GetCsvBytes(streams);
        
        // 3. Sauvegarde pour la prochaine fois
        _repo.Save(activityId, bytes);
        
        return (fileName, bytes);
    }

    // Pour l'onglet "Mes Séances"
    public async Task<IEnumerable<Activity>> GetRecentActivitiesAsync()
    {
        var token = await _provider.GetValidAccessTokenAsync();
        return await _provider.GetActivitiesAsync(token);
    }

    public async Task<(string FileName, byte[] Content)> GetLatestActivityCsvAsync()
    {
        var token = await _provider.GetValidAccessTokenAsync();
        var activities = await _provider.GetActivitiesAsync(token);
        var latest = activities.FirstOrDefault() ?? throw new Exception("Aucune activité.");

        return await GetActivityCsvAsync(latest.Id);
    }

    public async Task<List<ActivityViewModel>> GetActivitiesDashboardAsync()
    {
        var token = await _provider.GetValidAccessTokenAsync();
        var stravaActivities = await _provider.GetActivitiesAsync(token);
        var localIds = _repo.GetStoredActivityIds().ToHashSet();

        return stravaActivities.Select(a => new ActivityViewModel
        {
            Id = a.Id.ToString(),
            Name = a.Name,
            Date = a.Date,
            Distance = a.Distance,
            IsDownloaded = localIds.Contains(a.Id.ToString())
        }).ToList();
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
}