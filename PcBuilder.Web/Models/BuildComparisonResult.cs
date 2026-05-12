namespace PcBuilder.Web.Models;

public sealed class BuildComparisonResult
{
    public BuildSnapshot BuildA { get; init; } = new();

    public BuildSnapshot BuildB { get; init; } = new();

    public IReadOnlyList<ComparisonMetric> Metrics { get; init; } = [];
}

public sealed class BuildSnapshot
{
    public string Label { get; init; } = string.Empty;

    public string BuildCategory { get; init; } = "Uncategorized";

    public decimal TotalPrice { get; init; }

    public int EstimatedWattage { get; init; }

    public int CompatibilityPercentage { get; init; }

    public int AverageFps { get; init; }

    public int ThermalScore { get; init; }

    public int PsuHealthScore { get; init; }

    public int BottleneckPercentage { get; init; }

    public decimal ValueScore { get; init; }
}

public sealed class ComparisonMetric
{
    public string Label { get; init; } = string.Empty;

    public string BuildAValue { get; init; } = string.Empty;

    public string BuildBValue { get; init; } = string.Empty;

    public bool? IsBuildABetter { get; init; }
}
