using System.Text.Json;
using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Repositories;

public sealed class JsonComponentRepository : IComponentRepository
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<JsonComponentRepository> _logger;

    public JsonComponentRepository(IWebHostEnvironment environment, ILogger<JsonComponentRepository> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, "Data", "buildcores.json");
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Components JSON file does not exist: {Path}", filePath);
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var components = new List<Component>();
        foreach (var element in document.RootElement.EnumerateArray())
        {
            if (!element.TryGetProperty("type", out var typeProperty))
            {
                continue;
            }

            var type = typeProperty.GetString();
            if (string.IsNullOrWhiteSpace(type))
            {
                continue;
            }

            var component = CreateComponent(type, element);
            if (component is not null)
            {
                components.Add(component);
            }
        }

        return components;
    }

    private static Component? CreateComponent(string type, JsonElement element)
    {
        return type.ToLowerInvariant() switch
        {
            "cpu" => new Cpu
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                Socket = GetString(element, "socket"),
                TdpWatts = GetInt(element, "tdpWatts"),
                Cores = GetInt(element, "cores"),
                Threads = GetInt(element, "threads"),
                BaseClockGhz = GetDecimal(element, "baseClockGhz"),
                Generation = GetNullableString(element, "generation"),
                PerformanceTier = GetNullableString(element, "performanceTier"),
                GamingScore = GetInt(element, "gamingScore"),
                ProductivityScore = GetInt(element, "productivityScore")
            },
            "motherboard" => new Motherboard
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                Socket = GetString(element, "socket"),
                SupportedRamTypes = GetStringList(element, "supportedRamTypes"),
                Chipset = GetString(element, "chipset"),
                FormFactor = GetNullableString(element, "formFactor"),
                SupportedCpuGenerations = GetStringList(element, "supportedCpuGenerations")
            },
            "ram" => new Ram
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                RamType = GetString(element, "ramType"),
                CapacityGb = GetInt(element, "capacityGb"),
                FrequencyMhz = GetInt(element, "frequencyMhz"),
                PerformanceTier = GetNullableString(element, "performanceTier")
            },
            "gpu" => new Gpu
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                TdpWatts = GetInt(element, "tdpWatts"),
                VramGb = GetInt(element, "vramGb"),
                RecommendedPsuWattage = GetInt(element, "recommendedPsuWattage"),
                LengthMm = GetNullableInt(element, "lengthMm"),
                PerformanceTier = GetNullableString(element, "performanceTier"),
                RasterScore = GetInt(element, "rasterScore"),
                RayTracingScore = GetInt(element, "rayTracingScore")
            },
            "case" => new Case
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                SupportedMotherboardSizes = GetStringListFallback(element, "supportedMotherboardSizes", "supportedFormFactors"),
                MaxGpuLengthMm = GetNullableInt(element, "maxGpuLengthMm"),
                IncludedFans = GetInt(element, "includedFans"),
                AirflowRating = GetInt(element, "airflowRating")
            },
            "cooler" => new Cooler
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                SupportedSockets = GetStringList(element, "supportedSockets"),
                CoolingCapacityWatts = GetInt(element, "coolingCapacityWatts"),
                NoiseLevelDb = GetDecimal(element, "noiseLevelDb")
            },
            "psu" => new Psu
            {
                Id = GetInt(element, "id"),
                Name = GetString(element, "name"),
                Manufacturer = GetString(element, "manufacturer"),
                Price = GetDecimal(element, "price"),
                ImageUrl = GetString(element, "imageUrl"),
                Wattage = GetInt(element, "wattage"),
                EfficiencyRating = GetString(element, "efficiencyRating")
            },
            _ => null
        };
    }

    private static int GetInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value)
            ? value
            : 0;
    }

    private static decimal GetDecimal(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.TryGetDecimal(out var value)
            ? value
            : 0m;
    }

    private static string GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property)
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static string? GetNullableString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static int? GetNullableInt(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var property) && property.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static List<string> GetStringList(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            var value = item.GetString();
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return values;
    }

    private static List<string> GetStringListFallback(JsonElement element, string primary, string fallback)
    {
        var primaryValues = GetStringList(element, primary);
        return primaryValues.Count > 0 ? primaryValues : GetStringList(element, fallback);
    }
}
