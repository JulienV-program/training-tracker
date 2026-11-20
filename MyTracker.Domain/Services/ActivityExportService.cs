using MyTracker.Domain.Interfaces;

namespace MyTracker.Domain.Services;

/// <summary>Export CSV des points de mesure d'une activité (importée à la volée si nécessaire).</summary>
public class ActivityExportService(ActivityImportService importer, ICsvExportService csvExportService)
{
    public async Task<(string FileName, byte[] Content)> GetActivityCsvAsync(string activityId)
    {
        var dataPoints = await importer.GetOrImportDataPointsAsync(activityId);
        return ($"activity_{activityId}.csv", csvExportService.GetCsvBytes(dataPoints));
    }

    public async Task<(string FileName, byte[] Content)> GetLatestActivityCsvAsync()
        => await GetActivityCsvAsync(await importer.GetLatestStravaActivityIdAsync());
}
