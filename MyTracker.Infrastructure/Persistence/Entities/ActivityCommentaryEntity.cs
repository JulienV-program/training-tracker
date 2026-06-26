namespace MyTracker.Infrastructure.Persistence.Entities;

public class ActivityCommentaryEntity
{
    public string ActivityId { get; set; } = string.Empty;
    public string CommentaryText { get; set; } = string.Empty;
    public DateTime GeneratedAtUtc { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}
