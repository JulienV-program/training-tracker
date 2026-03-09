namespace MyTracker.Infrastructure.Dto;

// Un petit DTO (Data Transfer Object) pour mapper le JSON de Strava
// C'est ici qu'on gère les noms de champs bizarres de l'API
public record StravaActivityDto(
    long id,
    string name,
    DateTime start_date,
    string type,
    double distance,
    int moving_time,
    double total_elevation_gain,
    double? average_heartrate,
    double? max_heartrate,
    double? average_watts,
    double? max_watts,
    double? average_cadence,
    double? calories,
    double? suffer_score
);