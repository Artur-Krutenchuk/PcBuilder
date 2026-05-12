using PcBuilder.Web.Models;

namespace PcBuilder.Web.Services;

public interface IHomeService
{
    Task<HomeIndexViewModel> BuildIndexViewModelAsync(string? userId, CancellationToken cancellationToken = default);
}
