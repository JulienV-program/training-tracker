namespace MyTracker.Domain.Models;

/// <summary>Ligne de la liste "Mes séances".</summary>
public record ActivityListItem(string Id, string Name, DateTime Date, double Distance, bool IsDownloaded);

/// <summary>Indicateurs de la page d'accueil.</summary>
public record HomeDashboard(
    int TotalActivities,
    double TotalDistanceKm,
    double DistanceLast7DaysKm,
    double DistanceLast30DaysKm,
    Activity? LatestActivity,
    bool LatestActivityIsDownloaded);
