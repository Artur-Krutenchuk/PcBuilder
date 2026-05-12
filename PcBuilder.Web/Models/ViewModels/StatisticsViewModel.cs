namespace PcBuilder.Web.Models.ViewModels;

public sealed class StatisticsViewModel
{
    public int TotalComponents { get; init; }

    public int CpuCount { get; init; }

    public int GpuCount { get; init; }

    public int MotherboardCount { get; init; }

    public int RamCount { get; init; }

    public int PsuCount { get; init; }

    public int CaseCount { get; init; }

    public decimal AverageCpuPrice { get; init; }

    public decimal AverageGpuPrice { get; init; }

    public Dictionary<string, int> ManufacturerCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> SocketCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> ComponentTypeCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, decimal> AveragePriceByType { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
