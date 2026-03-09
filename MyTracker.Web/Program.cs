using MyTracker.Web.Components; // Vérifie que c'est le bon namespace pour tes composants
using MyTracker.Domain.Configurations;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Services;
using MyTracker.Infrastructure.External;
using MyTracker.Infrastructure.Csv;

// 1. On crée le fameux "builder"
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5215");

// 2. On ajoute les services de base de Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 3. Configuration Strava (Récupérée depuis appsettings.json)
var stravaSettings = builder.Configuration.GetSection("Strava").Get<StravaSettings>();
if (stravaSettings == null) throw new Exception("La section 'Strava' est manquante dans appsettings.json");

// 4. Injection de tes services (Le pont vers ton travail précédent)
builder.Services.AddSingleton(stravaSettings);
builder.Services.AddHttpClient<IActivityProvider, StravaProvider>();
builder.Services.AddScoped<IActivityRepository, CsvActivityRepository>();
builder.Services.AddScoped<ActivityService>();

// 5. On construit l'application
var app = builder.Build();

// 6. Configuration du pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 7. On mappe les composants Razor
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();