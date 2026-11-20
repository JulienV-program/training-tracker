using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyTracker.Web.Components;
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Services;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Csv;
using MyTracker.Infrastructure.Persistence;

// 1. Hôte web
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5215");

// 2. Blazor Server (composants interactifs côté serveur)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 3. Configuration typée, validée au démarrage (DataAnnotations) : l'application refuse de
//    démarrer avec une configuration Strava incomplète plutôt que d'échouer au premier appel.
builder.Services.AddOptions<StravaSettings>()
    .Bind(builder.Configuration.GetSection(StravaSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<OllamaSettings>()
    .Bind(builder.Configuration.GetSection(OllamaSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
// Le domaine consomme les POCO de configuration sans dépendre de Microsoft.Extensions.Options.
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<StravaSettings>>().Value);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<OllamaSettings>>().Value);

// 4. Base de données SQLite (cache local des activités, points de séance, commentaires IA)
var connectionString = TrainingTrackerDbPath.ResolveConnectionString(
    builder.Configuration.GetConnectionString("TrainingTrackerDb"));
builder.Services.AddDbContext<TrainingTrackerDbContext>(opts => opts.UseSqlite(connectionString));

// 5. Adaptateurs d'infrastructure (ports définis dans le domaine)
builder.Services.AddHttpClient<IActivityProvider, StravaProvider>();
builder.Services.AddHttpClient<IOllamaService, OllamaClient>();
builder.Services.AddScoped<IActivityRepository, SqliteActivityRepository>();
builder.Services.AddScoped<IActivityCommentaryRepository, SqliteActivityCommentaryRepository>();
builder.Services.AddScoped<IUserProfileRepository, SqliteUserProfileRepository>();
builder.Services.AddSingleton<ICsvExportService, CsvExportService>();
builder.Services.AddSingleton(TimeProvider.System);

// Services applicatifs (un par responsabilité)
builder.Services.AddScoped<ActivityImportService>();
builder.Services.AddScoped<ActivityQueryService>();
builder.Services.AddScoped<CoachingService>();
builder.Services.AddScoped<ActivityExportService>();
builder.Services.AddScoped<UserProfileService>();

// 6. Construction de l'application
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

// 9. Routage des composants Razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
