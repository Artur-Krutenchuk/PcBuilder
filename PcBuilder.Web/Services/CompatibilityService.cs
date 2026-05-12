using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Responses;
using PcBuilder.Web.Models;

namespace PcBuilder.Web.Services;

public sealed class CompatibilityService : ICompatibilityService
{
    private const int BaseSystemReserveWatts = 100;
    private const int OptimalCompatibilityThreshold = 90;
    private const int AcceptableCompatibilityThreshold = 70;

    public CompatibilityResult Check(SelectedBuild build)
    {
        var result = new CompatibilityResult();

        ValidateRequiredSelections(build, result);
        PopulateMetrics(build, result);

        if (result.Errors.Count > 0)
        {
            result.CompatibilityPercentage = Math.Min(result.CompatibilityPercentage, 60);
            return result;
        }

        ValidateCpuMotherboardSocket(build, result);
        ValidateCoolerSocketCompatibility(build, result);
        ValidateRamSupport(build, result);
        ValidateMotherboardCase(build, result);
        ValidateGpuCase(build, result);
        ValidateCpuGenerationMotherboard(build, result);
        ValidateRamFrequency(build, result);
        ValidatePowerBudget(build, result);

        ApplyCompatibilityScore(build, result);
        AssignBadges(build, result);
        PopulateBottleneckEstimates(build, result);
        PopulateRecommendations(build, result);
        return result;
    }

    public BuildComparisonResult CompareBuilds(SelectedBuild buildA, SelectedBuild buildB)
    {
        var snapshotA = CreateSnapshot("Build A", buildA);
        var snapshotB = CreateSnapshot("Build B", buildB);

        return new BuildComparisonResult
        {
            BuildA = snapshotA,
            BuildB = snapshotB,
            Metrics =
            [
                CompareMetric("Total price", snapshotA.TotalPrice, snapshotB.TotalPrice, "C2", higherIsBetter: false),
                CompareMetric("Estimated wattage", snapshotA.EstimatedWattage, snapshotB.EstimatedWattage, "N0", higherIsBetter: false, " W"),
                CompareMetric("Compatibility", snapshotA.CompatibilityPercentage, snapshotB.CompatibilityPercentage, "N0", higherIsBetter: true, "%"),
                CompareMetric("Average FPS", snapshotA.AverageFps, snapshotB.AverageFps, "N0", higherIsBetter: true),
                CompareMetric("Thermal score", snapshotA.ThermalScore, snapshotB.ThermalScore, "N0", higherIsBetter: true),
                CompareMetric("PSU health", snapshotA.PsuHealthScore, snapshotB.PsuHealthScore, "N0", higherIsBetter: true),
                CompareMetric("Bottleneck", snapshotA.BottleneckPercentage, snapshotB.BottleneckPercentage, "N0", higherIsBetter: false, "%"),
                CompareMetric("Value score (FPS/$)", snapshotA.ValueScore, snapshotB.ValueScore, "0.###", higherIsBetter: true)
            ]
        };
    }

    private BuildSnapshot CreateSnapshot(string label, SelectedBuild build)
    {
        var compatibility = Check(build);
        var averageFps = CalculateAverageFps(compatibility.EstimatedFps1080p, compatibility.EstimatedFps1440p, compatibility.EstimatedFps4k);
        var totalPrice = (build.Cpu?.Price ?? 0m)
                         + (build.Motherboard?.Price ?? 0m)
                         + (build.Ram?.Price ?? 0m)
                         + (build.Gpu?.Price ?? 0m)
                         + (build.Psu?.Price ?? 0m)
                         + (build.Case?.Price ?? 0m)
                         + (build.Cooler?.Price ?? 0m);
        var bottleneck = Math.Max(compatibility.CpuBottleneckPercentage, compatibility.GpuBottleneckPercentage);
        var valueScore = totalPrice <= 0m ? 0m : Math.Round(averageFps / totalPrice, 3);

        return new BuildSnapshot
        {
            Label = label,
            BuildCategory = compatibility.BuildCategory,
            TotalPrice = totalPrice,
            EstimatedWattage = compatibility.EstimatedSystemWattage,
            CompatibilityPercentage = compatibility.CompatibilityPercentage,
            AverageFps = averageFps,
            ThermalScore = compatibility.ThermalHealthScore,
            PsuHealthScore = compatibility.PsuHealthScore,
            BottleneckPercentage = bottleneck,
            ValueScore = valueScore
        };
    }

    private static int CalculateAverageFps(int fps1080p, int fps1440p, int fps4k)
    {
        var values = new[] { fps1080p, fps1440p, fps4k }.Where(value => value > 0).ToArray();
        if (values.Length == 0)
        {
            return 0;
        }

        return (int)Math.Round(values.Average());
    }

    private static ComparisonMetric CompareMetric(
        string label,
        decimal valueA,
        decimal valueB,
        string format,
        bool higherIsBetter,
        string suffix = "")
    {
        bool? isABetter = valueA == valueB ? null : higherIsBetter ? valueA > valueB : valueA < valueB;
        return new ComparisonMetric
        {
            Label = label,
            BuildAValue = $"{valueA.ToString(format)}{suffix}",
            BuildBValue = $"{valueB.ToString(format)}{suffix}",
            IsBuildABetter = isABetter
        };
    }

    private static void PopulateMetrics(SelectedBuild build, CompatibilityResult result)
    {
        result.EstimatedSystemWattage = CalculateEstimatedWattage(
            build.Cpu?.TdpWatts ?? 0,
            build.Gpu?.TdpWatts ?? 0,
            build.Ram?.CapacityGb ?? 0);

        var airflowFactor = GetCaseAirflowFactor(build.Case);
        var psuHeadroomRatio = CalculatePsuHeadroomRatio(result.EstimatedSystemWattage, build.Psu?.Wattage ?? 0);
        var coolerEffectiveness = GetCoolerEffectiveness(build.Cpu?.TdpWatts ?? 0, build.Cooler);

        result.EstimatedCpuTemperatureCelsius = EstimateCpuTemp(build.Cpu?.TdpWatts ?? 0, airflowFactor, psuHeadroomRatio, coolerEffectiveness);
        result.EstimatedGpuTemperatureCelsius = EstimateGpuTemp(build.Gpu?.TdpWatts ?? 0, airflowFactor, psuHeadroomRatio);
        result.PsuHealthScore = CalculatePsuHealthScore(result.EstimatedSystemWattage, build.Psu?.Wattage ?? 0);
        result.ThermalHealthScore = CalculateThermalHealthScore(result.EstimatedCpuTemperatureCelsius, result.EstimatedGpuTemperatureCelsius);
        result.EfficiencyScore = CalculateEfficiencyScore(build.Psu?.EfficiencyRating, result.PsuHealthScore, result.ThermalHealthScore);
        result.BuildCategory = DetectBuildCategory(build);

        var fps = EstimateFpsProfile(build, result.BuildCategory);
        result.EstimatedFps1080p = fps.Fps1080p;
        result.EstimatedFps1440p = fps.Fps1440p;
        result.EstimatedFps4k = fps.Fps4k;
        result.EstimatedRayTracingFps = fps.RayTracingFps;
        result.EstimatedCompetitiveFps = fps.CompetitiveFps;
        result.EstimatedAaaFps = fps.AaaFps;
    }

    private static void ValidateMotherboardCase(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Motherboard is null || build.Case is null)
            return;

        var mbForm = build.Motherboard.FormFactor;
        var caseForms = build.Case.SupportedMotherboardSizes;

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

    private static void ValidateRequiredSelections(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Cpu is null) result.Errors.Add("Please select a CPU.");
        if (build.Motherboard is null) result.Errors.Add("Please select a motherboard.");
        if (build.Ram is null) result.Errors.Add("Please select RAM.");
        if (build.Gpu is null) result.Errors.Add("Please select a GPU.");
        if (build.Psu is null) result.Errors.Add("Please select a PSU.");
        if (build.Cooler is null) result.Errors.Add("Please select a CPU cooler.");
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

    private static void ValidateCoolerSocketCompatibility(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Cpu is null || build.Cooler is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(build.Cpu.Socket))
        {
            result.Errors.Add("CPU socket data is missing.");
            return;
        }

        if (build.Cooler.SupportedSockets.Count == 0)
        {
            result.Errors.Add("Cooler socket support data is missing.");
            return;
        }

        var isSupported = build.Cooler.SupportedSockets.Any(socket =>
            string.Equals(socket, build.Cpu.Socket, StringComparison.OrdinalIgnoreCase));

        if (!isSupported)
        {
            result.Errors.Add($"Selected cooler does not support CPU socket '{build.Cpu.Socket}'.");
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

    private static void ApplyCompatibilityScore(SelectedBuild build, CompatibilityResult result)
    {
        var score = 100;
        score -= result.Errors.Count * 20;
        score -= result.Warnings.Count * 7;

        if (build.Psu is not null && result.EstimatedSystemWattage > 0)
        {
            var headroomRatio = CalculatePsuHeadroomRatio(result.EstimatedSystemWattage, build.Psu.Wattage);
            if (headroomRatio < 0.2m)
            {
                score -= 10;
            }
            else if (headroomRatio > 0.45m)
            {
                score += 4;
            }
        }

        score += (result.ThermalHealthScore - 50) / 5;
        score += (result.EfficiencyScore - 50) / 6;
        result.CompatibilityPercentage = Math.Clamp(score, 0, 100);
    }

    private static void AssignBadges(SelectedBuild build, CompatibilityResult result)
    {
        if (result.CompatibilityPercentage >= 92 && result.ThermalHealthScore >= 75 && result.PsuHealthScore >= 70)
        {
            result.BuildBadges.Add("Excellent Build");
        }

        if (result.CompatibilityPercentage >= AcceptableCompatibilityThreshold && result.CompatibilityPercentage <= OptimalCompatibilityThreshold)
        {
            result.BuildBadges.Add("Balanced Build");
        }

        if (build.Cpu is not null && build.Gpu is not null &&
            GetCpuTierScore(build.Cpu.PerformanceTier) >= 4 &&
            GetGpuTierScore(build.Gpu.PerformanceTier) >= 4)
        {
            result.BuildBadges.Add("High Performance");
        }

        if (result.EstimatedSystemWattage > 550 || result.PsuHealthScore < 50)
        {
            result.BuildBadges.Add("Power Hungry");
        }

        var totalPrice = (build.Cpu?.Price ?? 0m)
                         + (build.Motherboard?.Price ?? 0m)
                         + (build.Ram?.Price ?? 0m)
                         + (build.Gpu?.Price ?? 0m)
                         + (build.Psu?.Price ?? 0m)
                         + (build.Case?.Price ?? 0m)
                         + (build.Cooler?.Price ?? 0m);

        if (totalPrice > 0m && totalPrice <= 1100m)
        {
            result.BuildBadges.Add("Budget Friendly");
        }
    }

    private static int CalculateEstimatedWattage(int cpuTdp, int gpuTdp, int ramCapacityGb)
    {
        if (cpuTdp == 0 && gpuTdp == 0 && ramCapacityGb == 0)
        {
            return 0;
        }

        var ramReserve = (int)Math.Ceiling((ramCapacityGb / 16m) * 5m);
        return cpuTdp + gpuTdp + BaseSystemReserveWatts + ramReserve;
    }

    private static string DetectBuildCategory(SelectedBuild build)
    {
        if (build.Cpu is null || build.Gpu is null || build.Ram is null)
        {
            return "Uncategorized";
        }

        var cpu = build.Cpu;
        var gpu = build.Gpu;
        var ram = build.Ram;

        var cpuTier = GetCpuTierScore(cpu.PerformanceTier);
        var gpuTier = GetGpuTierScore(gpu.PerformanceTier);
        var price = cpu.Price + gpu.Price + ram.Price + (build.Psu?.Price ?? 0m);

        if (ram.CapacityGb >= 64 || cpu.Cores >= 12)
        {
            return "Workstation";
        }

        if (cpuTier >= 3 && gpuTier >= 3)
        {
            return "Gaming";
        }

        if (cpuTier >= 3 && gpuTier >= 2 && (cpu.Cores >= 8 || cpu.Threads >= 16))
        {
            return "Streaming";
        }

        if (price > 0m && price <= 950m)
        {
            return "Budget";
        }

        return "Gaming";
    }

    private static (int Fps1080p, int Fps1440p, int Fps4k, int RayTracingFps, int CompetitiveFps, int AaaFps) EstimateFpsProfile(
        SelectedBuild build,
        string buildCategory)
    {
        if (!string.Equals(buildCategory, "Gaming", StringComparison.OrdinalIgnoreCase) || build.Cpu is null || build.Gpu is null)
        {
            return (0, 0, 0, 0, 0, 0);
        }

        var gpuRasterScore = build.Gpu.RasterScore > 0 ? build.Gpu.RasterScore : (GetGpuTierScore(build.Gpu.PerformanceTier) * 60);
        var gpuRtScore = build.Gpu.RayTracingScore > 0 ? build.Gpu.RayTracingScore : (int)Math.Round(gpuRasterScore * 0.72m);
        var cpuGamingScore = build.Cpu.GamingScore > 0 ? build.Cpu.GamingScore : (GetCpuTierScore(build.Cpu.PerformanceTier) * 55);

        var cpuToGpuRatio = cpuGamingScore / Math.Max(1m, gpuRasterScore * 0.85m);
        var cpuBottleneckModifier = Math.Clamp(cpuToGpuRatio, 0.68m, 1.06m);

        var fps1080p = Math.Clamp((int)Math.Round((gpuRasterScore * 0.50m + 18m) * cpuBottleneckModifier), 35, 280);
        var fps1440p = Math.Max(24, (int)Math.Round(fps1080p * 0.72m));
        var fps4k = Math.Max(16, (int)Math.Round(fps1080p * 0.44m));
        var rayTracingFps = Math.Max(14, (int)Math.Round((gpuRtScore * 0.40m + 12m) * cpuBottleneckModifier));

        var cpuCeilingBonus = Math.Clamp((int)Math.Round((cpuGamingScore - 120) * 0.30m), -12, 45);
        var competitiveFps = Math.Clamp((int)Math.Round(fps1080p * 1.35m) + cpuCeilingBonus, 60, 360);
        var aaaFps = Math.Max(18, (int)Math.Round((fps1440p * 0.70m) + (fps4k * 0.30m)));
        return (fps1080p, fps1440p, fps4k, rayTracingFps, competitiveFps, aaaFps);
    }

    private static int EstimateCpuTemp(int cpuTdp, decimal airflowFactor, decimal psuHeadroomRatio, decimal coolerEffectiveness)
    {
        if (cpuTdp <= 0)
        {
            return 0;
        }

        var baseline = 34m + (cpuTdp * 0.23m);
        var coolerAdjustment = (1m - Math.Clamp(coolerEffectiveness, 0.2m, 1.15m)) * 14m;
        var coolingAdjustment = (1m - airflowFactor) * 10m;
        var headroomAdjustment = psuHeadroomRatio < 0.15m ? 4m : 0m;
        return (int)Math.Clamp(Math.Round(baseline + coolerAdjustment + coolingAdjustment + headroomAdjustment), 35m, 98m);
    }

    private static int EstimateGpuTemp(int gpuTdp, decimal airflowFactor, decimal psuHeadroomRatio)
    {
        if (gpuTdp <= 0)
        {
            return 0;
        }

        var baseline = 41m + (gpuTdp * 0.10m);
        var coolingAdjustment = (1m - airflowFactor) * 12m;
        var headroomAdjustment = psuHeadroomRatio < 0.15m ? 3m : 0m;
        return (int)Math.Clamp(Math.Round(baseline + coolingAdjustment + headroomAdjustment), 48m, 96m);
    }

    private static int CalculatePsuHealthScore(int estimatedWattage, int psuWattage)
    {
        if (estimatedWattage <= 0 || psuWattage <= 0)
        {
            return 0;
        }

        var ratio = CalculatePsuHeadroomRatio(estimatedWattage, psuWattage);
        if (ratio < 0m)
        {
            return 0;
        }

        var raw = 35m + (ratio * 120m);
        return (int)Math.Clamp(Math.Round(raw), 0m, 100m);
    }

    private static int CalculateThermalHealthScore(int cpuTemp, int gpuTemp)
    {
        if (cpuTemp == 0 && gpuTemp == 0)
        {
            return 0;
        }

        var maxTemp = Math.Max(cpuTemp, gpuTemp);
        var score = 100 - ((maxTemp - 45) * 2);
        return Math.Clamp(score, 0, 100);
    }

    private static int CalculateEfficiencyScore(string? efficiencyRating, int psuHealthScore, int thermalHealthScore)
    {
        var baseScore = efficiencyRating switch
        {
            null => 45,
            var rating when rating.Contains("Platinum", StringComparison.OrdinalIgnoreCase) => 90,
            var rating when rating.Contains("Gold", StringComparison.OrdinalIgnoreCase) => 80,
            var rating when rating.Contains("Silver", StringComparison.OrdinalIgnoreCase) => 68,
            var rating when rating.Contains("Bronze", StringComparison.OrdinalIgnoreCase) => 58,
            _ => 50
        };

        var blended = (int)Math.Round((baseScore * 0.55m) + (psuHealthScore * 0.25m) + (thermalHealthScore * 0.20m));
        return Math.Clamp(blended, 0, 100);
    }

    private static decimal CalculatePsuHeadroomRatio(int estimatedWattage, int psuWattage)
    {
        if (estimatedWattage <= 0 || psuWattage <= 0)
        {
            return 0m;
        }

        return (psuWattage - estimatedWattage) / (decimal)psuWattage;
    }

    private static decimal GetCaseAirflowFactor(Models.Components.Case? pcCase)
    {
        if (pcCase is null)
        {
            return 0.70m;
        }

        if (pcCase.AirflowRating > 0)
        {
            var ratingFactor = Math.Clamp(pcCase.AirflowRating / 100m, 0.55m, 0.98m);
            var fansFactor = pcCase.IncludedFans <= 0 ? 0m : Math.Clamp((pcCase.IncludedFans - 1) * 0.03m, 0m, 0.10m);
            return Math.Clamp(ratingFactor + fansFactor, 0.55m, 0.98m);
        }

        var caseName = pcCase.Name;
        if (!string.IsNullOrWhiteSpace(caseName) &&
            (caseName.Contains("Airflow", StringComparison.OrdinalIgnoreCase) ||
             caseName.Contains("Mesh", StringComparison.OrdinalIgnoreCase) ||
             caseName.Contains("Flow", StringComparison.OrdinalIgnoreCase)))
        {
            return 0.92m;
        }

        if (!string.IsNullOrWhiteSpace(caseName) &&
            (caseName.Contains("H510", StringComparison.OrdinalIgnoreCase) ||
             caseName.Contains("Q58", StringComparison.OrdinalIgnoreCase)))
        {
            return 0.66m;
        }

        return 0.80m;
    }

    private static decimal GetCoolerEffectiveness(int cpuTdpWatts, Models.Components.Cooler? cooler)
    {
        if (cpuTdpWatts <= 0)
        {
            return 1m;
        }

        if (cooler is null)
        {
            return 0.55m;
        }

        var capacity = cooler.CoolingCapacityWatts <= 0 ? 0 : cooler.CoolingCapacityWatts;
        var coverageRatio = capacity == 0 ? 0.6m : capacity / Math.Max(1m, cpuTdpWatts * 1.10m);
        var normalizedCoverage = Math.Clamp(coverageRatio, 0.55m, 1.25m);

        var noiseBonus = cooler.NoiseLevelDb <= 0m ? 0m : Math.Clamp((40m - cooler.NoiseLevelDb) * 0.004m, -0.04m, 0.06m);
        return Math.Clamp(normalizedCoverage + noiseBonus, 0.50m, 1.20m);
    }

    private static int GetCpuTierScore(string? tier) => tier?.Trim().ToLowerInvariant() switch
    {
        "entry" => 1,
        "mid-range" => 2,
        "high" => 3,
        "enthusiast" => 4,
        _ => 2
    };

    private static int GetGpuTierScore(string? tier) => tier?.Trim().ToLowerInvariant() switch
    {
        "mainstream" => 2,
        "enthusiast" => 4,
        "flagship" => 5,
        _ => 2
    };

    private static void PopulateBottleneckEstimates(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Cpu is null || build.Gpu is null)
        {
            result.CpuBottleneckPercentage = 0;
            result.GpuBottleneckPercentage = 0;
            return;
        }

        var cpuScore = build.Cpu.GamingScore > 0 ? build.Cpu.GamingScore : (GetCpuTierScore(build.Cpu.PerformanceTier) * 55);
        var gpuDemand = build.Gpu.RasterScore > 0 ? build.Gpu.RasterScore : (GetGpuTierScore(build.Gpu.PerformanceTier) * 60);
        var imbalanceRatio = (cpuScore - gpuDemand) / Math.Max(1m, gpuDemand);

        if (imbalanceRatio < 0m)
        {
            result.CpuBottleneckPercentage = Math.Clamp((int)Math.Round(Math.Abs(imbalanceRatio) * 100m), 0, 45);
            result.GpuBottleneckPercentage = 0;
            return;
        }

        if (imbalanceRatio > 0m)
        {
            result.GpuBottleneckPercentage = Math.Clamp((int)Math.Round(imbalanceRatio * 70m), 0, 40);
            result.CpuBottleneckPercentage = 0;
            return;
        }

        result.CpuBottleneckPercentage = 0;
        result.GpuBottleneckPercentage = 0;
    }

    private static void PopulateRecommendations(SelectedBuild build, CompatibilityResult result)
    {
        if (build.Psu is not null && result.EstimatedSystemWattage > 0)
        {
            var psuGap = build.Psu.Wattage - result.EstimatedSystemWattage;
            if (psuGap < 0)
            {
                result.Recommendations.Add("[high] PSU is underpowered. Upgrade to a higher wattage unit.");
            }
            else if (psuGap < 120)
            {
                result.Recommendations.Add("[medium] PSU headroom is limited. Consider adding 120W+ safety margin.");
            }
        }

        if (result.EstimatedCpuTemperatureCelsius >= 84 || result.EstimatedGpuTemperatureCelsius >= 82)
        {
            result.Recommendations.Add("[high] Overheating risk detected. Improve cooling or pick a higher-airflow case.");
        }
        else if (result.EstimatedCpuTemperatureCelsius >= 74 || result.EstimatedGpuTemperatureCelsius >= 74)
        {
            result.Recommendations.Add("[medium] Thermals are warm. Add intake/exhaust fans for better stability.");
        }

        if (build.Ram is not null && build.Motherboard?.MaxRamFrequencyMhz is int maxRamMhz && build.Ram.FrequencyMhz > maxRamMhz)
        {
            result.Recommendations.Add("[medium] RAM speed exceeds board limits; tune XMP/EXPO or choose a lower-frequency kit.");
        }

        if (result.CpuBottleneckPercentage >= 12)
        {
            result.Recommendations.Add($"[medium] CPU bottleneck risk around {result.CpuBottleneckPercentage}%. Consider a stronger CPU.");
        }

        if (result.GpuBottleneckPercentage >= 12)
        {
            result.Recommendations.Add($"[medium] GPU bottleneck risk around {result.GpuBottleneckPercentage}%. Consider a stronger GPU.");
        }

        if (result.Recommendations.Count == 0)
        {
            result.Recommendations.Add("[low] Build balance looks good. Minor tuning can focus on noise and efficiency.");
        }
    }
}
