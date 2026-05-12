using PcBuilder.Web.Models;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Services;

public sealed class GalleryService : IGalleryService
{
    private static readonly HashSet<string> AllowedSorts = new(StringComparer.OrdinalIgnoreCase)
    {
        "newest", "oldest", "price_asc", "price_desc", "compat_asc", "compat_desc", "fps_asc", "fps_desc", "name_asc",
        "name_desc"
    };

    private readonly ISavedBuildService _savedBuildService;

    public GalleryService(ISavedBuildService savedBuildService)
    {
        _savedBuildService = savedBuildService;
    }

    public async Task<GalleryFilteredResult> FilterAndSortBuildsAsync(
        string? searchQuery,
        string? category,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        var normalizedSort = string.IsNullOrWhiteSpace(sort) || !AllowedSorts.Contains(sort.Trim())
            ? "newest"
            : sort.Trim().ToLowerInvariant();

        var publicBuilds = (await _savedBuildService.GetPublicGalleryAsync(cancellationToken)).ToList();

        var categories = publicBuilds
            .Select(b => string.IsNullOrWhiteSpace(b.BuildCategory) ? "Uncategorized" : b.BuildCategory.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        IEnumerable<SavedBuild> filtered = publicBuilds;

        if (!string.IsNullOrWhiteSpace(category))
        {
            var cat = category.Trim();
            filtered = filtered.Where(b =>
                string.Equals(
                    string.IsNullOrWhiteSpace(b.BuildCategory) ? "Uncategorized" : b.BuildCategory.Trim(),
                    cat,
                    StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var term = searchQuery.Trim();
            filtered = filtered.Where(b =>
                b.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(b.CreatorUserName)
                    && b.CreatorUserName.Contains(term, StringComparison.OrdinalIgnoreCase))
                || (!string.IsNullOrWhiteSpace(b.BuildCategory)
                    && b.BuildCategory.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        var list = filtered.ToList();
        list = ApplySort(list, normalizedSort);

        return new GalleryFilteredResult(list, categories, searchQuery ?? string.Empty, category ?? string.Empty, normalizedSort);
    }

    private static List<SavedBuild> ApplySort(IReadOnlyList<SavedBuild> builds, string sort)
    {
        return sort.ToLowerInvariant() switch
        {
            "oldest" => builds.OrderBy(b => b.CreatedAtUtc).ToList(),
            "price_asc" => builds.OrderBy(b => b.TotalPrice).ToList(),
            "price_desc" => builds.OrderByDescending(b => b.TotalPrice).ToList(),
            "compat_asc" => builds.OrderBy(b => b.CompatibilityPercentage).ToList(),
            "compat_desc" => builds.OrderByDescending(b => b.CompatibilityPercentage).ToList(),
            "fps_asc" => builds.OrderBy(b => b.EstimatedFps1080p).ThenByDescending(b => b.CreatedAtUtc).ToList(),
            "fps_desc" => builds.OrderByDescending(b => b.EstimatedFps1080p).ThenByDescending(b => b.CreatedAtUtc)
                .ToList(),
            "name_asc" => builds.OrderBy(b => b.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(),
            "name_desc" => builds.OrderByDescending(b => b.DisplayName, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => builds.OrderByDescending(b => b.CreatedAtUtc).ToList()
        };
    }
}
