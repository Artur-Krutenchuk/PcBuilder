namespace PcBuilder.Web.Models;

public sealed class HomeIndexViewModel
{
    public IReadOnlyList<LandingFeatureItem> FeatureCards { get; init; } = [];

    public IReadOnlyList<LandingStatItem> HeroStats { get; init; } = [];

    public IReadOnlyList<HomeSpotlightBuildCard> SpotlightBuilds { get; init; } = [];

    public IReadOnlyList<PopularBuildCard> PopularBuilds { get; init; } = [];
}

public sealed class LandingFeatureItem
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string IconClass { get; init; } = "bi-circle";
}

public sealed class LandingStatItem
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string IconClass { get; init; } = "bi-circle";
}

public sealed class HomeSpotlightBuildCard
{
    public string Headline { get; init; } = string.Empty;

    public string Tagline { get; init; } = string.Empty;

    public decimal TotalPrice { get; init; }

    public string Category { get; init; } = string.Empty;

    public int CompatibilityPercentage { get; init; }

    public string AccentClass { get; init; } = "primary";

    public string? HeroImageUrl { get; init; }

    public int CpuId { get; init; }

    public int MotherboardId { get; init; }

    public int RamId { get; init; }

    public int GpuId { get; init; }

    public int PsuId { get; init; }

    public int CaseId { get; init; }

    public int CoolerId { get; init; }
}

public sealed class PopularBuildCard
{
    public string Title { get; init; } = string.Empty;

    public string Subtitle { get; init; } = string.Empty;

    public string CpuName { get; init; } = string.Empty;

    public string GpuName { get; init; } = string.Empty;

    public decimal TotalPrice { get; init; }

    public string Category { get; init; } = string.Empty;

    public int CompatibilityPercentage { get; init; }

    public string AccentClass { get; init; } = "primary";

    public int CpuId { get; init; }

    public int MotherboardId { get; init; }

    public int RamId { get; init; }

    public int GpuId { get; init; }

    public int PsuId { get; init; }

    public int CaseId { get; init; }

    public int CoolerId { get; init; }

    public string? HeroImageUrl { get; init; }
}
