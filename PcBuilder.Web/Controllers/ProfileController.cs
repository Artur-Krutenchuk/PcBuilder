using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Auth;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISavedBuildService _savedBuildService;
    private readonly IComponentService _componentService;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ISavedBuildService savedBuildService,
        IComponentService componentService)
    {
        _userManager = userManager;
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

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return NotFound();
        }

        var builds = (await _savedBuildService.GetAllAsync(userId, cancellationToken)).ToList();
        var components = await _componentService.GetAllAsync(cancellationToken);
        var componentById = components.ToDictionary(c => c.Id);

        var favoriteCategory = ComputeFavoriteCategory(builds);
        var avgBudget = builds.Count > 0 ? builds.Average(b => b.TotalPrice) : 0m;
        var manufacturerChart = ComputeManufacturerChart(builds, componentById);
        var recent = builds.OrderByDescending(b => b.CreatedAtUtc).Take(8).ToList();

        var model = new ProfileIndexViewModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            Email = user.Email ?? string.Empty,
            RegisteredAtUtc = user.RegisteredAtUtc,
            SavedBuildCount = builds.Count,
            FavoriteCategory = favoriteCategory,
            AverageBuildBudget = Math.Round(avgBudget, 2, MidpointRounding.AwayFromZero),
            RecentSavedBuilds = recent,
            ManufacturerChartData = manufacturerChart
        };

        return View(model);
    }

    private static string ComputeFavoriteCategory(List<SavedBuild> builds)
    {
        if (builds.Count == 0)
        {
            return "—";
        }

        var grouped = builds
            .GroupBy(b => string.IsNullOrWhiteSpace(b.BuildCategory) ? "Uncategorized" : b.BuildCategory.Trim())
            .OrderByDescending(g => g.Count())
            .First();

        return grouped.Key;
    }

    private static IReadOnlyList<ManufacturerChartItem> ComputeManufacturerChart(
        List<SavedBuild> builds,
        IReadOnlyDictionary<int, Component> componentById)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var build in builds)
        {
            foreach (var id in new int?[]
                     {
                         build.CpuId, build.MotherboardId, build.RamId, build.GpuId, build.PsuId, build.CaseId,
                         build.CoolerId
                     })
            {
                if (id is null || !componentById.TryGetValue(id.Value, out var component))
                {
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(component.Manufacturer)
                    ? "Unknown"
                    : component.Manufacturer.Trim();

                counts[name] = counts.GetValueOrDefault(name) + 1;
            }
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .Take(12)
            .Select(kv => new ManufacturerChartItem { Name = kv.Key, Count = kv.Value })
            .ToList();
    }
}
