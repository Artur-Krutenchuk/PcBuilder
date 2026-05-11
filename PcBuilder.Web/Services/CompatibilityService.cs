using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;

namespace PcBuilder.Web.Services;

public sealed class CompatibilityService : ICompatibilityService
{
    private const int BaseSystemReserveWatts = 100;

    public CompatibilityResult Check(SelectedBuild build)
    {
        var result = new CompatibilityResult();

        ValidateRequiredSelections(build, result);
        if (result.Errors.Count > 0)
        {
            return result;
        }

        ValidateCpuMotherboardSocket(build, result);
        ValidateRamSupport(build, result);
        ValidatePowerBudget(build, result);

        return result;
    }

    private static void ValidateRequiredSelections(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Cpu is null) result.Errors.Add("Please select a CPU.");
        if (build.Motherboard is null) result.Errors.Add("Please select a motherboard.");
        if (build.Ram is null) result.Errors.Add("Please select RAM.");
        if (build.Gpu is null) result.Errors.Add("Please select a GPU.");
        if (build.Psu is null) result.Errors.Add("Please select a PSU.");
    }

    private static void ValidateCpuMotherboardSocket(SelectedBuild build, CompatibilityResult result)
    {
        if (string.IsNullOrWhiteSpace(build.Cpu?.Socket) || string.IsNullOrWhiteSpace(build.Motherboard?.Socket))
        {
            result.Errors.Add("CPU or motherboard socket data is missing.");
            return;
        }

        if (!string.Equals(build.Cpu.Socket, build.Motherboard.Socket, StringComparison.OrdinalIgnoreCase))
        {
            result.Errors.Add($"CPU socket '{build.Cpu.Socket}' does not match motherboard socket '{build.Motherboard.Socket}'.");
        }
    }

    private static void ValidateRamSupport(SelectedBuild build, CompatibilityResult result)
    {
        if (string.IsNullOrWhiteSpace(build.Ram?.RamType))
        {
            result.Errors.Add("RAM type data is missing.");
            return;
        }

        var supportedTypes = build.Motherboard?.SupportedRamTypes ?? [];
        if (supportedTypes.Count == 0)
        {
            result.Errors.Add("Motherboard RAM support data is missing.");
            return;
        }

        var isSupported = supportedTypes.Any(type =>
            string.Equals(type, build.Ram.RamType, StringComparison.OrdinalIgnoreCase));

        if (!isSupported)
        {
            result.Errors.Add($"RAM type '{build.Ram.RamType}' is not supported by the selected motherboard.");
        }
    }

    private static void ValidatePowerBudget(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Cpu is null || build.Gpu is null || build.Psu is null || build.Ram is null)
        {
            result.Errors.Add("Power data is missing for CPU, GPU, RAM, or PSU.");
            return;
        }

        var estimatedConsumption = CalculateEstimatedWattage(build.Cpu.TdpWatts, build.Gpu.TdpWatts, build.Ram.CapacityGb);

        if (build.Psu.Wattage < estimatedConsumption)
        {
            result.Errors.Add(
                $"PSU wattage ({build.Psu.Wattage}W) is below estimated system consumption ({estimatedConsumption}W).");
            return;
        }

        if (build.Psu.Wattage < build.Gpu.RecommendedPsuWattage)
        {
            result.Warnings.Add(
                $"PSU wattage ({build.Psu.Wattage}W) can run this build, but the selected GPU recommends {build.Gpu.RecommendedPsuWattage}W or higher.");
        }
    }

    private static int CalculateEstimatedWattage(int cpuTdp, int gpuTdp, int ramCapacityGb)
    {
        var ramReserve = (int)Math.Ceiling((ramCapacityGb / 16m) * 5m);
        return cpuTdp + gpuTdp + BaseSystemReserveWatts + ramReserve;
    }
}
