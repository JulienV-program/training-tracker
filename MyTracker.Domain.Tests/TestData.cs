using MyTracker.Domain.Models;

namespace MyTracker.Domain.Tests;

internal static class TestData
{
    public static Activity Activity(string id = "123") => new(
        id, "Test Run", DateTime.UtcNow, "Run", 5000, 1800, 50,
        150, 180, null, null, 80, 300, 60);

    public static ActivityDetail Detail(string id = "123") => new(id, 3.0, 5.0, null, null, null, null, null, [], []);

    public static List<ActivityDataPoint> Points() => [new(0, 0, 100, null, null, null, null, null)];
}

/// <summary>Horloge figée pour rendre les calculs de fenêtres temporelles déterministes.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
