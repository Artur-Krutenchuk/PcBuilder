using PcBuilder.Web.Models;

namespace PcBuilder.Web.Services;

public interface ISavedBuildService
{
    Task<IReadOnlyList<SavedBuild>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default);
}
