using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Repositories;

public interface IComponentRepository
{
    Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken = default);

    Task SaveCatalogAsync(IReadOnlyList<Component> components, CancellationToken cancellationToken = default);
}
