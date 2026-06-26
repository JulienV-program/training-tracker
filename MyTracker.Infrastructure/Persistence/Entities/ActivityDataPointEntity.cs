namespace MyTracker.Infrastructure.Persistence.Entities;

public class ActivityDataPointEntity
{
    public int Id { get; set; }
    public string ActivityId { get; set; } = string.Empty;
    public int TimeOffset { get; set; }
    public double? Distance { get; set; }
    public double? HeartRate { get; set; }
    public double? Watts { get; set; }
    public double? Cadence { get; set; }
    public double? Altitude { get; set; }
    public double? Grade { get; set; }
    public double? Velocity { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public bool? Moving { get; set; }
    public double? Temperature { get; set; }
}
