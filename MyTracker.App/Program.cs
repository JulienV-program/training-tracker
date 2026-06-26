using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Services;
using MyTracker.Infrastructure.Csv;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Persistence;

// 1. Chargement de la configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .Build();

var settings = config.GetSection("Strava").Get<StravaSettings>();
var ollamaSettings = config.GetSection("Ollama").Get<OllamaSettings>() ?? new OllamaSettings();

// 2. Initialisation des outils (L'infrastructure)
using var httpClient = new HttpClient();
var stravaProvider = new StravaProvider(httpClient, settings!);
using var ollamaHttpClient = new HttpClient();
var ollamaClient = new OllamaClient(ollamaHttpClient, ollamaSettings);

var connectionString = TrainingTrackerDbPath.ResolveConnectionString(config.GetConnectionString("TrainingTrackerDb"));
var dbOptions = new DbContextOptionsBuilder<TrainingTrackerDbContext>().UseSqlite(connectionString).Options;
using var db = new TrainingTrackerDbContext(dbOptions);
db.Database.Migrate();

var activityRepo = new SqliteActivityRepository(db);
var commentaryRepo = new SqliteActivityCommentaryRepository(db);
var csvExportService = new CsvExportService();
var userProfileRepo = new SqliteUserProfileRepository(db);

// 3. Le Chef d'Orchestre (On lui donne ses outils)
var activityService = new ActivityService(stravaProvider, activityRepo, csvExportService, commentaryRepo, ollamaClient, ollamaSettings, userProfileRepo);

try
{
    Console.WriteLine("🚀 Lancement de l'extraction (Mode Console)...");

    // ON UTILISE LE SERVICE UNIQUE ICI
    var (fileName, content) = await activityService.GetLatestActivityCsvAsync();

    // Comme on est dans une console, on écrit physiquement le fichier
    await File.WriteAllBytesAsync(fileName, content);

    Console.WriteLine($"✨ Succès ! Fichier '{fileName}' généré avec succès.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Erreur critique : {ex.Message}");
}
