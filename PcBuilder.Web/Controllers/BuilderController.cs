using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

public sealed class BuilderController : Controller
{
    private const int BaseSystemReserveWatts = 100;
    private readonly IComponentService _componentService;
    private readonly ICompatibilityService _compatibilityService;

    public BuilderController(IComponentService componentService, ICompatibilityService compatibilityService)
    {
        _componentService = componentService;
        _compatibilityService = compatibilityService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var model = BuildViewModel(components, new SelectedBuild(), null, 0m, 0);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SelectedBuild build, CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        HydrateBuildSelections(build, components);
        var compatibilityResult = _compatibilityService.Check(build);
        var model = BuildViewModel(components, build, compatibilityResult, CalculateTotalPrice(build), CalculateEstimatedWattage(build));
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
            TotalPrice = totalPrice,
            EstimatedWattage = estimatedWattage,
            SelectedNames = new
            {
                Cpu = build.Cpu?.Name,
                Motherboard = build.Motherboard?.Name,
                Ram = build.Ram?.Name,
                Gpu = build.Gpu?.Name,
                Psu = build.Psu?.Name
            }
        });
    }

    private static BuilderIndexViewModel BuildViewModel(
        IReadOnlyList<Component> components,
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
            EstimatedWattage = estimatedWattage
        };
    }

    private static void HydrateBuildSelections(SelectedBuild build, IReadOnlyList<Component> components)
    {
        build.Cpu = FindById<Cpu>(components, build.CpuId);
        build.Motherboard = FindById<Motherboard>(components, build.MotherboardId);
        build.Ram = FindById<Ram>(components, build.RamId);
        build.Gpu = FindById<Gpu>(components, build.GpuId);
        build.Psu = FindById<Psu>(components, build.PsuId);
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
             + (build.Psu?.Price ?? 0m);
    }

    private static int CalculateEstimatedWattage(SelectedBuild build)
    {
        var cpuTdp = build.Cpu?.TdpWatts ?? 0;
        var gpuTdp = build.Gpu?.TdpWatts ?? 0;
        var ramCapacityGb = build.Ram?.CapacityGb ?? 0;

        if (cpuTdp == 0 && gpuTdp == 0 && ramCapacityGb == 0)
        {
            return 0;
        }

        var ramReserve = (int)Math.Ceiling((ramCapacityGb / 16m) * 5m);
        return cpuTdp + gpuTdp + BaseSystemReserveWatts + ramReserve;
    }
}
