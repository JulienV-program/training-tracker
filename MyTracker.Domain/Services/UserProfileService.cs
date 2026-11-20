using MyTracker.Domain.Interfaces;
using MyTracker.Domain.Models;

namespace MyTracker.Domain.Services;

/// <summary>Profil physiologique de l'athlète (utilisé pour contextualiser l'analyse IA).</summary>
public class UserProfileService(IUserProfileRepository repo)
{
    public Task<UserProfile?> GetProfileAsync() => repo.GetProfileAsync();

    public Task SaveProfileAsync(UserProfile profile) => repo.SaveProfileAsync(profile);
}
