namespace PcBuilder.Web.Models.ViewModels;

public sealed class BuildComparisonItemViewModel
{
    public string Label { get; init; } = string.Empty;

    public string BuildAValue { get; init; } = string.Empty;

    public string BuildBValue { get; init; } = string.Empty;

    public string BuildAClass { get; init; } = string.Empty;

    public string BuildBClass { get; init; } = string.Empty;
}
