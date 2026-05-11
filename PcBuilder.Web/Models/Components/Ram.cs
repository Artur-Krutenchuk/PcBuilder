namespace PcBuilder.Web.Models.Components;

public sealed class Ram : Component
{
    public override string Type => "ram";

    public string RamType { get; init; } = string.Empty;

    public int CapacityGb { get; init; }

    public int FrequencyMhz { get; init; }

    public string? PerformanceTier { get; init; }
}
