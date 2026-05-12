using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;
using PcBuilder.Web.Services;
using System.Text;
using System.Text.Json;
using System.Security.Claims;

namespace PcBuilder.Web.Controllers;

public sealed class BuilderController : Controller
{
    private readonly IComponentService _componentService;
    private readonly ICompatibilityService _compatibilityService;
    private readonly ISavedBuildService _savedBuildService;
    private readonly IBuildService _buildService;

    public BuilderController(
        IComponentService componentService,
        ICompatibilityService compatibilityService,
        ISavedBuildService savedBuildService,
        IBuildService buildService)
    {
        _componentService = componentService;
        _compatibilityService = compatibilityService;
        _savedBuildService = savedBuildService;
        _buildService = buildService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? cpuId,
        int? motherboardId,
        int? ramId,
        int? gpuId,
        int? psuId,
        int? caseId,
        int? coolerId,
        CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var savedBuilds = User.Identity?.IsAuthenticated == true
            ? await _savedBuildService.GetAllAsync(GetUserId(), cancellationToken)
            : [];
        var selection = new SelectedBuild
        {
            CpuId = cpuId,
            MotherboardId = motherboardId,
            RamId = ramId,
            GpuId = gpuId,
            PsuId = psuId,
            CaseId = caseId,
            CoolerId = coolerId
        };
        _buildService.HydrateBuild(selection, components);
        var compatibilityPreview = selection.CpuId is not null || selection.GpuId is not null
            ? _compatibilityService.Check(selection)
            : null;
        var totalPrice = _buildService.CalculateTotalPrice(selection);
        var estimatedWattage = _buildService.CalculateEstimatedWattage(selection);
        var model = BuildViewModel(
            components,
            savedBuilds,
            selection,
            compatibilityPreview,
            totalPrice,
            estimatedWattage == 0 && compatibilityPreview is not null
                ? compatibilityPreview.EstimatedSystemWattage
                : estimatedWattage);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SelectedBuild build, CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var savedBuilds = User.Identity?.IsAuthenticated == true
            ? await _savedBuildService.GetAllAsync(GetUserId(), cancellationToken)
            : [];
        _buildService.HydrateBuild(build, components);
        var compatibilityResult = _compatibilityService.Check(build);
        var model = BuildViewModel(
            components,
            savedBuilds,
            build,
            compatibilityResult,
            _buildService.CalculateTotalPrice(build),
            _buildService.CalculateEstimatedWattage(build));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate([FromForm] SelectedBuild build, CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        _buildService.HydrateBuild(build, components);

        var compatibilityResult = _compatibilityService.Check(build);
        var totalPrice = _buildService.CalculateTotalPrice(build);
        var estimatedWattage = _buildService.CalculateEstimatedWattage(build);

        return Json(new
        {
            compatibilityResult.IsValid,
            compatibilityResult.Errors,
            compatibilityResult.Warnings,
            compatibilityResult.CompatibilityPercentage,
            compatibilityResult.BuildCategory,
            compatibilityResult.EstimatedFps1080p,
            compatibilityResult.EstimatedFps1440p,
            compatibilityResult.EstimatedFps4k,
            compatibilityResult.EstimatedRayTracingFps,
            compatibilityResult.EstimatedCompetitiveFps,
            compatibilityResult.EstimatedAaaFps,
            compatibilityResult.EstimatedCpuTemperatureCelsius,
            compatibilityResult.EstimatedGpuTemperatureCelsius,
            compatibilityResult.PsuHealthScore,
            compatibilityResult.ThermalHealthScore,
            compatibilityResult.EfficiencyScore,
            compatibilityResult.BuildBadges,
            compatibilityResult.Recommendations,
            compatibilityResult.CpuBottleneckPercentage,
            compatibilityResult.GpuBottleneckPercentage,
            TotalPrice = totalPrice,
            EstimatedWattage = estimatedWattage == 0 ? compatibilityResult.EstimatedSystemWattage : estimatedWattage,
            SelectedNames = new
            {
                Cpu = build.Cpu?.Name,
                Motherboard = build.Motherboard?.Name,
                Ram = build.Ram?.Name,
                Gpu = build.Gpu?.Name,
                Psu = build.Psu?.Name,
                Case = build.Case?.Name,
                Cooler = build.Cooler?.Name
            }
        });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveBuild(
        [FromForm] SelectedBuild build,
        [FromForm] string? buildName,
        [FromForm] string? description,
        [FromForm] bool isPublic,
        CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        _buildService.HydrateBuild(build, components);

        var compatibilityResult = _compatibilityService.Check(build);
        var totalPrice = _buildService.CalculateTotalPrice(build);
        var estimatedWattage = _buildService.CalculateEstimatedWattage(build);

        var trimmedName = string.IsNullOrWhiteSpace(buildName)
            ? string.Empty
            : buildName.Trim();
        if (trimmedName.Length > 120)
        {
            trimmedName = trimmedName[..120];
        }

        var trimmedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        if (trimmedDescription?.Length > 500)
        {
            trimmedDescription = trimmedDescription[..500];
        }

        var savedBuild = new SavedBuild
        {
            UserId = GetUserId(),
            Name = trimmedName,
            BuildName = trimmedName,
            Description = trimmedDescription,
            IsPublic = isPublic,
            CreatorUserName = User.Identity?.Name ?? string.Empty,
            EstimatedFps1080p = compatibilityResult.EstimatedFps1080p,
            EstimatedFps1440p = compatibilityResult.EstimatedFps1440p,
            EstimatedFps4k = compatibilityResult.EstimatedFps4k,
            CpuBottleneckPercentage = compatibilityResult.CpuBottleneckPercentage,
            GpuBottleneckPercentage = compatibilityResult.GpuBottleneckPercentage,
            EfficiencyScore = compatibilityResult.EfficiencyScore,
            ThermalScore = compatibilityResult.ThermalHealthScore,
            PsuHealthScore = compatibilityResult.PsuHealthScore,
            CpuId = build.CpuId,
            MotherboardId = build.MotherboardId,
            RamId = build.RamId,
            GpuId = build.GpuId,
            PsuId = build.PsuId,
            CaseId = build.CaseId,
            CoolerId = build.CoolerId,
            TotalPrice = totalPrice,
            EstimatedWattage = estimatedWattage == 0 ? compatibilityResult.EstimatedSystemWattage : estimatedWattage,
            BuildCategory = compatibilityResult.BuildCategory,
            CompatibilityPercentage = compatibilityResult.CompatibilityPercentage,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _savedBuildService.SaveAsync(savedBuild, cancellationToken);
        return Json(new
        {
            saved = true,
            message = "Build saved successfully.",
            build = savedBuild
        });
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> SavedBuilds(CancellationToken cancellationToken)
    {
        var builds = await _savedBuildService.GetAllAsync(GetUserId(), cancellationToken);
        return Json(builds);
    }

    [HttpGet]
    public async Task<IActionResult> CompatibleOptions(
        int? cpuId,
        int? motherboardId,
        int? ramId,
        int? gpuId,
        int? psuId,
        CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var cpus = components.OfType<Cpu>().ToList();
        var motherboards = components.OfType<Motherboard>().ToList();
        var rams = components.OfType<Ram>().ToList();
        var gpus = components.OfType<Gpu>().ToList();
        var psus = components.OfType<Psu>().ToList();

        var selectedCpu = cpus.FirstOrDefault(component => component.Id == cpuId);
        var selectedMotherboard = motherboards.FirstOrDefault(component => component.Id == motherboardId);
        var selectedRam = rams.FirstOrDefault(component => component.Id == ramId);
        var selectedGpu = gpus.FirstOrDefault(component => component.Id == gpuId);

        var compatibleMotherboardIds = motherboards
            .Where(motherboard => selectedCpu is null
                                  || string.Equals(motherboard.Socket, selectedCpu.Socket, StringComparison.OrdinalIgnoreCase))
            .Select(motherboard => motherboard.Id)
            .ToList();

        var compatibleRamIds = rams
            .Where(ram => selectedMotherboard is null
                          || selectedMotherboard.SupportedRamTypes.Any(type =>
                              string.Equals(type, ram.RamType, StringComparison.OrdinalIgnoreCase)))
            .Select(ram => ram.Id)
            .ToList();

        var estimatedWattage = _buildService.CalculateEstimatedWattage(
            selectedCpu?.TdpWatts ?? 0,
            selectedGpu?.TdpWatts ?? 0,
            selectedRam?.CapacityGb ?? 0);
        var recommendedPsuThreshold = estimatedWattage == 0
            ? 0
            : (int)Math.Ceiling(estimatedWattage * 1.2m);

        var recommendedPsuIds = psus
            .Where(psu => recommendedPsuThreshold == 0 || psu.Wattage >= recommendedPsuThreshold)
            .Select(psu => psu.Id)
            .ToList();

        return Json(new
        {
            CompatibleMotherboardIds = compatibleMotherboardIds,
            CompatibleRamIds = compatibleRamIds,
            RecommendedPsuIds = recommendedPsuIds,
            SelectedPsuId = psuId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Compare(
        int? currentCpuId,
        int? currentMotherboardId,
        int? currentRamId,
        int? currentGpuId,
        int? currentPsuId,
        int? currentCaseId,
        int? currentCoolerId,
        int? compareCpuId,
        int? compareMotherboardId,
        int? compareRamId,
        int? compareGpuId,
        int? comparePsuId,
        int? compareCaseId,
        int? compareCoolerId,
        CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);

        var buildA = new SelectedBuild
        {
            CpuId = currentCpuId,
            MotherboardId = currentMotherboardId,
            RamId = currentRamId,
            GpuId = currentGpuId,
            PsuId = currentPsuId,
            CaseId = currentCaseId,
            CoolerId = currentCoolerId
        };

        var buildB = new SelectedBuild
        {
            CpuId = compareCpuId,
            MotherboardId = compareMotherboardId,
            RamId = compareRamId,
            GpuId = compareGpuId,
            PsuId = comparePsuId,
            CaseId = compareCaseId,
            CoolerId = compareCoolerId
        };

        _buildService.HydrateBuild(buildA, components);
        _buildService.HydrateBuild(buildB, components);

        var result = _compatibilityService.CompareBuilds(buildA, buildB);
        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportBuild(
        int? cpuId,
        int? motherboardId,
        int? ramId,
        int? gpuId,
        int? psuId,
        int? caseId,
        int? coolerId,
        string? format,
        CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var build = new SelectedBuild
        {
            CpuId = cpuId,
            MotherboardId = motherboardId,
            RamId = ramId,
            GpuId = gpuId,
            PsuId = psuId,
            CaseId = caseId,
            CoolerId = coolerId
        };

        _buildService.HydrateBuild(build, components);
        var compatibility = _compatibilityService.Check(build);
        var totalPrice = _buildService.CalculateTotalPrice(build);
        var estimatedWattage = _buildService.CalculateEstimatedWattage(build);
        var wattage = estimatedWattage == 0 ? compatibility.EstimatedSystemWattage : estimatedWattage;

        var export = new
        {
            GeneratedAtUtc = DateTime.UtcNow,
            SelectedParts = new
            {
                Cpu = build.Cpu is null ? null : new { build.Cpu.Id, build.Cpu.Name, build.Cpu.Manufacturer, build.Cpu.Price, build.Cpu.Socket, build.Cpu.TdpWatts },
                Motherboard = build.Motherboard is null ? null : new { build.Motherboard.Id, build.Motherboard.Name, build.Motherboard.Manufacturer, build.Motherboard.Price, build.Motherboard.Socket, build.Motherboard.FormFactor, build.Motherboard.Chipset },
                Ram = build.Ram is null ? null : new { build.Ram.Id, build.Ram.Name, build.Ram.Manufacturer, build.Ram.Price, build.Ram.RamType, build.Ram.CapacityGb, build.Ram.FrequencyMhz },
                Gpu = build.Gpu is null ? null : new { build.Gpu.Id, build.Gpu.Name, build.Gpu.Manufacturer, build.Gpu.Price, build.Gpu.VramGb, build.Gpu.TdpWatts, build.Gpu.LengthMm },
                Psu = build.Psu is null ? null : new { build.Psu.Id, build.Psu.Name, build.Psu.Manufacturer, build.Psu.Price, build.Psu.Wattage, build.Psu.EfficiencyRating },
                Case = build.Case is null ? null : new { build.Case.Id, build.Case.Name, build.Case.Manufacturer, build.Case.Price, build.Case.MaxGpuLengthMm, build.Case.SupportedMotherboardSizes, build.Case.IncludedFans, build.Case.AirflowRating },
                Cooler = build.Cooler is null ? null : new { build.Cooler.Id, build.Cooler.Name, build.Cooler.Manufacturer, build.Cooler.Price, build.Cooler.SupportedSockets, build.Cooler.CoolingCapacityWatts, build.Cooler.NoiseLevelDb }
            },
            Totals = new
            {
                TotalPrice = totalPrice,
                EstimatedWattage = wattage
            },
            Compatibility = new
            {
                compatibility.IsValid,
                compatibility.Errors,
                compatibility.Warnings,
                compatibility.CompatibilityPercentage,
                compatibility.BuildCategory,
                FpsEstimates = new
                {
                    compatibility.EstimatedFps1080p,
                    compatibility.EstimatedFps1440p,
                    compatibility.EstimatedFps4k,
                    compatibility.EstimatedRayTracingFps,
                    compatibility.EstimatedCompetitiveFps,
                    compatibility.EstimatedAaaFps
                },
                Recommendations = compatibility.Recommendations,
                BottleneckAnalysis = new
                {
                    compatibility.CpuBottleneckPercentage,
                    compatibility.GpuBottleneckPercentage
                }
            }
        };

        var normalizedFormat = (format ?? "json").Trim().ToLowerInvariant();
        return normalizedFormat switch
        {
            "txt" or "text" => File(Encoding.UTF8.GetBytes(_buildService.ExportBuildAsText(build, compatibility, totalPrice, wattage)), "text/plain; charset=utf-8", "pc-build.txt"),
            _ => File(JsonSerializer.SerializeToUtf8Bytes(_buildService.ExportBuild(build, compatibility, totalPrice, wattage), new JsonSerializerOptions { WriteIndented = true }), "application/json", "pc-build.json")
        };
    }

    private static BuilderIndexViewModel BuildViewModel(
        IReadOnlyList<Component> components,
        IReadOnlyList<SavedBuild> savedBuilds,
        SelectedBuild build,
        CompatibilityResult? compatibility,
        decimal totalPrice,
        int estimatedWattage)
    {
        return new BuilderIndexViewModel
        {
            Components = components,
            Build = build,
            Compatibility = compatibility,
            TotalPrice = totalPrice,
            EstimatedWattage = estimatedWattage,
            SavedBuilds = savedBuilds
        };
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}
