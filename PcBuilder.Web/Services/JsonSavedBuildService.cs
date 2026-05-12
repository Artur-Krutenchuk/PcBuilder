using PcBuilder.Web.Models;
using PcBuilder.Web.Repositories;

namespace PcBuilder.Web.Services;

public sealed class JsonSavedBuildService : ISavedBuildService
{
    private readonly ISavedBuildRepository _savedBuildRepository;

    public JsonSavedBuildService(ISavedBuildRepository savedBuildRepository)
    {
        _savedBuildRepository = savedBuildRepository;
    }

    public async Task<IReadOnlyList<SavedBuild>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _savedBuildRepository.GetAllAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<SavedBuild>> GetPublicGalleryAsync(CancellationToken cancellationToken = default)
    {
        return await _savedBuildRepository.GetPublicAsync(cancellationToken);
    }

    public async Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        await _savedBuildRepository.SaveAsync(build, cancellationToken);
    }

    public Task<bool> DeleteAsync(string id, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(false);
        }

        return _savedBuildRepository.DeleteAsync(id, userId, cancellationToken);
    }
}
