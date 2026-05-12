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
    private const int BaseSystemReserveWatts = 100;
    private readonly IComponentService _componentService;
    private readonly ICompatibilityService _compatibilityService;
    private readonly ISavedBuildService _savedBuildService;

    public BuilderController(
        IComponentService componentService,
        ICompatibilityService compatibilityService,
        ISavedBuildService savedBuildService)
    {
        _componentService = componentService;
        _compatibilityService = compatibilityService;
        _savedBuildService = savedBuildService;
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
        HydrateBuildSelections(selection, components);
        var compatibilityPreview = selection.CpuId is not null || selection.GpuId is not null
            ? _compatibilityService.Check(selection)
            : null;
        var totalPrice = CalculateTotalPrice(selection);
        var estimatedWattage = CalculateEstimatedWattage(selection);
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
        HydrateBuildSelections(build, components);
        var compatibilityResult = _compatibilityService.Check(build);
        var model = BuildViewModel(
            components,
            savedBuilds,
            build,
            compatibilityResult,
            CalculateTotalPrice(build),
            CalculateEstimatedWattage(build));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Validate([FromForm] SelectedBuild build, CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        HydrateBuildSelections(build, components);

        var compatibilityResult = _compatibilityService.Check(build);
        var totalPrice = CalculateTotalPrice(build);
        var estimatedWattage = CalculateEstimatedWattage(build);

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
        [FromForm] bool isPublic,
        CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        HydrateBuildSelections(build, components);

        var compatibilityResult = _compatibilityService.Check(build);
        var totalPrice = CalculateTotalPrice(build);
        var estimatedWattage = CalculateEstimatedWattage(build);

        var trimmedName = string.IsNullOrWhiteSpace(buildName)
            ? string.Empty
            : buildName.Trim();
        if (trimmedName.Length > 120)
        {
            trimmedName = trimmedName[..120];
        }

        var savedBuild = new SavedBuild
        {
            UserId = GetUserId(),
            Name = trimmedName,
            IsPublic = isPublic,
            CreatorUserName = User.Identity?.Name ?? string.Empty,
            EstimatedFps1080p = compatibilityResult.EstimatedFps1080p,
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

        var estimatedWattage = CalculateEstimatedWattage(
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

        HydrateBuildSelections(buildA, components);
        HydrateBuildSelections(buildB, components);

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

        HydrateBuildSelections(build, components);
        var compatibility = _compatibilityService.Check(build);
        var totalPrice = CalculateTotalPrice(build);
        var estimatedWattage = CalculateEstimatedWattage(build);
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
            "txt" or "text" => File(Encoding.UTF8.GetBytes(BuildTxtExport(build, compatibility, totalPrice, wattage)), "text/plain; charset=utf-8", "pc-build.txt"),
            _ => File(JsonSerializer.SerializeToUtf8Bytes(export, new JsonSerializerOptions { WriteIndented = true }), "application/json", "pc-build.json")
        };
    }

    private static string BuildTxtExport(SelectedBuild build, CompatibilityResult compatibility, decimal totalPrice, int estimatedWattage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PC Build Export");
        sb.AppendLine($"Generated (UTC): {DateTime.UtcNow:O}");
        sb.AppendLine();

        sb.AppendLine("Selected parts:");
        AppendPart(sb, "CPU", build.Cpu?.Name, build.Cpu?.Manufacturer, build.Cpu?.Price);
        AppendPart(sb, "Motherboard", build.Motherboard?.Name, build.Motherboard?.Manufacturer, build.Motherboard?.Price);
        AppendPart(sb, "RAM", build.Ram?.Name, build.Ram?.Manufacturer, build.Ram?.Price);
        AppendPart(sb, "GPU", build.Gpu?.Name, build.Gpu?.Manufacturer, build.Gpu?.Price);
        AppendPart(sb, "PSU", build.Psu?.Name, build.Psu?.Manufacturer, build.Psu?.Price);
        AppendPart(sb, "Case", build.Case?.Name, build.Case?.Manufacturer, build.Case?.Price);
        AppendPart(sb, "Cooler", build.Cooler?.Name, build.Cooler?.Manufacturer, build.Cooler?.Price);
        sb.AppendLine();

        sb.AppendLine($"Total price: {totalPrice:C2}");
        sb.AppendLine($"Estimated wattage: {estimatedWattage} W");
        sb.AppendLine();

        sb.AppendLine($"Compatibility: {compatibility.CompatibilityPercentage}% ({(compatibility.IsValid ? "Valid" : "Invalid")})");
        sb.AppendLine($"Category: {compatibility.BuildCategory}");
        sb.AppendLine();

        sb.AppendLine("FPS estimates (Gaming):");
        sb.AppendLine($"- 1080p: {compatibility.EstimatedFps1080p}");
        sb.AppendLine($"- 1440p: {compatibility.EstimatedFps1440p}");
        sb.AppendLine($"- 4K: {compatibility.EstimatedFps4k}");
        sb.AppendLine($"- Ray tracing: {compatibility.EstimatedRayTracingFps}");
        sb.AppendLine($"- Competitive: {compatibility.EstimatedCompetitiveFps}");
        sb.AppendLine($"- AAA: {compatibility.EstimatedAaaFps}");
        sb.AppendLine();

        sb.AppendLine("Bottleneck analysis:");
        sb.AppendLine($"- CPU bottleneck: {compatibility.CpuBottleneckPercentage}%");
        sb.AppendLine($"- GPU bottleneck: {compatibility.GpuBottleneckPercentage}%");
        sb.AppendLine();

        sb.AppendLine("Recommendations:");
        if (compatibility.Recommendations.Count == 0)
        {
            sb.AppendLine("- (none)");
        }
        else
        {
            foreach (var rec in compatibility.Recommendations)
            {
                sb.AppendLine($"- {rec}");
            }
        }

        return sb.ToString();
    }

    private static void AppendPart(StringBuilder sb, string label, string? name, string? manufacturer, decimal? price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            sb.AppendLine($"- {label}: (not selected)");
            return;
        }

        sb.AppendLine($"- {label}: {name} ({manufacturer ?? "Unknown"}) - {(price ?? 0m):C2}");
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

    private static void HydrateBuildSelections(SelectedBuild build, IReadOnlyList<Component> components)
    {
        build.Cpu = FindById<Cpu>(components, build.CpuId);
        build.Motherboard = FindById<Motherboard>(components, build.MotherboardId);
        build.Ram = FindById<Ram>(components, build.RamId);
        build.Gpu = FindById<Gpu>(components, build.GpuId);
        build.Psu = FindById<Psu>(components, build.PsuId);
        build.Case = FindById<Case>(components, build.CaseId);
        build.Cooler = FindById<Cooler>(components, build.CoolerId);
    }

    private static TComponent? FindById<TComponent>(IReadOnlyList<Component> components, int? id)
        where TComponent : Component
    {
        if (id is null)
        {
            return null;
        }

        return components.OfType<TComponent>().FirstOrDefault(component => component.Id == id.Value);
    }

    private static decimal CalculateTotalPrice(SelectedBuild build)
    {
        return (build.Cpu?.Price ?? 0m)
             + (build.Motherboard?.Price ?? 0m)
             + (build.Ram?.Price ?? 0m)
             + (build.Gpu?.Price ?? 0m)
             + (build.Psu?.Price ?? 0m)
             + (build.Case?.Price ?? 0m)
             + (build.Cooler?.Price ?? 0m);
    }

    private static int CalculateEstimatedWattage(SelectedBuild build)
    {
        var cpuTdp = build.Cpu?.TdpWatts ?? 0;
        var gpuTdp = build.Gpu?.TdpWatts ?? 0;
        var ramCapacityGb = build.Ram?.CapacityGb ?? 0;

        return CalculateEstimatedWattage(cpuTdp, gpuTdp, ramCapacityGb);
    }

    private static int CalculateEstimatedWattage(int cpuTdp, int gpuTdp, int ramCapacityGb)
    {

        if (cpuTdp == 0 && gpuTdp == 0 && ramCapacityGb == 0)
        {
            return 0;
        }

        var ramReserve = (int)Math.Ceiling((ramCapacityGb / 16m) * 5m);
        return cpuTdp + gpuTdp + BaseSystemReserveWatts + ramReserve;
    }

    private string GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }
}
