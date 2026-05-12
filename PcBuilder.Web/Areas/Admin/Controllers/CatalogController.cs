using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models.Admin;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Repositories;

namespace PcBuilder.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public sealed class CatalogController : Controller
{
    private readonly IComponentRepository _componentRepository;

    public CatalogController(IComponentRepository componentRepository)
    {
        _componentRepository = componentRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var components = await _componentRepository.GetAllAsync(cancellationToken);
        var ordered = components.OrderBy(c => c.Type, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id).ToList();
        return View(ordered);
    }

    [HttpGet]
    public IActionResult Create(string? type = null)
    {
        var vm = new AdminComponentEditorViewModel();
        if (!string.IsNullOrWhiteSpace(type))
        {
            vm.ComponentType = type.Trim().ToLowerInvariant();
        }

        return View("Editor", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminComponentEditorViewModel vm, CancellationToken cancellationToken)
    {
        NormalizeEditor(vm);
        if (!ModelState.IsValid)
        {
            return View("Editor", vm);
        }

        var list = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var nextId = list.Count == 0 ? 1 : list.Max(c => c.Id) + 1;

        if (!AdminComponentMapper.TryToComponent(vm, nextId, out var component, out var error))
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to create component.");
            return View("Editor", vm);
        }

        list.Add(component!);
        await _componentRepository.SaveCatalogAsync(list, cancellationToken);
        TempData["AdminMessage"] = "Component created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var list = await _componentRepository.GetAllAsync(cancellationToken);
        var existing = list.FirstOrDefault(c => c.Id == id);
        if (existing is null)
        {
            return NotFound();
        }

        var vm = AdminComponentMapper.FromComponent(existing);
        return View("Editor", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminComponentEditorViewModel vm, CancellationToken cancellationToken)
    {
        NormalizeEditor(vm);
        if (!ModelState.IsValid)
        {
            return View("Editor", vm);
        }

        var list = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var index = list.FindIndex(c => c.Id == vm.Id);
        if (index < 0)
        {
            return NotFound();
        }

        if (!AdminComponentMapper.TryToComponent(vm, vm.Id, out var component, out var error))
        {
            ModelState.AddModelError(string.Empty, error ?? "Unable to save component.");
            return View("Editor", vm);
        }

        list[index] = component!;
        await _componentRepository.SaveCatalogAsync(list, cancellationToken);
        TempData["AdminMessage"] = "Component updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var list = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var removed = list.RemoveAll(c => c.Id == id);
        if (removed == 0)
        {
            return NotFound();
        }

        await _componentRepository.SaveCatalogAsync(list, cancellationToken);
        TempData["AdminMessage"] = "Component deleted.";
        return RedirectToAction(nameof(Index));
    }

    private static void NormalizeEditor(AdminComponentEditorViewModel vm)
    {
        vm.ComponentType = (vm.ComponentType ?? string.Empty).Trim().ToLowerInvariant();
        vm.Name = vm.Name?.Trim() ?? string.Empty;
        vm.Manufacturer = vm.Manufacturer?.Trim() ?? string.Empty;
    }
}
