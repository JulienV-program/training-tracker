namespace MyTracker.Infrastructure.Dto;

public record StravaDetailedActivityDto(
    long id,
    double average_speed,
    double max_speed,
    List<double>? start_latlng,
    List<double>? end_latlng,
    StravaMapDto? map,
    List<StravaLapDto>? laps,
    List<StravaSplitDto>? splits_metric
);

public record StravaMapDto(string id, string? summary_polyline);

public record StravaLapDto(
    long id,
    string name,
    int lap_index,
    int elapsed_time,
    int moving_time,
    DateTime start_date,
    double distance,
    double average_speed,
    double? max_speed,
    double? average_heartrate,
    double? max_heartrate,
    double? total_elevation_gain
);

public record StravaSplitDto(
    double distance,
    int elapsed_time,
    double? elevation_difference,
    int moving_time,
    int split,
    double average_speed,
    double? average_heartrate,
    int? pace_zone
);
