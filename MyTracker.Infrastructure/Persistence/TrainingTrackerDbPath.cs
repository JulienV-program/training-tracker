namespace MyTracker.Infrastructure.Persistence;

/// <summary>Emplacements locaux partagés par l'application web et la console (~/.trainingtracker).</summary>
public static class TrainingTrackerDbPath
{
    public static string DataDirectory
    {
        get
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dir = Path.Combine(home, ".trainingtracker");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string DefaultTokenFilePath => Path.Combine(DataDirectory, "tokens.json");

    public static string ResolveConnectionString(string? configuredConnectionString)
    {
        if (!string.IsNullOrWhiteSpace(configuredConnectionString))
            return configuredConnectionString;

        return $"Data Source={Path.Combine(DataDirectory, "trainingtracker.db")}";
    }
}
