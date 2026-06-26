namespace MyTracker.Domain.Models;

public record ActivityLap(
    long LapId,
    string ActivityId,
    int LapIndex,
    string Name,
    int ElapsedTime,
    int MovingTime,
    DateTime StartDate,
    double Distance,
    double AverageSpeed,
    double? MaxSpeed,
    double? AverageHeartRate,
    double? MaxHeartRate,
    double? ElevationGain
);
