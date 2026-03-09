namespace MyTracker.Domain.Models;

public class StravaToken
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public long ExpiresAt { get; set; } // Timestamp Unix
}