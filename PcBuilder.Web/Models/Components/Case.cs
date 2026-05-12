namespace PcBuilder.Web.Models.Components;

public sealed class Case : Component
{
    public override string Type => "case";

    public int? MaxGpuLengthMm { get; init; }

    public List<string> SupportedMotherboardSizes { get; init; } = [];

    public int IncludedFans { get; init; }

    public int AirflowRating { get; init; }
}
