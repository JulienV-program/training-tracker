using MyTracker.Domain.Models;

namespace MyTracker.Domain.Interfaces;

public interface IOllamaService
{
    Task<string> GenerateCommentaryAsync(Activity activity, IEnumerable<ActivityDataPoint> dataPoints, CancellationToken ct = default);
}
