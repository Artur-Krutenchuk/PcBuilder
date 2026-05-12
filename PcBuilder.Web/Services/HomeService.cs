using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;

namespace PcBuilder.Web.Services;

public sealed class HomeService : IHomeService
{
    private const int EngineCategoryCount = 5;

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

    private static readonly (string Headline, string Tagline, int PresetIndex)[] SpotlightMap =
    [
        ("Budget Gaming", "Balanced parts for high-frame 1080p gaming.", 3),
        ("High Performance Gaming", "High-refresh 1440p with headroom to spare.", 1),
        ("Workstation", "Heavy CPU and GPU throughput for creation workloads.", 2)
    ];

    private static readonly LandingFeatureItem[] FeatureTemplate =
    [
        new()
        {
            Title = "Smart Compatibility Validation",
            IconClass = "bi-shield-check",
            Description =
                "Socket, RAM type, board sizing, GPU clearance, and power checks run automatically as you pick parts."
        },
        new()
        {
            Title = "FPS Estimation",
            IconClass = "bi-graph-up-arrow",
            Description =
                "See projected frame rates across 1080p–4K, ray tracing, competitive, and AAA scenarios before you buy."
        },
        new()
        {
            Title = "Thermal Analysis",
            IconClass = "bi-thermometer-half",
            Description =
                "Estimated CPU and GPU temperatures blend cooler ratings, case airflow, and PSU headroom."
        },
        new()
        {
            Title = "Bottleneck Detection",
            IconClass = "bi-diagram-3",
            Description =
                "Directional CPU and GPU balance hints keep upgrades purposeful instead of guesswork."
        },
        new()
        {
            Title = "Saved Builds",
            IconClass = "bi-bookmark-heart",
            Description =
                "Sign in to snapshot rigs with totals, wattage, and compatibility scores for one-click reload."
        },
        new()
        {
            Title = "Performance Scoring",
            IconClass = "bi-speedometer2",
            Description =
                "Compatibility percentage plus efficiency, thermal, and PSU health scores summarize rig quality."
        }
    ];

    private readonly IComponentService _componentService;
    private readonly ISavedBuildService _savedBuildService;
    private readonly ICompatibilityService _compatibilityService;
    private readonly IBuildService _buildService;

    public HomeService(
        IComponentService componentService,
        ISavedBuildService savedBuildService,
        ICompatibilityService compatibilityService,
        IBuildService buildService)
    {
        _componentService = componentService;
        _savedBuildService = savedBuildService;
        _compatibilityService = compatibilityService;
        _buildService = buildService;
    }

    public async Task<HomeIndexViewModel> BuildIndexViewModelAsync(string? userId, CancellationToken cancellationToken = default)
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

        int? savedCount = null;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var builds = await _savedBuildService.GetAllAsync(userId, cancellationToken);
            savedCount = builds.Count;
        }

        var popularBuilds = PopularPresets
            .Select(preset => TryCreatePopularCard(preset, components))
            .OfType<PopularBuildCard>()
            .ToList();

        var spotlight = new List<HomeSpotlightBuildCard>();
        foreach (var (headline, tagline, presetIndex) in SpotlightMap)
        {
            if ((uint)presetIndex >= (uint)PopularPresets.Length)
            {
                continue;
            }

            var card = TryCreateSpotlightCard(headline, tagline, PopularPresets[presetIndex], components);
            if (card is not null)
            {
                spotlight.Add(card);
            }
        }

        var heroStats = BuildHeroStats(components.Count, sockets.Count, savedCount);

        return new HomeIndexViewModel
        {
            FeatureCards = FeatureTemplate,
            HeroStats = heroStats,
            SpotlightBuilds = spotlight,
            PopularBuilds = popularBuilds
        };
    }

    private static IReadOnlyList<LandingStatItem> BuildHeroStats(int componentCount, int socketCount, int? savedBuildCount)
    {
        var savedValue = savedBuildCount is null ? "—" : savedBuildCount.Value.ToString("N0");
        var savedDescription = savedBuildCount is null
            ? "Sign in to count rigs stored under your profile."
            : "Builds saved to your account from the configurator.";

        return
        [
            new LandingStatItem
            {
                Label = "Catalog components",
                Value = componentCount.ToString("N0"),
                Description = "CPUs, boards, memory, GPUs, PSUs, cases, and coolers.",
                IconClass = "bi-box-seam"
            },
            new LandingStatItem
            {
                Label = "Supported sockets",
                Value = socketCount.ToString("N0"),
                Description = "Distinct CPU sockets represented in the catalog.",
                IconClass = "bi-cpu"
            },
            new LandingStatItem
            {
                Label = "Build categories",
                Value = EngineCategoryCount.ToString("N0"),
                Description = "Gaming, budget, workstation, streaming, and more.",
                IconClass = "bi-tags"
            },
            new LandingStatItem
            {
                Label = "Your saved builds",
                Value = savedValue,
                Description = savedDescription,
                IconClass = "bi-bookmark-check"
            }
        ];
    }

    private PopularBuildCard? TryCreatePopularCard(
        PopularPreset preset,
        IReadOnlyList<Component> components)
    {
        var draft = TryEvaluatePreset(preset, components);
        if (draft is null)
        {
            return null;
        }

        return new PopularBuildCard
        {
            Title = preset.Title,
            Subtitle = preset.Subtitle,
            CpuName = draft.CpuName,
            GpuName = draft.GpuName,
            TotalPrice = draft.TotalPrice,
            Category = draft.Category,
            CompatibilityPercentage = draft.CompatibilityPercentage,
            AccentClass = preset.AccentClass,
            CpuId = preset.CpuId,
            MotherboardId = preset.MotherboardId,
            RamId = preset.RamId,
            GpuId = preset.GpuId,
            PsuId = preset.PsuId,
            CaseId = preset.CaseId,
            CoolerId = preset.CoolerId,
            HeroImageUrl = draft.HeroImageUrl
        };
    }

    private HomeSpotlightBuildCard? TryCreateSpotlightCard(
        string headline,
        string tagline,
        PopularPreset preset,
        IReadOnlyList<Component> components)
    {
        var draft = TryEvaluatePreset(preset, components);
        if (draft is null)
        {
            return null;
        }

        return new HomeSpotlightBuildCard
        {
            Headline = headline,
            Tagline = tagline,
            TotalPrice = draft.TotalPrice,
            Category = draft.Category,
            CompatibilityPercentage = draft.CompatibilityPercentage,
            AccentClass = preset.AccentClass,
            HeroImageUrl = draft.HeroImageUrl,
            CpuId = preset.CpuId,
            MotherboardId = preset.MotherboardId,
            RamId = preset.RamId,
            GpuId = preset.GpuId,
            PsuId = preset.PsuId,
            CaseId = preset.CaseId,
            CoolerId = preset.CoolerId
        };
    }

    private PresetEvaluation? TryEvaluatePreset(PopularPreset preset, IReadOnlyList<Component> components)
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

        _buildService.HydrateBuild(build, components);
        if (build.Cpu is null || build.Gpu is null)
        {
            return null;
        }

        var gpu = build.Gpu;
        var result = _compatibilityService.Check(build);
        var total = _buildService.CalculateTotalPrice(build);

        var heroImage = !string.IsNullOrWhiteSpace(gpu.ImageUrl)
            ? gpu.ImageUrl
            : build.Case?.ImageUrl;

        return new PresetEvaluation(
            build.Cpu.Name,
            gpu.Name,
            total,
            result.BuildCategory,
            result.CompatibilityPercentage,
            heroImage);
    }

    private sealed record PresetEvaluation(
        string CpuName,
        string GpuName,
        decimal TotalPrice,
        string Category,
        int CompatibilityPercentage,
        string? HeroImageUrl);

    private sealed record PopularPreset(
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
