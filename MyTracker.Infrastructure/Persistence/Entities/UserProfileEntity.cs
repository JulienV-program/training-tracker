namespace MyTracker.Infrastructure.Persistence.Entities;

public class UserProfileEntity
{
    public int Id { get; set; }
    public int Age { get; set; }
    public string Sex { get; set; } = string.Empty;
    public double HeightCm { get; set; }
    public double WeightKg { get; set; }
    public int MaxHeartRate { get; set; }
    public int RestingHeartRate { get; set; }
    public double? FtpWatts { get; set; }
    public double? VmaMinPerKm { get; set; }
    public double? CriticalSwimSpeedMinPer100m { get; set; }
}
