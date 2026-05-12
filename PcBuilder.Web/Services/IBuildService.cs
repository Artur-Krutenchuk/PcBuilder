using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;
using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Services;

public interface IBuildService
{
    void HydrateBuild(SelectedBuild build, IReadOnlyList<Component> components);

    decimal CalculateTotalPrice(SelectedBuild build);

    int CalculateEstimatedWattage(SelectedBuild build);

    int CalculateEstimatedWattage(int cpuTdpWatts, int gpuTdpWatts, int ramCapacityGb);

    BuildExportData ExportBuild(SelectedBuild build, CompatibilityResult compatibility, decimal totalPrice, int estimatedWattage);

    string ExportBuildAsText(SelectedBuild build, CompatibilityResult compatibility, decimal totalPrice, int estimatedWattage);
}

public record BuildExportData(
    DateTime GeneratedAtUtc,
    object SelectedParts,
    object Totals,
    object Compatibility);
