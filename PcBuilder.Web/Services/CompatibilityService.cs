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
        ValidateMotherboardCase(build, result);
        ValidateGpuCase(build, result);
        ValidateCpuGenerationMotherboard(build, result);
        ValidateRamFrequency(build, result);
        ValidatePowerBudget(build, result);
    private static void ValidateMotherboardCase(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Motherboard is null || build.Case is null)
            return;
        var mbForm = build.Motherboard.FormFactor;
        var caseForms = build.Case.SupportedFormFactors;
        if (string.IsNullOrWhiteSpace(mbForm) || caseForms.Count == 0)
            return;
        if (!caseForms.Any(f => string.Equals(f, mbForm, StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add("Motherboard form factor is not supported by the selected case.");
        }
    }

    private static void ValidateGpuCase(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Gpu?.LengthMm is null || build.Case?.MaxGpuLengthMm is null)
            return;
        if (build.Gpu.LengthMm > build.Case.MaxGpuLengthMm)
        {
            result.Errors.Add("GPU is too large for the selected case.");
        }
    }

    private static void ValidateCpuGenerationMotherboard(SelectedBuild build, CompatibilityResult result)
    {
        if (string.IsNullOrWhiteSpace(build.Cpu?.Generation) || build.Motherboard?.SupportedCpuGenerations is null)
            return;
        if (!build.Motherboard.SupportedCpuGenerations.Any(gen => string.Equals(gen, build.Cpu.Generation, StringComparison.OrdinalIgnoreCase)))
        {
            result.Errors.Add("Motherboard chipset does not support this CPU generation.");
        }
    }

    private static void ValidateRamFrequency(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Ram is null || build.Motherboard is null)
            return;
        if (build.Motherboard.MaxRamFrequencyMhz is null)
            return;
        if (build.Ram.FrequencyMhz > build.Motherboard.MaxRamFrequencyMhz)
        {
            result.Warnings.Add("RAM frequency exceeds motherboard supported speed and may run at lower frequency.");
        }
    }

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
