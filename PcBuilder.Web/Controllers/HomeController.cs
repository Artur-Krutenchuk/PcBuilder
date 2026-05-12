using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Services;
using System.Diagnostics;

namespace PcBuilder.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IComponentService _componentService;
    private readonly ICompatibilityService _compatibilityService;

    private static readonly PopularPreset[] PopularPresets =
    [
        new(
            "1080p Balanced",
            "AM4 · DDR4 · Efficient mid-tier GPU",
            "primary",
            2, 101, 202, 302, 402, 508, 605),
        new(
            "1440p Powerhouse",
            "AM5 · DDR5 · Ryzen X3D meets Radeon",
            "danger",
            4, 106, 207, 305, 407, 510, 602),
        new(
            "4K Creator",
            "Intel flagship · RTX 4080 · Airflow tower",
            "info",
            8, 107, 206, 307, 408, 502, 604),
        new(
            "Budget Starter",
            "Intel i5 · DDR4 · Compact mesh airflow",
            "success",
            5, 108, 201, 301, 401, 505, 601),
        new(
            "Next-gen Baseline",
            "Zen 4 · DDR5 · Mesh case · Strong uplift path",
            "warning",
            3, 104, 205, 303, 406, 509, 603)
    ];

    public HomeController(IComponentService componentService, ICompatibilityService compatibilityService)
    {
        _componentService = componentService;
        _compatibilityService = compatibilityService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var cpus = components.OfType<Cpu>().ToList();
        var motherboards = components.OfType<Motherboard>().ToList();

        var sockets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var cpu in cpus)
        {
            if (!string.IsNullOrWhiteSpace(cpu.Socket))
            {
                sockets.Add(cpu.Socket.Trim());
            }
        }

        foreach (var mb in motherboards)
        {
            if (!string.IsNullOrWhiteSpace(mb.Socket))
            {
                sockets.Add(mb.Socket.Trim());
            }
        }

        const int engineCategoryCount = 5;
        var popularBuilds = PopularPresets
            .Select(preset => TryCreatePopularCard(preset, components, _compatibilityService))
            .Where(card => card is not null)
            .Cast<PopularBuildCard>()
            .ToList();

        var model = new HomeIndexViewModel
        {
            ComponentCount = components.Count,
            SupportedSocketCount = sockets.Count,
            BuildCategoryCount = engineCategoryCount,
            CatalogStats =
            [
                new LandingMetricItem
                {
                    Label = "Catalog components",
                    Value = components.Count.ToString("N0"),
                    Description = "CPUs, boards, memory, GPUs, PSUs, cases, and coolers.",
                    IconClass = "bi-box-seam"
                },
                new LandingMetricItem
                {
                    Label = "Supported sockets",
                    Value = sockets.Count.ToString("N0"),
                    Description = "Distinct CPU sockets represented in the catalog.",
                    IconClass = "bi-cpu"
                },
                new LandingMetricItem
                {
                    Label = "Build categories",
                    Value = engineCategoryCount.ToString("N0"),
                    Description = "Gaming, Budget, Workstation, Streaming, and more.",
                    IconClass = "bi-tags"
                },
                new LandingMetricItem
                {
                    Label = "Live validation",
                    Value = "AJAX",
                    Description = "Instant feedback without full page reloads.",
                    IconClass = "bi-lightning-charge"
                }
            ],
            EngineMetrics =
            [
                new LandingMetricItem
                {
                    Label = "Compatibility checks",
                    Value = "9+",
                    Description = "Socket, power, thermals, clearance, generations, and memory.",
                    IconClass = "bi-shield-check"
                },
                new LandingMetricItem
                {
                    Label = "FPS scenarios",
                    Value = "6",
                    Description = "1080p through 4K, ray tracing, competitive, and AAA estimates.",
                    IconClass = "bi-graph-up-arrow"
                },
                new LandingMetricItem
                {
                    Label = "Health signals",
                    Value = "4",
                    Description = "Compatibility score, efficiency, PSU health, and thermal index.",
                    IconClass = "bi-heart-pulse"
                },
                new LandingMetricItem
                {
                    Label = "Intelligence layers",
                    Value = "3",
                    Description = "Badges, bottlenecks, and prioritized upgrade hints.",
                    IconClass = "bi-stars"
                }
            ],
            PopularBuilds = popularBuilds
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static PopularBuildCard? TryCreatePopularCard(
        PopularPreset preset,
        IReadOnlyList<Component> components,
        ICompatibilityService compatibilityService)
    {
        var build = new SelectedBuild
        {
            CpuId = preset.CpuId,
            MotherboardId = preset.MotherboardId,
            RamId = preset.RamId,
            GpuId = preset.GpuId,
            PsuId = preset.PsuId,
            CaseId = preset.CaseId,
            CoolerId = preset.CoolerId
        };

        HydrateSelection(build, components);
        if (build.Cpu is null || build.Gpu is null)
        {
            return null;
        }

        var result = compatibilityService.Check(build);
        var total = (build.Cpu?.Price ?? 0m)
                    + (build.Motherboard?.Price ?? 0m)
                    + (build.Ram?.Price ?? 0m)
                    + (build.Gpu?.Price ?? 0m)
                    + (build.Psu?.Price ?? 0m)
                    + (build.Case?.Price ?? 0m)
                    + (build.Cooler?.Price ?? 0m);

        var heroImage = !string.IsNullOrWhiteSpace(build.Gpu?.ImageUrl)
            ? build.Gpu.ImageUrl
            : build.Case?.ImageUrl;

        return new PopularBuildCard
        {
            Title = preset.Title,
            Subtitle = preset.Subtitle,
            CpuName = build.Cpu!.Name,
            GpuName = build.Gpu!.Name,
            TotalPrice = total,
            Category = result.BuildCategory,
            AccentClass = preset.AccentClass,
            CpuId = preset.CpuId,
            MotherboardId = preset.MotherboardId,
            RamId = preset.RamId,
            GpuId = preset.GpuId,
            PsuId = preset.PsuId,
            CaseId = preset.CaseId,
            CoolerId = preset.CoolerId,
            HeroImageUrl = heroImage
        };
    }

    private static void HydrateSelection(SelectedBuild build, IReadOnlyList<Component> components)
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

    private readonly record struct PopularPreset(
        string Title,
        string Subtitle,
        string AccentClass,
        int CpuId,
        int MotherboardId,
        int RamId,
        int GpuId,
        int PsuId,
        int CaseId,
        int CoolerId);
}
