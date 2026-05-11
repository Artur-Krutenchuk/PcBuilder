using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;

namespace PcBuilder.Web.Services;

public interface ICompatibilityService
{
    CompatibilityResult Check(SelectedBuild build);
}
