namespace PcBuilder.Web.Models.Components;

public sealed class Motherboard : Component
{
    public override string Type => "motherboard";

    public string Socket { get; init; } = string.Empty;

    public List<string> SupportedRamTypes { get; init; } = [];

    public string Chipset { get; init; } = string.Empty;

    public string FormFactor { get; init; } = string.Empty;
}
