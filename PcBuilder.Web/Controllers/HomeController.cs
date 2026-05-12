using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly IHomeService _homeService;

    public HomeController(IHomeService homeService)
    {
        _homeService = homeService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var model = await _homeService.BuildIndexViewModelAsync(userId, cancellationToken);
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
