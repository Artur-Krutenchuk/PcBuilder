namespace PcBuilder.Web.Models.ViewModels;

public sealed class PopularBuildCardViewModel
{
    public string Name { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public decimal TotalPrice { get; init; }

    public int CompatibilityPercentage { get; init; }

    public int EstimatedFps1080p { get; init; }

    public int EstimatedWattage { get; init; }

    public string Badge { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string CpuId { get; init; } = string.Empty;

    public string MotherboardId { get; init; } = string.Empty;

    public string RamId { get; init; } = string.Empty;

    public string GpuId { get; init; } = string.Empty;

    public string PsuId { get; init; } = string.Empty;

    public string? CaseId { get; init; }
}
