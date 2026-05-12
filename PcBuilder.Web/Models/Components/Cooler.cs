namespace PcBuilder.Web.Models.Components;

public sealed class Cooler : Component
{
    public override string Type => "cooler";

    public List<string> SupportedSockets { get; init; } = [];

    public int CoolingCapacityWatts { get; init; }

    public decimal NoiseLevelDb { get; init; }
}

