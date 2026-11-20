using System.ComponentModel.DataAnnotations;

namespace MyTracker.Domain.Configurations;

public class StravaSettings
{
    public const string SectionName = "Strava";

    [Required(AllowEmptyStrings = false)]
    public string ClientId { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ClientSecret { get; set; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>Fichier de cache du token OAuth. Par défaut : ~/.trainingtracker/tokens.json.</summary>
    public string? TokenFilePath { get; set; }

    /// <summary>Taille de page pour la liste des activités (max 200 côté Strava).</summary>
    [Range(1, 200)]
    public int ActivitiesPageSize { get; set; } = 100;

    /// <summary>Nombre maximal de pages récupérées lors d'une synchronisation.</summary>
    [Range(1, 100)]
    public int MaxActivityPages { get; set; } = 10;
}
