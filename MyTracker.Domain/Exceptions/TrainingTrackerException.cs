namespace MyTracker.Domain.Exceptions;

/// <summary>
/// Base de toutes les erreurs métier de l'application. Permet à l'UI de distinguer
/// une erreur attendue (message affichable) d'un bug inattendu.
/// </summary>
public abstract class TrainingTrackerException(string message, Exception? innerException = null)
    : Exception(message, innerException);
