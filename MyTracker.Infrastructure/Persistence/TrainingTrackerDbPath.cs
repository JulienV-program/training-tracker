namespace MyTracker.Infrastructure.Persistence;

public static class TrainingTrackerDbPath
{
    public static string ResolveConnectionString(string? configuredConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
            return configuredConnectionString;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = Path.Combine(home, ".trainingtracker");
        Directory.CreateDirectory(dir);
        var dbPath = Path.Combine(dir, "trainingtracker.db");
        return $"Data Source={dbPath}";
    }
}
