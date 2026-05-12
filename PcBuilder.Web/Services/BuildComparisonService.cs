using PcBuilder.Web.Models;
using PcBuilder.Web.Models.ViewModels;

namespace PcBuilder.Web.Services;

public sealed class BuildComparisonService : IBuildComparisonService
{
    private const string BetterClass = "table-success";
    private const string WorseClass = "table-danger-subtle";

    private readonly ISavedBuildService _savedBuildService;

    public BuildComparisonService(ISavedBuildService savedBuildService)
    {
        _savedBuildService = savedBuildService;
    }

    public async Task<BuildComparisonViewModel> BuildComparisonAsync(
        string userId,
        string? buildAId,
        string? buildBId,
        CancellationToken cancellationToken = default)
    {
        var builds = (await _savedBuildService.GetAllAsync(userId, cancellationToken))
            .OrderByDescending(build => build.CreatedAtUtc)
            .ToList();

        var buildA = FindBuild(builds, buildAId);
        var buildB = FindBuild(builds, buildBId);
        var message = BuildMessage(builds, buildAId, buildBId, buildA, buildB);

        return new BuildComparisonViewModel
        {
            SavedBuilds = builds,
            BuildAId = buildAId,
            BuildBId = buildBId,
            BuildA = buildA,
            BuildB = buildB,
            Items = buildA is not null && buildB is not null ? BuildItems(buildA, buildB) : [],
            Message = message
        };
    }

    private static SavedBuild? FindBuild(IReadOnlyList<SavedBuild> builds, string? id)
    {
        return Guid.TryParse(id, out var guid)
            ? builds.FirstOrDefault(build => build.Id == guid)
            : null;
    }

    private static string? BuildMessage(
        IReadOnlyList<SavedBuild> builds,
        string? buildAId,
        string? buildBId,
        SavedBuild? buildA,
        SavedBuild? buildB)
    {
        if (builds.Count < 2)
        {
            return "Save at least two builds to compare them.";
        }

        if (string.IsNullOrWhiteSpace(buildAId) || string.IsNullOrWhiteSpace(buildBId))
        {
            return "Select two saved builds to compare.";
        }

        if (string.Equals(buildAId, buildBId, StringComparison.OrdinalIgnoreCase))
        {
            return "Choose two different saved builds.";
        }

        if (buildA is null || buildB is null)
        {
            return "One of the selected builds was not found in your saved builds.";
        }

        return null;
    }

    private static IReadOnlyList<BuildComparisonItemViewModel> BuildItems(SavedBuild buildA, SavedBuild buildB)
    {
        return
        [
            CompareDecimal("Total price", buildA.TotalPrice, buildB.TotalPrice, "C2", higherIsBetter: false),
            CompareInt("Compatibility", buildA.CompatibilityPercentage, buildB.CompatibilityPercentage, "N0", higherIsBetter: true, "%"),
            CompareInt("Estimated wattage", buildA.EstimatedWattage, buildB.EstimatedWattage, "N0", higherIsBetter: false, " W"),
            CompareInt("FPS 1080p", buildA.EstimatedFps1080p, buildB.EstimatedFps1080p, "N0", higherIsBetter: true),
            CompareInt("FPS 1440p", buildA.EstimatedFps1440p, buildB.EstimatedFps1440p, "N0", higherIsBetter: true),
            CompareInt("FPS 4K", buildA.EstimatedFps4k, buildB.EstimatedFps4k, "N0", higherIsBetter: true),
            CompareInt("CPU bottleneck", buildA.CpuBottleneckPercentage, buildB.CpuBottleneckPercentage, "N0", higherIsBetter: false, "%"),
            CompareInt("GPU bottleneck", buildA.GpuBottleneckPercentage, buildB.GpuBottleneckPercentage, "N0", higherIsBetter: false, "%"),
            CompareInt("Efficiency score", buildA.EfficiencyScore, buildB.EfficiencyScore, "N0", higherIsBetter: true),
            CompareInt("Thermal score", buildA.ThermalScore, buildB.ThermalScore, "N0", higherIsBetter: true),
            CompareInt("PSU health score", buildA.PsuHealthScore, buildB.PsuHealthScore, "N0", higherIsBetter: true)
        ];
    }

    private static BuildComparisonItemViewModel CompareInt(
        string label,
        int valueA,
        int valueB,
        string format,
        bool higherIsBetter,
        string suffix = "")
    {
        return CompareDecimal(label, valueA, valueB, format, higherIsBetter, suffix);
    }

    private static BuildComparisonItemViewModel CompareDecimal(
        string label,
        decimal valueA,
        decimal valueB,
        string format,
        bool higherIsBetter,
        string suffix = "")
    {
        var (classA, classB) = GetComparisonClasses(valueA, valueB, higherIsBetter);

        return new BuildComparisonItemViewModel
        {
            Label = label,
            BuildAValue = $"{valueA.ToString(format)}{suffix}",
            BuildBValue = $"{valueB.ToString(format)}{suffix}",
            BuildAClass = classA,
            BuildBClass = classB
        };
    }

    private static (string ClassA, string ClassB) GetComparisonClasses(decimal valueA, decimal valueB, bool higherIsBetter)
    {
        if (valueA == valueB)
        {
            return (string.Empty, string.Empty);
        }

        var aIsBetter = higherIsBetter ? valueA > valueB : valueA < valueB;
        return aIsBetter ? (BetterClass, WorseClass) : (WorseClass, BetterClass);
    }
}
