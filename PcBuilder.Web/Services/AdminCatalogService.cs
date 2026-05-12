using PcBuilder.Web.Repositories;
using PcBuilder.Web.Models.Admin;

namespace PcBuilder.Web.Services;

public sealed class AdminCatalogService : IAdminCatalogService
{
    private readonly IComponentRepository _componentRepository;

    public AdminCatalogService(IComponentRepository componentRepository)
    {
        _componentRepository = componentRepository;
    }

    public async Task<IReadOnlyList<AdminComponentViewModel>> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var components = await _componentRepository.GetAllAsync(cancellationToken);
        return components
            .OrderBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Id)
            .Select(c => new AdminComponentViewModel(c.Id, c.Name, c.Manufacturer, c.Type))
            .ToList();
    }

    public async Task<AdminComponentViewModel?> GetComponentAsync(int id, CancellationToken cancellationToken = default)
    {
        var components = await _componentRepository.GetAllAsync(cancellationToken);
        var component = components.FirstOrDefault(c => c.Id == id);

        if (component is null)
        {
            return null;
        }

        return new AdminComponentViewModel(component.Id, component.Name, component.Manufacturer, component.Type);
    }

    public async Task CreateComponentAsync(AdminComponentEditorViewModel vm, CancellationToken cancellationToken = default)
    {
        var list = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var nextId = list.Count == 0 ? 1 : list.Max(c => c.Id) + 1;

        if (!AdminComponentMapper.TryToComponent(vm, nextId, out var component, out _))
        {
            throw new InvalidOperationException("Unable to create component.");
        }

        list.Add(component!);
        await _componentRepository.SaveCatalogAsync(list, cancellationToken);
    }

    public async Task UpdateComponentAsync(AdminComponentEditorViewModel vm, CancellationToken cancellationToken = default)
    {
        var list = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var index = list.FindIndex(c => c.Id == vm.Id);

        if (index < 0)
        {
            throw new KeyNotFoundException($"Component with id {vm.Id} not found.");
        }

        if (!AdminComponentMapper.TryToComponent(vm, vm.Id, out var component, out _))
        {
            throw new InvalidOperationException("Unable to update component.");
        }

        list[index] = component!;
        await _componentRepository.SaveCatalogAsync(list, cancellationToken);
    }

    public async Task DeleteComponentAsync(int id, CancellationToken cancellationToken = default)
    {
        var list = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var removed = list.RemoveAll(c => c.Id == id);

        if (removed == 0)
        {
            throw new KeyNotFoundException($"Component with id {id} not found.");
        }

        await _componentRepository.SaveCatalogAsync(list, cancellationToken);
    }
}
