using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.Persistence;
using Xunit;

namespace MyTracker.Infrastructure.Tests;

public class SqliteActivityRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TrainingTrackerDbContext _db;
    private readonly SqliteActivityRepository _repo;

    public SqliteActivityRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TrainingTrackerDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TrainingTrackerDbContext(options);
        _db.Database.EnsureCreated();
        _repo = new SqliteActivityRepository(_db);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private static Activity MakeActivity(string id = "1") => new(
        id, "Test Run", new DateTime(2026, 1, 1), "Run", 5000, 1800, 50,
        150, 180, null, null, 80, 300, 60,
        AverageSpeed: 2.8, MaxSpeed: 4.0, StartLat: 45.8, StartLng: 3.16, EndLat: 45.81, EndLng: 3.17, MapPolyline: "abc");

    [Fact]
    public async Task SaveActivityAsync_ThenGetActivityAsync_RoundTripsBaseFields()
    {
        var activity = MakeActivity();
        await _repo.SaveActivityAsync(activity);

        var loaded = await _repo.GetActivityAsync("1");

        Assert.NotNull(loaded);
        Assert.Equal(activity.Name, loaded!.Name);
        Assert.Equal(activity.Distance, loaded.Distance);
        Assert.Equal(activity.AverageHeartRate, loaded.AverageHeartRate);
        // Les champs étendus (vitesse, lat/lng, polyline) ne sont écrits que par SaveActivityDetailAsync
        Assert.Null(loaded.AverageSpeed);
    }

    [Fact]
    public async Task SaveActivityDetailAsync_ThenGetActivityAsync_RoundTripsExtendedSummaryFields()
    {
        await _repo.SaveActivityAsync(MakeActivity());
        var detail = new ActivityDetail("1", 2.8, 4.0, 45.8, 3.16, 45.81, 3.17, "abc", [], []);

        await _repo.SaveActivityDetailAsync(detail);
        var loaded = await _repo.GetActivityAsync("1");

        Assert.NotNull(loaded);
        Assert.Equal(detail.AverageSpeed, loaded!.AverageSpeed);
        Assert.Equal(detail.StartLat, loaded.StartLat);
        Assert.Equal(detail.MapPolyline, loaded.MapPolyline);
    }

    [Fact]
    public async Task ExistsAsync_ReflectsState()
    {
        Assert.False(await _repo.ExistsAsync("1"));

        await _repo.SaveActivityAsync(MakeActivity());

        Assert.True(await _repo.ExistsAsync("1"));
    }

    [Fact]
    public async Task SaveDataPointsAsync_ReplacesExistingPoints_OnSecondCall()
    {
        await _repo.SaveActivityAsync(MakeActivity());

        await _repo.SaveDataPointsAsync("1", [new ActivityDataPoint(0, 0, 100, null, null, null, null, null)]);
        await _repo.SaveDataPointsAsync("1", [
            new ActivityDataPoint(0, 0, 110, null, null, null, null, null),
            new ActivityDataPoint(1, 5, 115, null, null, null, null, null)
        ]);

        var points = (await _repo.GetDataPointsAsync("1")).ToList();

        Assert.Equal(2, points.Count);
        Assert.Equal(110, points[0].HeartRate);
    }

    [Fact]
    public async Task SaveActivityDetailAsync_PersistsAndReplacesLapsAndSplits()
    {
        await _repo.SaveActivityAsync(MakeActivity());

        var lap = new ActivityLap(1, "1", 1, "Lap 1", 600, 590, new DateTime(2026, 1, 1), 1000, 1.7, 2.0, 140, 150, 10);
        var split = new ActivitySplit("1", 1, 1000, 590, 5, 580, 1.7, 140, 2);
        var detail = new ActivityDetail("1", 2.8, 4.0, 45.8, 3.16, 45.81, 3.17, "abc", [lap], [split]);

        await _repo.SaveActivityDetailAsync(detail);

        Assert.Single(await _repo.GetLapsAsync("1"));
        Assert.Single(await _repo.GetSplitsAsync("1"));

        // Deuxième sauvegarde : doit remplacer, pas accumuler
        var newDetail = detail with { Laps = [lap, lap with { LapId = 2, LapIndex = 2 }], Splits = [] };
        await _repo.SaveActivityDetailAsync(newDetail);

        Assert.Equal(2, (await _repo.GetLapsAsync("1")).Count());
        Assert.Empty(await _repo.GetSplitsAsync("1"));
    }

    [Fact]
    public async Task GetStoredActivityIdsAsync_ReflectsSavedActivities()
    {
        await _repo.SaveActivityAsync(MakeActivity("1"));
        await _repo.SaveActivityAsync(MakeActivity("2"));

        var ids = (await _repo.GetStoredActivityIdsAsync()).ToList();

        Assert.Contains("1", ids);
        Assert.Contains("2", ids);
        Assert.Equal(2, ids.Count);
    }

    [Fact]
    public async Task GetStoredActivityIdsAsync_ExcludesActivitiesOnlySyncedAsSummary()
    {
        await _repo.SaveActivitySummariesAsync([MakeActivity("1")]); // jamais importée en détail
        await _repo.SaveActivityAsync(MakeActivity("2")); // import complet

        var ids = (await _repo.GetStoredActivityIdsAsync()).ToList();

        Assert.DoesNotContain("1", ids);
        Assert.Contains("2", ids);
    }

    [Fact]
    public async Task SaveActivitySummariesAsync_NeverDowngradesAlreadyFullyImportedActivity()
    {
        await _repo.SaveActivityAsync(MakeActivity("1")); // import complet -> IsFullyImported = true

        await _repo.SaveActivitySummariesAsync([MakeActivity("1") with { Name = "Renamed by sync" }]);

        var ids = (await _repo.GetStoredActivityIdsAsync()).ToList();
        Assert.Contains("1", ids); // toujours marquée comme importée après une simple synchro de liste

        var loaded = await _repo.GetActivityAsync("1");
        Assert.Equal("Renamed by sync", loaded!.Name); // les champs résumé sont bien mis à jour
    }

    [Fact]
    public async Task GetCachedActivitiesAsync_ReturnsAllActivities_OrderedByDateDescending()
    {
        await _repo.SaveActivitySummariesAsync([
            MakeActivity("1") with { Date = new DateTime(2026, 1, 1) },
            MakeActivity("2") with { Date = new DateTime(2026, 2, 1) }
        ]);

        var cached = (await _repo.GetCachedActivitiesAsync()).ToList();

        Assert.Equal(2, cached.Count);
        Assert.Equal("2", cached[0].Id); // la plus récente en premier
        Assert.Equal("1", cached[1].Id);
    }
}
