using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Services;

public sealed class ProfileService : IProfileService
{
    public string ComputeFavoriteCategory(IReadOnlyList<SavedBuild> builds)
    {
        if (builds.Count == 0)
        {
            return "—";
        }

        var grouped = builds
            .GroupBy(b => string.IsNullOrWhiteSpace(b.BuildCategory) ? "Uncategorized" : b.BuildCategory.Trim())
            .OrderByDescending(g => g.Count())
            .First();

        return grouped.Key;
    }

    public decimal ComputeAverageBudget(IReadOnlyList<SavedBuild> builds)
    {
        if (builds.Count == 0)
        {
            return 0m;
        }

        return Math.Round(builds.Average(b => b.TotalPrice), 2, MidpointRounding.AwayFromZero);
    }

    public IReadOnlyList<ManufacturerChartItem> ComputeManufacturerChart(
        IReadOnlyList<SavedBuild> builds,
        IReadOnlyDictionary<int, Component> componentById)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var build in builds)
        {
            foreach (var id in new int?[]
                     {
                         build.CpuId, build.MotherboardId, build.RamId, build.GpuId, build.PsuId, build.CaseId,
                         build.CoolerId
                     })
            {
                if (id is null || !componentById.TryGetValue(id.Value, out var component))
                {
                    continue;
                }

                var name = string.IsNullOrWhiteSpace(component.Manufacturer)
                    ? "Unknown"
                    : component.Manufacturer.Trim();

                counts[name] = counts.GetValueOrDefault(name) + 1;
            }
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .Take(12)
            .Select(kv => new ManufacturerChartItem { Name = kv.Key, Count = kv.Value })
            .ToList();
    }
}
