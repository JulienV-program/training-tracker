using Microsoft.EntityFrameworkCore;
using MyTracker.Domain.Interfaces;
using MyTracker.Infrastructure.Persistence.Entities;

namespace MyTracker.Infrastructure.Persistence;

public class SqliteActivityCommentaryRepository(TrainingTrackerDbContext db) : IActivityCommentaryRepository
{
    public async Task<string?> GetCommentaryAsync(string activityId)
    {
        var entity = await db.ActivityCommentaries.FindAsync(activityId);
        return entity?.CommentaryText;
    }

    public async Task SaveCommentaryAsync(string activityId, string commentaryText, string modelUsed)
    {
        var entity = await db.ActivityCommentaries.FindAsync(activityId);
        if (entity == null)
        {
            entity = new ActivityCommentaryEntity { ActivityId = activityId };
            db.ActivityCommentaries.Add(entity);
        }

        entity.CommentaryText = commentaryText;
        entity.ModelUsed = modelUsed;
        entity.GeneratedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}
