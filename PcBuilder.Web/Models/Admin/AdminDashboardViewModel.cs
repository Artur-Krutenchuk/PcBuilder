namespace PcBuilder.Web.Models.Admin;

public sealed class AdminDashboardViewModel
{
    public int TotalUsers { get; init; }

    public int TotalSavedBuilds { get; init; }

    public IReadOnlyList<PopularComponentRow> PopularComponents { get; init; } = [];

    public IReadOnlyList<CompatibilityIssueRow> CommonCompatibilityIssues { get; init; } = [];
}

public sealed class PopularComponentRow
{
    public string Slot { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public int ComponentId { get; init; }

    public int UsageCount { get; init; }
}

public sealed class CompatibilityIssueRow
{
    public string Message { get; init; } = string.Empty;

    public int Count { get; init; }

    public bool IsWarning { get; init; }
}
