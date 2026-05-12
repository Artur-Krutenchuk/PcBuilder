using PcBuilder.Web.Models;

namespace PcBuilder.Web.Repositories;

public interface ISavedBuildRepository
{
    Task<IReadOnlyList<SavedBuild>> GetAllAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedBuild>> GetPublicAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedBuild>> GetAllForAdminAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default);
}
