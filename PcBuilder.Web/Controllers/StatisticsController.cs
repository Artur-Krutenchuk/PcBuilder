using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.ViewModels;
using PcBuilder.Web.Services;

namespace PcBuilder.Web.Controllers;

public sealed class StatisticsController : Controller
{
    private static readonly JsonSerializerOptions ChartJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IComponentService _componentService;

    public StatisticsController(IComponentService componentService)
    {
        _componentService = componentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var components = await _componentService.GetAllAsync(cancellationToken);
        var model = BuildViewModel(components);

        ViewData["Title"] = "Statistics";
        ViewData["StatisticsChartByType"] = SerializeChartPairs(model.ComponentTypeCounts);
        ViewData["StatisticsChartManufacturers"] = SerializeChartPairs(model.ManufacturerCounts);
        ViewData["StatisticsChartSockets"] = SerializeChartPairs(model.SocketCounts);
        ViewData["StatisticsChartAvgPriceByType"] = SerializeAvgPriceChart(model.AveragePriceByType);

        return View(model);
    }

    private static StatisticsViewModel BuildViewModel(IReadOnlyList<Component> components)
    {
        var cpus = components.OfType<Cpu>().ToList();
        var gpus = components.OfType<Gpu>().ToList();
        var motherboards = components.OfType<Motherboard>().ToList();
        var rams = components.OfType<Ram>().ToList();
        var psus = components.OfType<Psu>().ToList();
        var cases = components.OfType<Case>().ToList();
        var coolers = components.OfType<Cooler>().ToList();

        var manufacturerCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in components)
        {
            var key = string.IsNullOrWhiteSpace(c.Manufacturer) ? "Unknown" : c.Manufacturer.Trim();
            manufacturerCounts[key] = manufacturerCounts.GetValueOrDefault(key) + 1;
        }

        var socketCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        void AddSocket(string? socket)
        {
            if (string.IsNullOrWhiteSpace(socket))
            {
                return;
            }

            var key = socket.Trim();
            socketCounts[key] = socketCounts.GetValueOrDefault(key) + 1;
        }

        foreach (var cpu in cpus)
        {
            AddSocket(cpu.Socket);
        }

        foreach (var mb in motherboards)
        {
            AddSocket(mb.Socket);
        }

        foreach (var cooler in coolers)
        {
            foreach (var s in cooler.SupportedSockets)
            {
                AddSocket(s);
            }
        }

        var typeCounts = components
            .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

        var avgByType = components
            .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => Math.Round(g.Average(x => x.Price), 2, MidpointRounding.AwayFromZero),
                StringComparer.OrdinalIgnoreCase);

        return new StatisticsViewModel
        {
            TotalComponents = components.Count,
            CpuCount = cpus.Count,
            GpuCount = gpus.Count,
            MotherboardCount = motherboards.Count,
            RamCount = rams.Count,
            PsuCount = psus.Count,
            CaseCount = cases.Count,
            AverageCpuPrice = cpus.Count == 0
                ? 0m
                : Math.Round(cpus.Average(c => c.Price), 2, MidpointRounding.AwayFromZero),
            AverageGpuPrice = gpus.Count == 0
                ? 0m
                : Math.Round(gpus.Average(g => g.Price), 2, MidpointRounding.AwayFromZero),
            ManufacturerCounts = manufacturerCounts
                .OrderByDescending(kv => kv.Value)
                .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
            SocketCounts = socketCounts
                .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase),
            ComponentTypeCounts = typeCounts,
            AveragePriceByType = avgByType
        };
    }

    private static string SerializeChartPairs(IReadOnlyDictionary<string, int> pairs)
    {
        var labels = pairs.Keys.ToList();
        var data = labels.Select(l => pairs[l]).ToList();
        return JsonSerializer.Serialize(new { labels, data }, ChartJsonOptions);
    }

    private static string SerializeAvgPriceChart(IReadOnlyDictionary<string, decimal> averages)
    {
        var labels = averages.Keys.ToList();
        var data = labels.Select(l => averages[l]).ToList();
        return JsonSerializer.Serialize(new { labels, data }, ChartJsonOptions);
    }
}
