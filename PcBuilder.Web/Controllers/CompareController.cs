using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

[Authorize]
public sealed class CompareController : Controller
{
    private readonly IBuildComparisonService _buildComparisonService;

    public CompareController(IBuildComparisonService buildComparisonService)
    {
        _buildComparisonService = buildComparisonService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? buildAId, string? buildBId, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Challenge();
        }

        var model = await _buildComparisonService.BuildComparisonAsync(userId, buildAId, buildBId, cancellationToken);
        ViewData["Title"] = "Compare Builds";
        return View(model);
    }
}
