using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Auth;
using PcBuilder.Web.Models.ViewModels;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

[Authorize]
public sealed class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISavedBuildService _savedBuildService;
    private readonly IProfileService _profileService;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ISavedBuildService savedBuildService,
        IProfileService profileService)
    {
        _userManager = userManager;
        _savedBuildService = savedBuildService;
        _profileService = profileService;
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
        var favoriteCategory = _profileService.ComputeFavoriteCategory(builds);
        var averagePrice = _profileService.ComputeAverageBudget(builds);
        var (bestCompat, avgCompat) = ComputeCompatibilityStats(builds);
        var lastCreated = builds.Count == 0
            ? (DateTime?)null
            : builds.Max(b => b.CreatedAtUtc);
        var recent = builds.OrderByDescending(b => b.CreatedAtUtc).Take(8).ToList();

        var model = new ProfileDashboardViewModel
        {
            UserName = user.UserName ?? user.Email ?? "User",
            Email = user.Email ?? string.Empty,
            SavedBuildsCount = builds.Count,
            AverageBuildPrice = averagePrice,
            FavoriteBuildCategory = favoriteCategory,
            BestCompatibilityPercentage = bestCompat,
            AverageCompatibilityPercentage = avgCompat,
            LastBuildCreatedAt = lastCreated,
            RecentSavedBuilds = recent
        };

        ViewData["Title"] = "Profile";
        return View(model);
    }

    private static (int Best, int Average) ComputeCompatibilityStats(IReadOnlyList<SavedBuild> builds)
    {
        if (builds.Count == 0)
        {
            return (0, 0);
        }

        var best = builds.Max(b => b.CompatibilityPercentage);
        var avg = (int)Math.Round(builds.Average(b => b.CompatibilityPercentage), MidpointRounding.AwayFromZero);
        return (best, avg);
    }
}
