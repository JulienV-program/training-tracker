using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Exceptions;
using MyTracker.Domain.Services;
using MyTracker.Infrastructure.Csv;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Persistence;

// Utilitaire console : exporte la dernière activité Strava en CSV.
// Câblage manuel (sans conteneur DI) : le domaine ne dépend que d'interfaces, il se compose à la main.

// 1. Configuration (appsettings.json optionnel + variables d'environnement Strava__ClientId, ...)
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

using var loggerFactory = LoggerFactory.Create(b => b
    .AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; })
    .SetMinimumLevel(LogLevel.Information));
var logger = loggerFactory.CreateLogger("MyTracker.App");

var stravaSettings = config.GetSection(StravaSettings.SectionName).Get<StravaSettings>() ?? new StravaSettings();
var validationErrors = new List<ValidationResult>();
if (!Validator.TryValidateObject(stravaSettings, new ValidationContext(stravaSettings), validationErrors, validateAllProperties: true))
{
    foreach (var error in validationErrors)
        logger.LogError("Configuration Strava invalide : {Error}", error.ErrorMessage);
    return 1;
}

// 2. Infrastructure
using var httpClient = new HttpClient();
var stravaProvider = new StravaProvider(httpClient, stravaSettings, loggerFactory.CreateLogger<StravaProvider>());

var connectionString = TrainingTrackerDbPath.ResolveConnectionString(config.GetConnectionString("TrainingTrackerDb"));
var dbOptions = new DbContextOptionsBuilder<TrainingTrackerDbContext>().UseSqlite(connectionString).Options;
await using var db = new TrainingTrackerDbContext(dbOptions);
await db.Database.MigrateAsync();

// 3. Services du domaine (les mêmes que ceux de l'application web)
var importer = new ActivityImportService(stravaProvider, new SqliteActivityRepository(db), loggerFactory.CreateLogger<ActivityImportService>());
var exporter = new ActivityExportService(importer, new CsvExportService());

try
{
    logger.LogInformation("Export de la dernière activité Strava...");
    var (fileName, content) = await exporter.GetLatestActivityCsvAsync();
    await File.WriteAllBytesAsync(fileName, content);
    logger.LogInformation("Fichier {FileName} généré ({Size} octets)", fileName, content.Length);
    return 0;
}
catch (TrainingTrackerException ex)
{
    logger.LogError("{Message}", ex.Message);
    return 2;
}
