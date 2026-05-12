using PcBuilder.Web.Models;

namespace PcBuilder.Web.Models.ViewModels;

public sealed class MyBuildsIndexViewModel
{
    public IReadOnlyList<MySavedBuildCardViewModel> Builds { get; init; } = [];
}

public sealed class MySavedBuildCardViewModel
{
    public SavedBuild Build { get; init; } = null!;

    public IReadOnlyList<string> PartLines { get; init; } = [];
}
