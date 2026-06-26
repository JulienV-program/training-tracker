namespace MyTracker.Domain.Models;

public record ActivityDetail(
    string ActivityId,
    double AverageSpeed,
    double MaxSpeed,
    double? StartLat,
    double? StartLng,
    double? EndLat,
    double? EndLng,
    string? MapPolyline,
    IReadOnlyList<ActivityLap> Laps,
    IReadOnlyList<ActivitySplit> Splits
);
