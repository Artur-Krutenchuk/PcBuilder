using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;

namespace PcBuilder.Web.Models;

public sealed class BuilderIndexViewModel
{
    public IReadOnlyList<Component> Components { get; init; } = [];

    public SelectedBuild Build { get; init; } = new();

    public CompatibilityResult? Compatibility { get; init; }

    public decimal TotalPrice { get; init; }

    public int EstimatedWattage { get; init; }

    public IReadOnlyList<SavedBuild> SavedBuilds { get; init; } = [];
}
