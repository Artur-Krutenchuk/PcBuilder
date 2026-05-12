namespace PcBuilder.Web.Models.Responses;

public sealed class CompatibilityResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; init; } = [];

    public List<string> Warnings { get; init; } = [];

    public int CompatibilityPercentage { get; set; }

    public string BuildCategory { get; set; } = "Uncategorized";

    public int EstimatedFps1080p { get; set; }

    public int EstimatedFps1440p { get; set; }

    public int EstimatedFps4k { get; set; }

    public int EstimatedCpuTemperatureCelsius { get; set; }

    public int EstimatedGpuTemperatureCelsius { get; set; }

    public int EstimatedSystemWattage { get; set; }

    public int PsuHealthScore { get; set; }

    public int ThermalHealthScore { get; set; }

    public int EfficiencyScore { get; set; }

    public List<string> BuildBadges { get; init; } = [];

    public List<string> Recommendations { get; init; } = [];

    public int CpuBottleneckPercentage { get; set; }

    public int GpuBottleneckPercentage { get; set; }

    public int EstimatedRayTracingFps { get; set; }

    public int EstimatedCompetitiveFps { get; set; }

    public int EstimatedAaaFps { get; set; }
}
