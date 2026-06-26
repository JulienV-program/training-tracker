using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyTracker.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TrainingTrackerDbContext>
{
    public TrainingTrackerDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TrainingTrackerDbContext>();
        optionsBuilder.UseSqlite(TrainingTrackerDbPath.ResolveConnectionString(null));
        return new TrainingTrackerDbContext(optionsBuilder.Options);
    }
}
