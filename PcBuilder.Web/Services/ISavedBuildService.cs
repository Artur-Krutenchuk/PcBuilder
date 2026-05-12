using PcBuilder.Web.Models;

namespace PcBuilder.Web.Services;

public interface ISavedBuildService
{
    Task<IReadOnlyList<SavedBuild>> GetAllAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedBuild>> GetPublicGalleryAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default);
}
