using System.Text.Json.Serialization;

namespace PcBuilder.Web.Models;

public sealed class SavedBuild
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public string UserId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsPublic { get; init; }

    public string CreatorUserName { get; init; } = string.Empty;

    public int EstimatedFps1080p { get; init; }

    [JsonIgnore]
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name)
            ? $"Untitled · {Id.ToString("N")[..8]}"
            : Name.Trim();

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
