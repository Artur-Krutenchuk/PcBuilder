namespace PcBuilder.Web.Models.Components;

public abstract class Component
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public abstract string Type { get; }
}
