using Microsoft.EntityFrameworkCore;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.Persistence.Entities;

namespace MyTracker.Infrastructure.Persistence;

public class SqliteActivityRepository(TrainingTrackerDbContext db) : IActivityRepository
{
    public async Task<bool> ExistsAsync(string activityId)
        => await db.Activities.AnyAsync(a => a.Id == activityId);

    public async Task SaveActivityAsync(Activity activity)
    {
        var entity = await db.Activities.FindAsync(activity.Id);
        if (entity == null)
        {
            entity = new ActivityEntity { Id = activity.Id };
            db.Activities.Add(entity);
        }

        entity.Name = activity.Name;
        entity.Date = activity.Date;
        entity.StartDateLocal = activity.StartDateLocal;
        entity.Type = activity.Type;
        entity.Distance = activity.Distance;
        entity.MovingTime = activity.MovingTime;
        entity.ElevationGain = activity.ElevationGain;
        entity.AverageHeartRate = activity.AverageHeartRate;
        entity.MaxHeartRate = activity.MaxHeartRate;
        entity.AverageWatts = activity.AverageWatts;
        entity.MaxWatts = activity.MaxWatts;
        entity.AverageCadence = activity.AverageCadence;
        entity.Calories = activity.Calories;
        entity.SufferScore = activity.SufferScore;
        entity.IsFullyImported = true;

        await db.SaveChangesAsync();
    }

    public async Task SaveActivitySummariesAsync(IEnumerable<Activity> activities)
    {
        foreach (var activity in activities)
        {
            var entity = await db.Activities.FindAsync(activity.Id);
            if (entity == null)
            {
                entity = new ActivityEntity { Id = activity.Id };
                db.Activities.Add(entity);
            }

            entity.Name = activity.Name;
            entity.Date = activity.Date;
            entity.Type = activity.Type;
            entity.Distance = activity.Distance;
            entity.MovingTime = activity.MovingTime;
            entity.ElevationGain = activity.ElevationGain;
            entity.AverageHeartRate = activity.AverageHeartRate;
            entity.MaxHeartRate = activity.MaxHeartRate;
            entity.AverageWatts = activity.AverageWatts;
            entity.MaxWatts = activity.MaxWatts;
            entity.AverageCadence = activity.AverageCadence;
            entity.Calories = activity.Calories;
            entity.SufferScore = activity.SufferScore;
            // IsFullyImported volontairement non modifié : une simple synchro de liste ne doit jamais
            // "downgrader" une activité déjà importée en détail (streams/laps/splits).
        }

        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<Activity>> GetCachedActivitiesAsync()
    {
        return await db.Activities
            .OrderByDescending(a => a.Date)
            .Select(entity => new Activity(
                entity.Id,
                entity.Name,
                entity.Date,
                entity.Type,
                entity.Distance,
                entity.MovingTime,
                entity.ElevationGain,
                entity.AverageHeartRate,
                entity.MaxHeartRate,
                entity.AverageWatts,
                entity.MaxWatts,
                entity.AverageCadence,
                entity.Calories,
                entity.SufferScore,
                entity.AverageSpeed,
                entity.MaxSpeed,
                entity.StartLat,
                entity.StartLng,
                entity.EndLat,
                entity.EndLng,
                entity.MapPolyline))
            .ToListAsync();
    }

    public async Task SaveActivityDetailAsync(ActivityDetail detail)
    {
        var entity = await db.Activities.FindAsync(detail.ActivityId)
            ?? throw new Exception("Activité introuvable en cache pour enregistrer le détail.");

        entity.AverageSpeed = detail.AverageSpeed;
        entity.MaxSpeed = detail.MaxSpeed;
        entity.StartLat = detail.StartLat;
        entity.StartLng = detail.StartLng;
        entity.EndLat = detail.EndLat;
        entity.EndLng = detail.EndLng;
        entity.MapPolyline = detail.MapPolyline;

        db.ActivityLaps.RemoveRange(db.ActivityLaps.Where(l => l.ActivityId == detail.ActivityId));
        db.ActivitySplits.RemoveRange(db.ActivitySplits.Where(s => s.ActivityId == detail.ActivityId));

        await db.ActivityLaps.AddRangeAsync(detail.Laps.Select(l => new ActivityLapEntity
        {
            ActivityId = detail.ActivityId,
            StravaLapId = l.LapId,
            LapIndex = l.LapIndex,
            Name = l.Name,
            ElapsedTime = l.ElapsedTime,
            MovingTime = l.MovingTime,
            StartDate = l.StartDate,
            Distance = l.Distance,
            AverageSpeed = l.AverageSpeed,
            MaxSpeed = l.MaxSpeed,
            AverageHeartRate = l.AverageHeartRate,
            MaxHeartRate = l.MaxHeartRate,
            ElevationGain = l.ElevationGain
        }));

        await db.ActivitySplits.AddRangeAsync(detail.Splits.Select(s => new ActivitySplitEntity
        {
            ActivityId = detail.ActivityId,
            SplitIndex = s.SplitIndex,
            Distance = s.Distance,
            ElapsedTime = s.ElapsedTime,
            ElevationDifference = s.ElevationDifference,
            MovingTime = s.MovingTime,
            AverageSpeed = s.AverageSpeed,
            AverageHeartRate = s.AverageHeartRate,
            PaceZone = s.PaceZone
        }));

        await db.SaveChangesAsync();
    }

    public async Task SaveDataPointsAsync(string activityId, IEnumerable<ActivityDataPoint> points)
    {
        var existing = db.ActivityDataPoints.Where(p => p.ActivityId == activityId);
        db.ActivityDataPoints.RemoveRange(existing);

        var entities = points.Select(p => new ActivityDataPointEntity
        {
            ActivityId = activityId,
            TimeOffset = p.TimeOffset,
            Distance = p.Distance,
            HeartRate = p.HeartRate,
            Watts = p.Watts,
            Cadence = p.Cadence,
            Altitude = p.Altitude,
            Grade = p.Grade,
            Velocity = p.Velocity,
            Lat = p.Lat,
            Lng = p.Lng,
            Moving = p.Moving,
            Temperature = p.Temperature
        });

        await db.ActivityDataPoints.AddRangeAsync(entities);
        await db.SaveChangesAsync();
    }

    public async Task<Activity?> GetActivityAsync(string activityId)
    {
        var entity = await db.Activities.FindAsync(activityId);
        if (entity == null) return null;

        return new Activity(
            entity.Id,
            entity.Name,
            entity.Date,
            entity.Type,
            entity.Distance,
            entity.MovingTime,
            entity.ElevationGain,
            entity.AverageHeartRate,
            entity.MaxHeartRate,
            entity.AverageWatts,
            entity.MaxWatts,
            entity.AverageCadence,
            entity.Calories,
            entity.SufferScore,
            entity.AverageSpeed,
            entity.MaxSpeed,
            entity.StartLat,
            entity.StartLng,
            entity.EndLat,
            entity.EndLng,
            entity.MapPolyline)
        {
            StartDateLocal = entity.StartDateLocal
        };
    }

    public async Task<IEnumerable<ActivityDataPoint>> GetDataPointsAsync(string activityId)
    {
        return await db.ActivityDataPoints
            .Where(p => p.ActivityId == activityId)
            .OrderBy(p => p.TimeOffset)
            .Select(p => new ActivityDataPoint(
                p.TimeOffset,
                p.Distance,
                p.HeartRate,
                p.Watts,
                p.Cadence,
                p.Altitude,
                p.Grade,
                p.Velocity,
                p.Lat,
                p.Lng,
                p.Moving,
                p.Temperature))
            .ToListAsync();
    }

    public async Task<IEnumerable<string>> GetStoredActivityIdsAsync()
        => await db.Activities.Where(a => a.IsFullyImported).Select(a => a.Id).ToListAsync();

    public async Task<IEnumerable<ActivityLap>> GetLapsAsync(string activityId)
        => await db.ActivityLaps
            .Where(l => l.ActivityId == activityId)
            .OrderBy(l => l.LapIndex)
            .Select(l => new ActivityLap(
                l.StravaLapId, l.ActivityId, l.LapIndex, l.Name, l.ElapsedTime, l.MovingTime,
                l.StartDate, l.Distance, l.AverageSpeed, l.MaxSpeed, l.AverageHeartRate, l.MaxHeartRate, l.ElevationGain))
            .ToListAsync();

    public async Task<IEnumerable<ActivitySplit>> GetSplitsAsync(string activityId)
        => await db.ActivitySplits
            .Where(s => s.ActivityId == activityId)
            .OrderBy(s => s.SplitIndex)
            .Select(s => new ActivitySplit(
                s.ActivityId, s.SplitIndex, s.Distance, s.ElapsedTime, s.ElevationDifference,
                s.MovingTime, s.AverageSpeed, s.AverageHeartRate, s.PaceZone))
            .ToListAsync();
}
