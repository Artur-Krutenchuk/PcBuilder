using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Models.ViewModels;

public sealed class ComponentCatalogViewModel
{
    public IReadOnlyList<Component> Components { get; init; } = [];

    public string? Search { get; init; }

    public string? Type { get; init; }

    public string? Manufacturer { get; init; }

    public string? Socket { get; init; }

    public string? SortBy { get; init; }

    /// <summary>Distinct types for filter dropdown (full catalog).</summary>
    public IReadOnlyList<string> AvailableTypes { get; init; } = [];

    /// <summary>Distinct manufacturers for filter dropdown (full catalog).</summary>
    public IReadOnlyList<string> AvailableManufacturers { get; init; } = [];

    /// <summary>Distinct sockets for filter dropdown (from CPUs, boards, coolers).</summary>
    public IReadOnlyList<string> AvailableSockets { get; init; } = [];
}
