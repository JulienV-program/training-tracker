namespace MyTracker.Domain.Models;

public record UserProfile(
    int Age,
    string Sex,
    double HeightCm,
    double WeightKg,
    int MaxHeartRate,
    int RestingHeartRate,
    double? FtpWatts = null,                     // FTP vélo (watts)
    double? VmaMinPerKm = null,                  // VMA course à pied (min/km)
    double? CriticalSwimSpeedMinPer100m = null   // Vitesse de nage critique (min/100m)
);
