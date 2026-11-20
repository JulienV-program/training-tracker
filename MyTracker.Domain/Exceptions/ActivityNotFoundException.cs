namespace MyTracker.Domain.Exceptions;

/// <summary>Activité absente de la source consultée (cache local ou Strava).</summary>
public sealed class ActivityNotFoundException(string activityId, string lookedUpIn)
    : TrainingTrackerException($"Activité {activityId} introuvable ({lookedUpIn}).")
{
    public string ActivityId { get; } = activityId;
    public string LookedUpIn { get; } = lookedUpIn;
}
