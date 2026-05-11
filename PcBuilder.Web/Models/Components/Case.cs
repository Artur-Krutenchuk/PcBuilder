namespace PcBuilder.Web.Models.Components;

public sealed class Case : Component
{
    public override string Type => "case";

    public List<string> SupportedFormFactors { get; init; } = [];

    public int? MaxGpuLengthMm { get; init; }
}
