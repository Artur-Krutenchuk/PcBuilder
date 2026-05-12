using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.ViewModels;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

public sealed class ComponentsController : Controller
{
    private readonly IComponentService _componentService;

    public ComponentsController(IComponentService componentService)
    {
        _componentService = componentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? type,
        string? manufacturer,
        string? socket,
        string? sortBy,
        CancellationToken cancellationToken)
    {
        var all = (await _componentService.GetAllAsync(cancellationToken)).ToList();

        var availableTypes = all.Select(c => c.Type)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var availableManufacturers = all
            .Select(c => c.Manufacturer.Trim())
            .Where(m => m.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var socketSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in all)
        {
            switch (c)
            {
                case Cpu cpu when !string.IsNullOrWhiteSpace(cpu.Socket):
                    socketSet.Add(cpu.Socket.Trim());
                    break;
                case Motherboard mb when !string.IsNullOrWhiteSpace(mb.Socket):
                    socketSet.Add(mb.Socket.Trim());
                    break;
                case Cooler cooler:
                    foreach (var s in cooler.SupportedSockets)
                    {
                        if (!string.IsNullOrWhiteSpace(s))
                        {
                            socketSet.Add(s.Trim());
                        }
                    }

                    break;
            }
        }

        var availableSockets = socketSet.OrderBy(s => s, StringComparer.OrdinalIgnoreCase).ToList();

        IEnumerable<Component> query = all;

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            var t = type.Trim();
            query = query.Where(c => string.Equals(c.Type, t, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(manufacturer))
        {
            var m = manufacturer.Trim();
            query = query.Where(c => string.Equals(c.Manufacturer.Trim(), m, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(socket))
        {
            var sk = socket.Trim();
            query = query.Where(c => MatchesSocketFilter(c, sk));
        }

        var normalizedSort = string.IsNullOrWhiteSpace(sortBy) ? "name" : sortBy.Trim().ToLowerInvariant();
        var list = normalizedSort switch
        {
            "price_asc" => query.OrderBy(c => c.Price).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            "price_desc" => query.OrderByDescending(c => c.Price).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            "manufacturer" => query.OrderBy(c => c.Manufacturer, StringComparer.OrdinalIgnoreCase)
                .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => query.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList()
        };

        var model = new ComponentCatalogViewModel
        {
            Components = list,
            Search = search,
            Type = type,
            Manufacturer = manufacturer,
            Socket = socket,
            SortBy = normalizedSort,
            AvailableTypes = availableTypes,
            AvailableManufacturers = availableManufacturers,
            AvailableSockets = availableSockets
        };

        ViewData["Title"] = "Catalog";
        return View(model);
    }

    private static bool MatchesSocketFilter(Component component, string socket)
    {
        return component switch
        {
            Cpu cpu => string.Equals(cpu.Socket?.Trim(), socket, StringComparison.OrdinalIgnoreCase),
            Motherboard mb => string.Equals(mb.Socket?.Trim(), socket, StringComparison.OrdinalIgnoreCase),
            Cooler cooler => cooler.SupportedSockets.Any(s =>
                !string.IsNullOrWhiteSpace(s) && string.Equals(s.Trim(), socket, StringComparison.OrdinalIgnoreCase)),
            _ => true
        };
    }
}
