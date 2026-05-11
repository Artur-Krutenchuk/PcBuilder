namespace PcBuilder.Web.Models.Components;

public sealed class Cpu : Component
{
    public override string Type => "cpu";

    public string Socket { get; init; } = string.Empty;

    public int TdpWatts { get; init; }

    public int Cores { get; init; }

    public int Threads { get; init; }

    public decimal BaseClockGhz { get; init; }

    public string? Generation { get; init; }

    public string? PerformanceTier { get; init; }
}
