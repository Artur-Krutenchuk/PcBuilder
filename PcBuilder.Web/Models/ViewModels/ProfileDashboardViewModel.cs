using PcBuilder.Web.Models;

namespace PcBuilder.Web.Models.ViewModels;

public sealed class ProfileDashboardViewModel
{
    public string UserName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public int SavedBuildsCount { get; init; }

    public decimal AverageBuildPrice { get; init; }

    public string FavoriteBuildCategory { get; init; } = "—";

    public int BestCompatibilityPercentage { get; init; }

    public int AverageCompatibilityPercentage { get; init; }

    public DateTime? LastBuildCreatedAt { get; init; }

    public IReadOnlyList<SavedBuild> RecentSavedBuilds { get; init; } = [];
}
