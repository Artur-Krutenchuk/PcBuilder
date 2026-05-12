using PcBuilder.Web.Repositories;
using PcBuilder.Web.Models.Admin;

namespace PcBuilder.Web.Services;

public interface IAdminCatalogService
{
    Task<IReadOnlyList<AdminComponentViewModel>> GetCatalogAsync(CancellationToken cancellationToken = default);

    Task<AdminComponentViewModel?> GetComponentAsync(int id, CancellationToken cancellationToken = default);

    Task CreateComponentAsync(AdminComponentEditorViewModel vm, CancellationToken cancellationToken = default);

    Task UpdateComponentAsync(AdminComponentEditorViewModel vm, CancellationToken cancellationToken = default);

    Task DeleteComponentAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record AdminComponentViewModel(
    int Id,
    string Name,
    string Manufacturer,
    string Type);
