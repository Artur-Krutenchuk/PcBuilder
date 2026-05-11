namespace PcBuilder.Web.Models.Components;

public sealed class Gpu : Component
{
    public override string Type => "gpu";

    public int TdpWatts { get; init; }

    public int VramGb { get; init; }

    public int RecommendedPsuWattage { get; init; }

    public int? LengthMm { get; init; }

    public string? PerformanceTier { get; init; }
}
