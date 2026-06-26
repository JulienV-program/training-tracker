using Microsoft.EntityFrameworkCore;
using MyTracker.Infrastructure.Persistence.Entities;

namespace MyTracker.Infrastructure.Persistence;

public class TrainingTrackerDbContext(DbContextOptions<TrainingTrackerDbContext> options) : DbContext(options)
{
    public DbSet<ActivityEntity> Activities => Set<ActivityEntity>();
    public DbSet<ActivityDataPointEntity> ActivityDataPoints => Set<ActivityDataPointEntity>();
    public DbSet<ActivityCommentaryEntity> ActivityCommentaries => Set<ActivityCommentaryEntity>();
    public DbSet<ActivityLapEntity> ActivityLaps => Set<ActivityLapEntity>();
    public DbSet<ActivitySplitEntity> ActivitySplits => Set<ActivitySplitEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActivityEntity>().HasKey(a => a.Id);

        modelBuilder.Entity<ActivityDataPointEntity>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => new { p.ActivityId, p.TimeOffset }).IsUnique();
        });

        modelBuilder.Entity<ActivityCommentaryEntity>().HasKey(c => c.ActivityId);

        modelBuilder.Entity<ActivityLapEntity>(builder =>
        {
            builder.HasKey(l => l.Id);
            builder.HasIndex(l => new { l.ActivityId, l.LapIndex }).IsUnique();
        });

        modelBuilder.Entity<ActivitySplitEntity>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.HasIndex(s => new { s.ActivityId, s.SplitIndex }).IsUnique();
        });
    }
}
