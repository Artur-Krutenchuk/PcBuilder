using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Repositories;

namespace PcBuilder.Web.Services;

public sealed class JsonComponentService : IComponentService
{
    private readonly IComponentRepository _componentRepository;

    public JsonComponentService(IComponentRepository componentRepository)
    {
        _componentRepository = componentRepository;
    }

    public async Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _componentRepository.GetAllAsync(cancellationToken);
    }
}
