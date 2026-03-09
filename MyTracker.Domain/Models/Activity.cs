namespace MyTracker.Domain.Models;

public record Activity(
    string Id,
    string Name,
    DateTime Date,
    string Type,
    double Distance,
    int MovingTime,
    double ElevationGain,
    double? AverageHeartRate,
    double? MaxHeartRate,
    double? AverageWatts,   // Puissance (vélo ou course avec capteur)
    double? MaxWatts,
    double? AverageCadence,
    double? Calories,
    double? SufferScore     // L'indice d'effort Strava
)
{
    public double AveragePace => MovingTime > 0 ? (MovingTime / 60.0) / (Distance / 1000.0) : 0;
    public string FormattedDuration => TimeSpan.FromSeconds(MovingTime).ToString(@"hh\:mm\:ss");

    public DateTime StartDateLocal { get; internal set; }
}