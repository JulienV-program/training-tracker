namespace MyTracker.Domain.Models;

public record ActivitySplit(
    string ActivityId,
    int SplitIndex,
    double Distance,
    int ElapsedTime,
    double? ElevationDifference,
    int MovingTime,
    double AverageSpeed,
    double? AverageHeartRate,
    int? PaceZone
);
