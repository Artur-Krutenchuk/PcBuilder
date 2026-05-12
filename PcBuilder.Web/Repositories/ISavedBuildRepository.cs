using PcBuilder.Web.Models;

namespace PcBuilder.Web.Repositories;

public interface ISavedBuildRepository
{
    Task<IReadOnlyList<SavedBuild>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default);
}
