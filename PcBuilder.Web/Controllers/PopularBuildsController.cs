using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.ViewModels;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

public sealed class PopularBuildsController : Controller
{
    private readonly IComponentService _componentService;
    private readonly ICompatibilityService _compatibilityService;
    private readonly IBuildService _buildService;

    /// <summary>
    /// Curated presets using component IDs from Data/buildcores.json.
    /// </summary>
    private static readonly PopularBuildDefinition[] Definitions =
    [
        new(
            "Budget Gaming PC",
            "Great 1080p performance: Core i5-12400F, B760 DDR4, RTX 4060, 16 GB RAM.",
            "success",
            5, 108, 201, 301, 401, 505, 601),
        new(
            "Mid-range Gaming PC",
            "Strong 1080p/1440p balance: Ryzen 7 5800X, B550 Tomahawk, RTX 4060 Ti, 32 GB DDR4.",
            "primary",
            2, 102, 202, 302, 402, 508, 605),
        new(
            "High-end Gaming PC",
            "Enthusiast frame rates: Ryzen 7 7800X3D, B650 AORUS, RX 7800 XT, 32 GB DDR5.",
            "danger",
            4, 106, 207, 305, 407, 510, 602),
        new(
            "Streaming PC",
            "Encode-friendly: Core i7-12700K, Z790, RTX 4070, 32 GB DDR5, 240 mm AIO.",
            "info",
            7, 107, 205, 303, 403, 507, 604),
        new(
            "Workstation PC",
            "Creation muscle: Core i7-14700K, Z790, RTX 4080, 64 GB DDR5 Dominator.",
            "dark",
            8, 107, 208, 307, 408, 502, 603),
        new(
            "Office PC",
            "Efficient daily driver: Ryzen 5 5600X, B550M DS3H, RTX 4060, 16 GB DDR4.",
            "secondary",
            1, 103, 201, 301, 404, 504, 606)
    ];

    public PopularBuildsController(
        IComponentService componentService,
        ICompatibilityService compatibilityService,
        IBuildService buildService)
    {
        _componentService = componentService;
        _compatibilityService = compatibilityService;
        _buildService = buildService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var cards = new List<PopularBuildCardViewModel>(Definitions.Length);
        foreach (var def in Definitions)
        {
            var card = TryBuildCard(def, components);
            if (card is not null)
            {
                cards.Add(card);
            }
        }

        ViewData["Title"] = "Popular Builds";
        return View(cards);
    }

    private PopularBuildCardViewModel? TryBuildCard(PopularBuildDefinition def, IReadOnlyList<Component> components)
    {
        var build = new SelectedBuild
        {
            CpuId = def.CpuId,
            MotherboardId = def.MotherboardId,
            RamId = def.RamId,
            GpuId = def.GpuId,
            PsuId = def.PsuId,
            CaseId = def.CaseId,
            CoolerId = def.CoolerId
        };

        _buildService.HydrateBuild(build, components);
        if (build.Cpu is null || build.Motherboard is null || build.Ram is null || build.Gpu is null || build.Psu is null)
        {
            return null;
        }

        var result = _compatibilityService.Check(build);
        var totalPrice = _buildService.CalculateTotalPrice(build);
        var estimatedWattage = _buildService.CalculateEstimatedWattage(build);
        if (estimatedWattage == 0)
        {
            estimatedWattage = result.EstimatedSystemWattage;
        }

        return new PopularBuildCardViewModel
        {
            Name = def.Name,
            Category = result.BuildCategory,
            TotalPrice = totalPrice,
            CompatibilityPercentage = result.CompatibilityPercentage,
            EstimatedFps1080p = result.EstimatedFps1080p,
            EstimatedWattage = estimatedWattage,
            Badge = def.Badge,
            Description = def.Description,
            CpuId = def.CpuId.ToString(CultureInfo.InvariantCulture),
            MotherboardId = def.MotherboardId.ToString(CultureInfo.InvariantCulture),
            RamId = def.RamId.ToString(CultureInfo.InvariantCulture),
            GpuId = def.GpuId.ToString(CultureInfo.InvariantCulture),
            PsuId = def.PsuId.ToString(CultureInfo.InvariantCulture),
            CaseId = def.CaseId.ToString(CultureInfo.InvariantCulture)
        };
    }

    private sealed record PopularBuildDefinition(
        string Name,
        string Description,
        string Badge,
        int CpuId,
        int MotherboardId,
        int RamId,
        int GpuId,
        int PsuId,
        int CaseId,
        int CoolerId);
}
