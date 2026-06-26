namespace MyTracker.Domain.Interfaces;

public interface IActivityCommentaryRepository
{
    Task<string?> GetCommentaryAsync(string activityId);
    Task SaveCommentaryAsync(string activityId, string commentaryText, string modelUsed);
}
