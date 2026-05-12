using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.ViewModels;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

[Authorize]
public sealed class MyBuildsController : Controller
{
    private readonly ISavedBuildService _savedBuildService;
    private readonly IComponentService _componentService;

    public MyBuildsController(ISavedBuildService savedBuildService, IComponentService componentService)
    {
        _savedBuildService = savedBuildService;
        _componentService = componentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var builds = await _savedBuildService.GetAllAsync(userId, cancellationToken);
        var components = await _componentService.GetAllAsync(cancellationToken);
        var byId = components.ToDictionary(c => c.Id);

        var cards = builds
            .OrderByDescending(b => b.CreatedAtUtc)
            .Select(b => new MySavedBuildCardViewModel
            {
                Build = b,
                PartLines = BuildPartLines(b, byId)
            })
            .ToList();

        var model = new MyBuildsIndexViewModel { Builds = cards };
        ViewData["Title"] = "My Builds";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        var deleted = await _savedBuildService.DeleteAsync(id, userId, cancellationToken);
        if (!deleted)
        {
            TempData["MyBuildsMessage"] = "That build could not be deleted, or it does not belong to your account.";
        }

        return RedirectToAction(nameof(Index));
    }

    private static IReadOnlyList<string> BuildPartLines(SavedBuild build, IReadOnlyDictionary<int, Component> byId)
    {
        var lines = new List<string>(7);
        AddPartLine(lines, "CPU", build.CpuId, byId);
        AddPartLine(lines, "Motherboard", build.MotherboardId, byId);
        AddPartLine(lines, "RAM", build.RamId, byId);
        AddPartLine(lines, "GPU", build.GpuId, byId);
        AddPartLine(lines, "PSU", build.PsuId, byId);
        AddPartLine(lines, "Case", build.CaseId, byId);
        AddPartLine(lines, "Cooler", build.CoolerId, byId);
        return lines;
    }

    private static void AddPartLine(
        List<string> lines,
        string label,
        int? partId,
        IReadOnlyDictionary<int, Component> byId)
    {
        if (partId is null)
        {
            lines.Add($"{label}: —");
            return;
        }

        if (!byId.TryGetValue(partId.Value, out var component))
        {
            lines.Add($"{label}: (not in catalog)");
            return;
        }

        lines.Add($"{label}: {component.Name}");
    }
}
