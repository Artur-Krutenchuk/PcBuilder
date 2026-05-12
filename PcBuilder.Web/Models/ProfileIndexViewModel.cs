namespace PcBuilder.Web.Models;

public sealed class ProfileIndexViewModel
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTime? RegisteredAtUtc { get; init; }

    public int SavedBuildCount { get; init; }

    public string FavoriteCategory { get; init; } = "—";

    public decimal AverageBuildBudget { get; init; }

    public IReadOnlyList<SavedBuild> RecentSavedBuilds { get; init; } = [];

    public IReadOnlyList<ManufacturerChartItem> ManufacturerChartData { get; init; } = [];
}

public sealed class ManufacturerChartItem
{
    public string Name { get; init; } = string.Empty;

    public int Count { get; init; }
}
