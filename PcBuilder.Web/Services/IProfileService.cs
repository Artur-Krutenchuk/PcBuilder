using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Services;

public interface IProfileService
{
    string ComputeFavoriteCategory(IReadOnlyList<SavedBuild> builds);

    decimal ComputeAverageBudget(IReadOnlyList<SavedBuild> builds);

    IReadOnlyList<ManufacturerChartItem> ComputeManufacturerChart(
        IReadOnlyList<SavedBuild> builds,
        IReadOnlyDictionary<int, Component> componentById);
}
