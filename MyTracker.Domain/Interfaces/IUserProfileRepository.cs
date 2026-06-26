using MyTracker.Domain.Models;

namespace MyTracker.Domain.Interfaces;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetProfileAsync();
    Task SaveProfileAsync(UserProfile profile);
}
