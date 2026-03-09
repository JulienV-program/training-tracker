using Microsoft.Extensions.Configuration;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Services;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Csv;

// 1. Chargement de la configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json")
    .Build();

var settings = config.GetSection("Strava").Get<StravaSettings>();

// 2. Initialisation des outils (L'infrastructure)
using var httpClient = new HttpClient();
var stravaProvider = new StravaProvider(httpClient, settings!);
var csvRepo = new CsvActivityRepository();

// 3. Le Chef d'Orchestre (On lui donne ses outils)
var activityService = new ActivityService(stravaProvider, csvRepo);

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