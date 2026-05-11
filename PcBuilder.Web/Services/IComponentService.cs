using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Services;

public interface IComponentService
{
    Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken = default);
}
