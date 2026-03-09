using MyTracker.Domain.Models;

namespace MyTracker.Domain.Interfaces;

public interface IActivityProvider
{
    // Récupère les activités depuis une source externe (Strava)
    Task<IEnumerable<Activity>> GetActivitiesAsync(string accessToken);
    Task<IEnumerable<ActivityDataPoint>> GetActivityStreamsAsync(string activityId, string accessToken);
    Task<string> GetValidAccessTokenAsync();
}