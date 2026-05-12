using System.Text.Json;
using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Repositories;

public sealed class JsonComponentRepository : IComponentRepository
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<JsonComponentRepository> _logger;

    public JsonComponentRepository(IWebHostEnvironment environment, ILogger<JsonComponentRepository> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Component>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await FileLock.WaitAsync(cancellationToken);
        try
        {
            return await ReadCatalogCoreAsync(cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task SaveCatalogAsync(IReadOnlyList<Component> components, CancellationToken cancellationToken = default)
    {
        await FileLock.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetFilePath();
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var ordered = components.OrderBy(c => c.Type, StringComparer.OrdinalIgnoreCase).ThenBy(c => c.Id).ToList();
            var tempPath = Path.Combine(Path.GetTempPath(), $"buildcores-{Guid.NewGuid():N}.tmp.json");
            await using (var stream = File.Create(tempPath))
            {
                await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
                writer.WriteStartArray();
                foreach (var component in ordered)
                {
                    WriteComponent(writer, component);
                }

                writer.WriteEndArray();
            }

            try
            {
                File.Move(tempPath, filePath, overwrite: true);
            }
            catch
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                throw;
            }

            _logger.LogInformation("Wrote {Count} components to catalog.", ordered.Count);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task<List<Component>> ReadCatalogCoreAsync(CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
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

    private string GetFilePath()
    {
        return Path.Combine(_environment.ContentRootPath, "Data", "buildcores.json");
    }

    private static void WriteComponent(Utf8JsonWriter w, Component component)
    {
        switch (component)
        {
            case Cpu cpu:
                w.WriteStartObject();
                w.WriteString("type", "cpu");
                w.WriteNumber("id", cpu.Id);
                w.WriteString("name", cpu.Name);
                w.WriteString("manufacturer", cpu.Manufacturer);
                w.WriteNumber("price", cpu.Price);
                w.WriteString("imageUrl", cpu.ImageUrl);
                w.WriteString("socket", cpu.Socket);
                w.WriteNumber("tdpWatts", cpu.TdpWatts);
                w.WriteNumber("cores", cpu.Cores);
                w.WriteNumber("threads", cpu.Threads);
                w.WriteNumber("baseClockGhz", cpu.BaseClockGhz);
                WriteNullableString(w, "generation", cpu.Generation);
                WriteNullableString(w, "performanceTier", cpu.PerformanceTier);
                w.WriteNumber("gamingScore", cpu.GamingScore);
                w.WriteNumber("productivityScore", cpu.ProductivityScore);
                w.WriteEndObject();
                break;

            case Motherboard mb:
                w.WriteStartObject();
                w.WriteString("type", "motherboard");
                w.WriteNumber("id", mb.Id);
                w.WriteString("name", mb.Name);
                w.WriteString("manufacturer", mb.Manufacturer);
                w.WriteNumber("price", mb.Price);
                w.WriteString("imageUrl", mb.ImageUrl);
                w.WriteString("socket", mb.Socket);
                WriteStringArray(w, "supportedRamTypes", mb.SupportedRamTypes);
                w.WriteString("chipset", mb.Chipset);
                WriteNullableString(w, "formFactor", mb.FormFactor);
                WriteStringArray(w, "supportedCpuGenerations", mb.SupportedCpuGenerations);
                if (mb.MaxRamFrequencyMhz is { } maxMhz)
                {
                    w.WriteNumber("maxRamFrequencyMhz", maxMhz);
                }

                w.WriteEndObject();
                break;

            case Ram ram:
                w.WriteStartObject();
                w.WriteString("type", "ram");
                w.WriteNumber("id", ram.Id);
                w.WriteString("name", ram.Name);
                w.WriteString("manufacturer", ram.Manufacturer);
                w.WriteNumber("price", ram.Price);
                w.WriteString("imageUrl", ram.ImageUrl);
                w.WriteString("ramType", ram.RamType);
                w.WriteNumber("capacityGb", ram.CapacityGb);
                w.WriteNumber("frequencyMhz", ram.FrequencyMhz);
                WriteNullableString(w, "performanceTier", ram.PerformanceTier);
                w.WriteEndObject();
                break;

            case Gpu gpu:
                w.WriteStartObject();
                w.WriteString("type", "gpu");
                w.WriteNumber("id", gpu.Id);
                w.WriteString("name", gpu.Name);
                w.WriteString("manufacturer", gpu.Manufacturer);
                w.WriteNumber("price", gpu.Price);
                w.WriteString("imageUrl", gpu.ImageUrl);
                w.WriteNumber("tdpWatts", gpu.TdpWatts);
                w.WriteNumber("vramGb", gpu.VramGb);
                w.WriteNumber("recommendedPsuWattage", gpu.RecommendedPsuWattage);
                if (gpu.LengthMm is { } len)
                {
                    w.WriteNumber("lengthMm", len);
                }

                WriteNullableString(w, "performanceTier", gpu.PerformanceTier);
                w.WriteNumber("rasterScore", gpu.RasterScore);
                w.WriteNumber("rayTracingScore", gpu.RayTracingScore);
                w.WriteEndObject();
                break;

            case Psu psu:
                w.WriteStartObject();
                w.WriteString("type", "psu");
                w.WriteNumber("id", psu.Id);
                w.WriteString("name", psu.Name);
                w.WriteString("manufacturer", psu.Manufacturer);
                w.WriteNumber("price", psu.Price);
                w.WriteString("imageUrl", psu.ImageUrl);
                w.WriteNumber("wattage", psu.Wattage);
                w.WriteString("efficiencyRating", psu.EfficiencyRating);
                w.WriteEndObject();
                break;

            case Case pcCase:
                w.WriteStartObject();
                w.WriteString("type", "case");
                w.WriteNumber("id", pcCase.Id);
                w.WriteString("name", pcCase.Name);
                w.WriteString("manufacturer", pcCase.Manufacturer);
                w.WriteNumber("price", pcCase.Price);
                w.WriteString("imageUrl", pcCase.ImageUrl);
                WriteStringArray(w, "supportedMotherboardSizes", pcCase.SupportedMotherboardSizes);
                if (pcCase.MaxGpuLengthMm is { } maxGpu)
                {
                    w.WriteNumber("maxGpuLengthMm", maxGpu);
                }

                w.WriteNumber("includedFans", pcCase.IncludedFans);
                w.WriteNumber("airflowRating", pcCase.AirflowRating);
                w.WriteEndObject();
                break;

            case Cooler cooler:
                w.WriteStartObject();
                w.WriteString("type", "cooler");
                w.WriteNumber("id", cooler.Id);
                w.WriteString("name", cooler.Name);
                w.WriteString("manufacturer", cooler.Manufacturer);
                w.WriteNumber("price", cooler.Price);
                w.WriteString("imageUrl", cooler.ImageUrl);
                WriteStringArray(w, "supportedSockets", cooler.SupportedSockets);
                w.WriteNumber("coolingCapacityWatts", cooler.CoolingCapacityWatts);
                w.WriteNumber("noiseLevelDb", cooler.NoiseLevelDb);
                w.WriteEndObject();
                break;

            default:
                throw new InvalidOperationException($"Unsupported component type: {component.GetType().Name}");
        }
    }

    private static void WriteNullableString(Utf8JsonWriter w, string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        w.WriteString(name, value);
    }

    private static void WriteStringArray(Utf8JsonWriter w, string name, IReadOnlyList<string> values)
    {
        w.WritePropertyName(name);
        w.WriteStartArray();
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                w.WriteStringValue(v.Trim());
            }
        }

        w.WriteEndArray();
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
                SupportedCpuGenerations = GetStringList(element, "supportedCpuGenerations"),
                MaxRamFrequencyMhz = GetNullableInt(element, "maxRamFrequencyMhz")
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
