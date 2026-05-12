using PcBuilder.Web.Models;

namespace PcBuilder.Web.Models.ViewModels;

public sealed class BuildComparisonViewModel
{
    public IReadOnlyList<SavedBuild> SavedBuilds { get; init; } = [];

    public string? BuildAId { get; init; }

    public string? BuildBId { get; init; }

    public SavedBuild? BuildA { get; init; }

    public SavedBuild? BuildB { get; init; }

    public IReadOnlyList<BuildComparisonItemViewModel> Items { get; init; } = [];

    public string? Message { get; init; }
}
