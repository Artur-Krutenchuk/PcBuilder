using PcBuilder.Web.Models;

namespace PcBuilder.Web.Repositories;

public interface ISavedBuildRepository
{
    Task<IReadOnlyList<SavedBuild>> GetAllAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedBuild>> GetPublicAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedBuild>> GetAllForAdminAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default);

    /// <summary>Removes a build only when <paramref name="id"/> matches and the build belongs to <paramref name="userId"/>.</summary>
    Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default);
}