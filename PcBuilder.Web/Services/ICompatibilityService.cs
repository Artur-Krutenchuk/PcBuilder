using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;
using PcBuilder.Web.Models;

namespace PcBuilder.Web.Services;

public interface ICompatibilityService
{
    CompatibilityResult Check(SelectedBuild build);

    BuildComparisonResult CompareBuilds(SelectedBuild buildA, SelectedBuild buildB);
}
