using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.Persistence;
using Xunit;

namespace MyTracker.Infrastructure.Tests;

public class SqliteUserProfileRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TrainingTrackerDbContext _db;
    private readonly SqliteUserProfileRepository _repo;

    public SqliteUserProfileRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TrainingTrackerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TrainingTrackerDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new SqliteUserProfileRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenNoProfileSaved()
    {
        Assert.Null(await _repo.GetProfileAsync());
    }

    [Fact]
    public async Task SaveProfileAsync_ThenGetProfileAsync_RoundTrips()
    {
        var profile = new UserProfile(30, "Homme", 180, 75, 190, 55);
        await _repo.SaveProfileAsync(profile);

        var loaded = await _repo.GetProfileAsync();

        Assert.Equal(profile, loaded);
    }

    [Fact]
    public async Task SaveProfileAsync_RoundTripsOptionalPerformanceFields_WhenProvided()
    {
        var profile = new UserProfile(30, "Homme", 180, 75, 190, 55, FtpWatts: 250, VmaMinPerKm: 3.5, CriticalSwimSpeedMinPer100m: 1.5);
        await _repo.SaveProfileAsync(profile);

        var loaded = await _repo.GetProfileAsync();

        Assert.Equal(250, loaded!.FtpWatts);
        Assert.Equal(3.5, loaded.VmaMinPerKm);
        Assert.Equal(1.5, loaded.CriticalSwimSpeedMinPer100m);
    }

    [Fact]
    public async Task SaveProfileAsync_UpdatesExistingProfile_RatherThanCreatingASecondRow()
    {
        await _repo.SaveProfileAsync(new UserProfile(30, "Homme", 180, 75, 190, 55));
        await _repo.SaveProfileAsync(new UserProfile(31, "Homme", 180, 76, 191, 54));

        var loaded = await _repo.GetProfileAsync();

        Assert.Equal(31, loaded!.Age);
        Assert.Equal(1, await _db.UserProfile.CountAsync());
    }
}
