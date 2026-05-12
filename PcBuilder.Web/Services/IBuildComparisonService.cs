using PcBuilder.Web.Models.ViewModels;

namespace PcBuilder.Web.Services;

public interface IBuildComparisonService
{
    Task<BuildComparisonViewModel> BuildComparisonAsync(
        string userId,
        string? buildAId,
        string? buildBId,
        CancellationToken cancellationToken = default);
}
