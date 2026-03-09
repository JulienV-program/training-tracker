namespace MyTracker.Domain.Models;

public record ActivityDataPoint(
    int TimeOffset,         // Secondes écoulées depuis le début
    double? Distance,       // En mètres
    double? HeartRate,      // BPM
    double? Watts,          // Puissance
    double? Cadence,        // RPM
    double? Altitude,       // En mètres
    double? Grade,          // Pente en %
    double? Velocity        // m/s
);