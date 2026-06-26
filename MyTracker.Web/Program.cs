using MyTracker.Web.Components; // Vérifie que c'est le bon namespace pour tes composants
using Microsoft.EntityFrameworkCore;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Services;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Csv;
using MyTracker.Infrastructure.Persistence;

// 1. On crée le fameux "builder"
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5215");

// 2. On ajoute les services de base de Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 3. Configuration Strava (Récupérée depuis appsettings.json / user-secrets)
var stravaSettings = builder.Configuration.GetSection("Strava").Get<StravaSettings>();
if (stravaSettings == null) throw new Exception("La section 'Strava' est manquante dans appsettings.json");

var ollamaSettings = builder.Configuration.GetSection("Ollama").Get<OllamaSettings>() ?? new OllamaSettings();

// 4. Base de données SQLite (cache local des activités, points de séance, commentaires IA)
var connectionString = TrainingTrackerDbPath.ResolveConnectionString(
    builder.Configuration.GetConnectionString("TrainingTrackerDb"));
builder.Services.AddDbContext<TrainingTrackerDbContext>(opts => opts.UseSqlite(connectionString));

// 5. Injection de tes services (Le pont vers ton travail précédent)
builder.Services.AddSingleton(stravaSettings);
builder.Services.AddSingleton(ollamaSettings);
builder.Services.AddHttpClient<IActivityProvider, StravaProvider>();
builder.Services.AddHttpClient<IOllamaService, OllamaClient>();
builder.Services.AddScoped<IActivityRepository, SqliteActivityRepository>();
builder.Services.AddScoped<IActivityCommentaryRepository, SqliteActivityCommentaryRepository>();
builder.Services.AddScoped<IUserProfileRepository, SqliteUserProfileRepository>();
builder.Services.AddSingleton<ICsvExportService, CsvExportService>();
builder.Services.AddScoped<ActivityService>();

// 6. On construit l'application
var app = builder.Build();

// 7. Application des migrations EF Core au démarrage
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TrainingTrackerDbContext>();
    db.Database.Migrate();
}

// 8. Configuration du pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 9. On mappe les composants Razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
