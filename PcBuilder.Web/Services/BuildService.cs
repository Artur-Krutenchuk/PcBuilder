using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;
using PcBuilder.Web.Models.Components;
using System.Text;

namespace PcBuilder.Web.Services;

public sealed class BuildService : IBuildService
{
    private const int BaseSystemReserveWatts = 100;

    public void HydrateBuild(SelectedBuild build, IReadOnlyList<Component> components)
    {
        build.Cpu = FindById<Cpu>(components, build.CpuId);
        build.Motherboard = FindById<Motherboard>(components, build.MotherboardId);
        build.Ram = FindById<Ram>(components, build.RamId);
        build.Gpu = FindById<Gpu>(components, build.GpuId);
        build.Psu = FindById<Psu>(components, build.PsuId);
        build.Case = FindById<Case>(components, build.CaseId);
        build.Cooler = FindById<Cooler>(components, build.CoolerId);
    }

    public decimal CalculateTotalPrice(SelectedBuild build)
    {
        return (build.Cpu?.Price ?? 0m)
             + (build.Motherboard?.Price ?? 0m)
             + (build.Ram?.Price ?? 0m)
             + (build.Gpu?.Price ?? 0m)
             + (build.Psu?.Price ?? 0m)
             + (build.Case?.Price ?? 0m)
             + (build.Cooler?.Price ?? 0m);
    }

    public int CalculateEstimatedWattage(SelectedBuild build)
    {
        return CalculateEstimatedWattage(
            build.Cpu?.TdpWatts ?? 0,
            build.Gpu?.TdpWatts ?? 0,
            build.Ram?.CapacityGb ?? 0);
    }

    public int CalculateEstimatedWattage(int cpuTdpWatts, int gpuTdpWatts, int ramCapacityGb)
    {
        return EstimateWattageFromTdps(cpuTdpWatts, gpuTdpWatts, ramCapacityGb);
    }

    public BuildExportData ExportBuild(SelectedBuild build, CompatibilityResult compatibility, decimal totalPrice, int estimatedWattage)
    {
        return new BuildExportData(
            DateTime.UtcNow,
            new
            {
                Cpu = build.Cpu is null ? null : new { build.Cpu.Id, build.Cpu.Name, build.Cpu.Manufacturer, build.Cpu.Price, build.Cpu.Socket, build.Cpu.TdpWatts },
                Motherboard = build.Motherboard is null ? null : new { build.Motherboard.Id, build.Motherboard.Name, build.Motherboard.Manufacturer, build.Motherboard.Price, build.Motherboard.Socket, build.Motherboard.FormFactor, build.Motherboard.Chipset },
                Ram = build.Ram is null ? null : new { build.Ram.Id, build.Ram.Name, build.Ram.Manufacturer, build.Ram.Price, build.Ram.RamType, build.Ram.CapacityGb, build.Ram.FrequencyMhz },
                Gpu = build.Gpu is null ? null : new { build.Gpu.Id, build.Gpu.Name, build.Gpu.Manufacturer, build.Gpu.Price, build.Gpu.VramGb, build.Gpu.TdpWatts, build.Gpu.LengthMm },
                Psu = build.Psu is null ? null : new { build.Psu.Id, build.Psu.Name, build.Psu.Manufacturer, build.Psu.Price, build.Psu.Wattage, build.Psu.EfficiencyRating },
                Case = build.Case is null ? null : new { build.Case.Id, build.Case.Name, build.Case.Manufacturer, build.Case.Price, build.Case.MaxGpuLengthMm, build.Case.SupportedMotherboardSizes, build.Case.IncludedFans, build.Case.AirflowRating },
                Cooler = build.Cooler is null ? null : new { build.Cooler.Id, build.Cooler.Name, build.Cooler.Manufacturer, build.Cooler.Price, build.Cooler.SupportedSockets, build.Cooler.CoolingCapacityWatts, build.Cooler.NoiseLevelDb }
            },
            new
            {
                TotalPrice = totalPrice,
                EstimatedWattage = estimatedWattage
            },
            new
            {
                compatibility.IsValid,
                compatibility.Errors,
                compatibility.Warnings,
                compatibility.CompatibilityPercentage,
                compatibility.BuildCategory,
                FpsEstimates = new
                {
                    compatibility.EstimatedFps1080p,
                    compatibility.EstimatedFps1440p,
                    compatibility.EstimatedFps4k,
                    compatibility.EstimatedRayTracingFps,
                    compatibility.EstimatedCompetitiveFps,
                    compatibility.EstimatedAaaFps
                },
                Recommendations = compatibility.Recommendations,
                BottleneckAnalysis = new
                {
                    compatibility.CpuBottleneckPercentage,
                    compatibility.GpuBottleneckPercentage
                }
            });
    }

    public string ExportBuildAsText(SelectedBuild build, CompatibilityResult compatibility, decimal totalPrice, int estimatedWattage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("PC Build Export");
        sb.AppendLine($"Generated (UTC): {DateTime.UtcNow:O}");
        sb.AppendLine();

        sb.AppendLine("Selected parts:");
        AppendPart(sb, "CPU", build.Cpu?.Name, build.Cpu?.Manufacturer, build.Cpu?.Price);
        AppendPart(sb, "Motherboard", build.Motherboard?.Name, build.Motherboard?.Manufacturer, build.Motherboard?.Price);
        AppendPart(sb, "RAM", build.Ram?.Name, build.Ram?.Manufacturer, build.Ram?.Price);
        AppendPart(sb, "GPU", build.Gpu?.Name, build.Gpu?.Manufacturer, build.Gpu?.Price);
        AppendPart(sb, "PSU", build.Psu?.Name, build.Psu?.Manufacturer, build.Psu?.Price);
        AppendPart(sb, "Case", build.Case?.Name, build.Case?.Manufacturer, build.Case?.Price);
        AppendPart(sb, "Cooler", build.Cooler?.Name, build.Cooler?.Manufacturer, build.Cooler?.Price);
        sb.AppendLine();

        sb.AppendLine($"Total price: {totalPrice:C2}");
        sb.AppendLine($"Estimated wattage: {estimatedWattage} W");
        sb.AppendLine();

        sb.AppendLine($"Compatibility: {compatibility.CompatibilityPercentage}% ({(compatibility.IsValid ? "Valid" : "Invalid")})");
        sb.AppendLine($"Category: {compatibility.BuildCategory}");
        sb.AppendLine();

        sb.AppendLine("FPS estimates (Gaming):");
        sb.AppendLine($"- 1080p: {compatibility.EstimatedFps1080p}");
        sb.AppendLine($"- 1440p: {compatibility.EstimatedFps1440p}");
        sb.AppendLine($"- 4K: {compatibility.EstimatedFps4k}");
        sb.AppendLine($"- Ray tracing: {compatibility.EstimatedRayTracingFps}");
        sb.AppendLine($"- Competitive: {compatibility.EstimatedCompetitiveFps}");
        sb.AppendLine($"- AAA: {compatibility.EstimatedAaaFps}");
        sb.AppendLine();

        sb.AppendLine("Bottleneck analysis:");
        sb.AppendLine($"- CPU bottleneck: {compatibility.CpuBottleneckPercentage}%");
        sb.AppendLine($"- GPU bottleneck: {compatibility.GpuBottleneckPercentage}%");
        sb.AppendLine();

        sb.AppendLine("Recommendations:");
        if (compatibility.Recommendations.Count == 0)
        {
            sb.AppendLine("- (none)");
        }
        else
        {
            foreach (var rec in compatibility.Recommendations)
            {
                sb.AppendLine($"- {rec}");
            }
        }

        return sb.ToString();
    }

    private static void AppendPart(StringBuilder sb, string label, string? name, string? manufacturer, decimal? price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            sb.AppendLine($"- {label}: (not selected)");
            return;
        }

        sb.AppendLine($"- {label}: {name} ({manufacturer ?? "Unknown"}) - {(price ?? 0m):C2}");
    }

    private static TComponent? FindById<TComponent>(IReadOnlyList<Component> components, int? id)
        where TComponent : Component
    {
        if (id is null)
        {
            return null;
        }

        return components.OfType<TComponent>().FirstOrDefault(component => component.Id == id.Value);
    }

    private static int EstimateWattageFromTdps(int cpuTdp, int gpuTdp, int ramCapacityGb)
    {
        var totalTdp = cpuTdp + gpuTdp + (ramCapacityGb * 3);
        return totalTdp == 0 ? 0 : totalTdp + BaseSystemReserveWatts;
    }
}
