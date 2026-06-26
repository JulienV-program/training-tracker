using Microsoft.EntityFrameworkCore;
using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;
using MyTracker.Infrastructure.Persistence.Entities;

namespace MyTracker.Infrastructure.Persistence;

public class SqliteUserProfileRepository(TrainingTrackerDbContext db) : IUserProfileRepository
{
    private const int SingletonId = 1;

    public async Task<UserProfile?> GetProfileAsync()
    {
        var entity = await db.UserProfile.FirstOrDefaultAsync(p => p.Id == SingletonId);
        if (entity == null) return null;

        return new UserProfile(
            entity.Age, entity.Sex, entity.HeightCm, entity.WeightKg, entity.MaxHeartRate, entity.RestingHeartRate,
            entity.FtpWatts, entity.VmaMinPerKm, entity.CriticalSwimSpeedMinPer100m);
    }

    public async Task SaveProfileAsync(UserProfile profile)
    {
        var entity = await db.UserProfile.FirstOrDefaultAsync(p => p.Id == SingletonId);
        if (entity == null)
        {
            entity = new UserProfileEntity { Id = SingletonId };
            db.UserProfile.Add(entity);
        }

        entity.Age = profile.Age;
        entity.Sex = profile.Sex;
        entity.HeightCm = profile.HeightCm;
        entity.WeightKg = profile.WeightKg;
        entity.MaxHeartRate = profile.MaxHeartRate;
        entity.RestingHeartRate = profile.RestingHeartRate;
        entity.FtpWatts = profile.FtpWatts;
        entity.VmaMinPerKm = profile.VmaMinPerKm;
        entity.CriticalSwimSpeedMinPer100m = profile.CriticalSwimSpeedMinPer100m;

        await db.SaveChangesAsync();
    }
}
