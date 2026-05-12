namespace PcBuilder.Web.Models;

public sealed class GalleryIndexViewModel
{
    public IReadOnlyList<SavedBuild> Builds { get; init; } = [];

    public IReadOnlyList<string> Categories { get; init; } = [];

    public string SearchQuery { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Sort { get; init; } = "newest";
}
