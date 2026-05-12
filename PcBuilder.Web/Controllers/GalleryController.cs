using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

public sealed class GalleryController : Controller
{
    private readonly IGalleryService _galleryService;

    public GalleryController(IGalleryService galleryService)
    {
        _galleryService = galleryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? q,
        string? category,
        string? sort,
        CancellationToken cancellationToken)
    {
        var result = await _galleryService.FilterAndSortBuildsAsync(q, category, sort, cancellationToken);

        var model = new GalleryIndexViewModel
        {
            Builds = result.Builds,
            Categories = result.Categories,
            SearchQuery = result.SearchQuery,
            Category = result.Category,
            Sort = result.Sort
        };

        return View(model);
    }
}
