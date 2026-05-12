namespace PcBuilder.Web.Models;

public sealed class SavedBuild
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public int? CpuId { get; init; }

    public int? MotherboardId { get; init; }

    public int? RamId { get; init; }

    public int? GpuId { get; init; }

    public int? PsuId { get; init; }

    public int? CaseId { get; init; }

    public int? CoolerId { get; init; }

    public decimal TotalPrice { get; init; }

    public int EstimatedWattage { get; init; }

    public string BuildCategory { get; init; } = "Uncategorized";

    public int CompatibilityPercentage { get; init; }

    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}
