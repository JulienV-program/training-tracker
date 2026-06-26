using MyTracker.Domain.Models;

namespace MyTracker.Domain.Interfaces;

public interface IActivityRepository
{
    Task<bool> ExistsAsync(string activityId);
    Task SaveActivityAsync(Activity activity);
    Task SaveDataPointsAsync(string activityId, IEnumerable<ActivityDataPoint> points);
    Task<Activity?> GetActivityAsync(string activityId);
    Task<IEnumerable<ActivityDataPoint>> GetDataPointsAsync(string activityId);
    Task<IEnumerable<string>> GetStoredActivityIdsAsync();
    Task SaveActivityDetailAsync(ActivityDetail detail);
    Task<IEnumerable<ActivityLap>> GetLapsAsync(string activityId);
    Task<IEnumerable<ActivitySplit>> GetSplitsAsync(string activityId);
}
