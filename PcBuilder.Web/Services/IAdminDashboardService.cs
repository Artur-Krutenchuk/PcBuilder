using PcBuilder.Web.Models.Admin;

namespace PcBuilder.Web.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default);
}
