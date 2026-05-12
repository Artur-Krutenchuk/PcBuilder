using PcBuilder.Web.Models;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Services;

public interface IGalleryService
{
    Task<GalleryFilteredResult> FilterAndSortBuildsAsync(
        string? searchQuery,
        string? category,
        string? sort,
        CancellationToken cancellationToken = default);
}

public record GalleryFilteredResult(
    IReadOnlyList<SavedBuild> Builds,
    IReadOnlyList<string> Categories,
    string SearchQuery,
    string Category,
    string Sort);
