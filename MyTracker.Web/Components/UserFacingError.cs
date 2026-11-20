using MyTracker.Domain.Exceptions;

namespace MyTracker.Web.Components;

/// <summary>
/// Traduit une exception en message affichable : le détail d'une erreur métier attendue
/// est montré à l'utilisateur, un bug inattendu est journalisé sans exposer sa trace.
/// </summary>
public static class UserFacingError
{
    public static string Describe(Exception ex, ILogger logger, string context)
    {
        if (ex is TrainingTrackerException)
        {
            logger.LogWarning(ex, "{Context}", context);
            return $"{context} : {ex.Message}";
        }

        logger.LogError(ex, "Erreur inattendue : {Context}", context);
        return $"{context} : une erreur inattendue est survenue (détails dans les logs).";
    }
}
