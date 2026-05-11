namespace PcBuilder.Web.Models.Components;

public sealed class Psu : Component
{
    public override string Type => "psu";

    public int Wattage { get; init; }

    public string EfficiencyRating { get; init; } = string.Empty;
}
