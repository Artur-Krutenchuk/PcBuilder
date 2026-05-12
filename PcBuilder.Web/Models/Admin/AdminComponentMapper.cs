using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Models.Admin;

public static class AdminComponentMapper
{
    public static AdminComponentEditorViewModel FromComponent(Component component)
    {
        var vm = new AdminComponentEditorViewModel
        {
            Id = component.Id,
            Name = component.Name,
            Manufacturer = component.Manufacturer,
            Price = component.Price,
            ImageUrl = component.ImageUrl
        };

        switch (component)
        {
            case Cpu cpu:
                vm.ComponentType = "cpu";
                vm.Socket = cpu.Socket;
                vm.TdpWatts = cpu.TdpWatts;
                vm.Cores = cpu.Cores;
                vm.Threads = cpu.Threads;
                vm.BaseClockGhz = cpu.BaseClockGhz;
                vm.Generation = cpu.Generation;
                vm.PerformanceTier = cpu.PerformanceTier;
                vm.GamingScore = cpu.GamingScore;
                vm.ProductivityScore = cpu.ProductivityScore;
                break;
            case Motherboard mb:
                vm.ComponentType = "motherboard";
                vm.Socket = mb.Socket;
                vm.SupportedRamTypesCsv = string.Join(", ", mb.SupportedRamTypes);
                vm.Chipset = mb.Chipset;
                vm.FormFactor = mb.FormFactor;
                vm.SupportedCpuGenerationsCsv = string.Join(", ", mb.SupportedCpuGenerations);
                vm.MaxRamFrequencyMhz = mb.MaxRamFrequencyMhz;
                break;
            case Ram ram:
                vm.ComponentType = "ram";
                vm.RamType = ram.RamType;
                vm.CapacityGb = ram.CapacityGb;
                vm.FrequencyMhz = ram.FrequencyMhz;
                vm.PerformanceTier = ram.PerformanceTier;
                break;
            case Gpu gpu:
                vm.ComponentType = "gpu";
                vm.TdpWatts = gpu.TdpWatts;
                vm.VramGb = gpu.VramGb;
                vm.RecommendedPsuWattage = gpu.RecommendedPsuWattage;
                vm.LengthMm = gpu.LengthMm;
                vm.PerformanceTier = gpu.PerformanceTier;
                vm.RasterScore = gpu.RasterScore;
                vm.RayTracingScore = gpu.RayTracingScore;
                break;
            case Psu psu:
                vm.ComponentType = "psu";
                vm.Wattage = psu.Wattage;
                vm.EfficiencyRating = psu.EfficiencyRating;
                break;
            case Case pcCase:
                vm.ComponentType = "case";
                vm.SupportedMotherboardSizesCsv = string.Join(", ", pcCase.SupportedMotherboardSizes);
                vm.MaxGpuLengthMm = pcCase.MaxGpuLengthMm;
                vm.IncludedFans = pcCase.IncludedFans;
                vm.AirflowRating = pcCase.AirflowRating;
                break;
            case Cooler cooler:
                vm.ComponentType = "cooler";
                vm.SupportedSocketsCsv = string.Join(", ", cooler.SupportedSockets);
                vm.CoolingCapacityWatts = cooler.CoolingCapacityWatts;
                vm.NoiseLevelDb = cooler.NoiseLevelDb;
                break;
        }

        return vm;
    }

    public static bool TryToComponent(AdminComponentEditorViewModel vm, int targetId, out Component? component, out string? error)
    {
        component = null;
        error = null;

        var type = (vm.ComponentType ?? string.Empty).Trim().ToLowerInvariant();
        var name = vm.Name.Trim();
        var manufacturer = vm.Manufacturer.Trim();
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(manufacturer))
        {
            error = "Name and manufacturer are required.";
            return false;
        }

        switch (type)
        {
            case "cpu":
                if (string.IsNullOrWhiteSpace(vm.Socket))
                {
                    error = "CPU socket is required.";
                    return false;
                }

                component = new Cpu
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    Socket = vm.Socket.Trim(),
                    TdpWatts = vm.TdpWatts,
                    Cores = vm.Cores,
                    Threads = vm.Threads,
                    BaseClockGhz = vm.BaseClockGhz,
                    Generation = string.IsNullOrWhiteSpace(vm.Generation) ? null : vm.Generation.Trim(),
                    PerformanceTier = string.IsNullOrWhiteSpace(vm.PerformanceTier) ? null : vm.PerformanceTier.Trim(),
                    GamingScore = vm.GamingScore,
                    ProductivityScore = vm.ProductivityScore
                };
                return true;

            case "motherboard":
                if (string.IsNullOrWhiteSpace(vm.Socket))
                {
                    error = "Motherboard socket is required.";
                    return false;
                }

                var ramTypes = SplitCsv(vm.SupportedRamTypesCsv);
                var gens = SplitCsv(vm.SupportedCpuGenerationsCsv);
                if (ramTypes.Count == 0)
                {
                    error = "Enter at least one supported RAM type.";
                    return false;
                }

                component = new Motherboard
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    Socket = vm.Socket.Trim(),
                    SupportedRamTypes = ramTypes,
                    Chipset = vm.Chipset.Trim(),
                    FormFactor = string.IsNullOrWhiteSpace(vm.FormFactor) ? null : vm.FormFactor.Trim(),
                    SupportedCpuGenerations = gens,
                    MaxRamFrequencyMhz = vm.MaxRamFrequencyMhz
                };
                return true;

            case "ram":
                if (string.IsNullOrWhiteSpace(vm.RamType))
                {
                    error = "RAM type is required.";
                    return false;
                }

                component = new Ram
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    RamType = vm.RamType.Trim(),
                    CapacityGb = vm.CapacityGb,
                    FrequencyMhz = vm.FrequencyMhz,
                    PerformanceTier = string.IsNullOrWhiteSpace(vm.PerformanceTier) ? null : vm.PerformanceTier.Trim()
                };
                return true;

            case "gpu":
                component = new Gpu
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    TdpWatts = vm.TdpWatts,
                    VramGb = vm.VramGb,
                    RecommendedPsuWattage = vm.RecommendedPsuWattage,
                    LengthMm = vm.LengthMm,
                    PerformanceTier = string.IsNullOrWhiteSpace(vm.PerformanceTier) ? null : vm.PerformanceTier.Trim(),
                    RasterScore = vm.RasterScore,
                    RayTracingScore = vm.RayTracingScore
                };
                return true;

            case "psu":
                if (vm.Wattage <= 0)
                {
                    error = "PSU wattage must be greater than zero.";
                    return false;
                }

                component = new Psu
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    Wattage = vm.Wattage,
                    EfficiencyRating = string.IsNullOrWhiteSpace(vm.EfficiencyRating) ? "80+ Bronze" : vm.EfficiencyRating.Trim()
                };
                return true;

            case "case":
                var sizes = SplitCsv(vm.SupportedMotherboardSizesCsv);
                if (sizes.Count == 0)
                {
                    error = "Enter at least one supported motherboard size.";
                    return false;
                }

                component = new Case
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    SupportedMotherboardSizes = sizes,
                    MaxGpuLengthMm = vm.MaxGpuLengthMm,
                    IncludedFans = vm.IncludedFans,
                    AirflowRating = vm.AirflowRating
                };
                return true;

            case "cooler":
                var sockets = SplitCsv(vm.SupportedSocketsCsv);
                if (sockets.Count == 0)
                {
                    error = "Enter at least one supported socket.";
                    return false;
                }

                component = new Cooler
                {
                    Id = targetId,
                    Name = name,
                    Manufacturer = manufacturer,
                    Price = vm.Price,
                    ImageUrl = vm.ImageUrl?.Trim() ?? string.Empty,
                    SupportedSockets = sockets,
                    CoolingCapacityWatts = vm.CoolingCapacityWatts,
                    NoiseLevelDb = vm.NoiseLevelDb
                };
                return true;

            default:
                error = "Unknown component type.";
                return false;
        }
    }

    private static List<string> SplitCsv(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv))
        {
            return [];
        }

        return csv.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
    }
}
