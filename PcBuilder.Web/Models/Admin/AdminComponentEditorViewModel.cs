using System.ComponentModel.DataAnnotations;

namespace PcBuilder.Web.Models.Admin;

public sealed class AdminComponentEditorViewModel
{
    [Required]
    [Display(Name = "Component type")]
    public string ComponentType { get; set; } = "cpu";

    [Display(Name = "Id")]
    public int Id { get; set; }

    [Required]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [Display(Name = "Price (USD)")]
    public decimal Price { get; set; }

    [Display(Name = "Image URL")]
    public string ImageUrl { get; set; } = string.Empty;

    public string Socket { get; set; } = string.Empty;

    [Display(Name = "TDP (W)")]
    public int TdpWatts { get; set; }

    public int Cores { get; set; }

    public int Threads { get; set; }

    [Display(Name = "Base clock (GHz)")]
    public decimal BaseClockGhz { get; set; }

    public string? Generation { get; set; }

    [Display(Name = "Performance tier")]
    public string? PerformanceTier { get; set; }

    [Display(Name = "Gaming score")]
    public int GamingScore { get; set; }

    [Display(Name = "Productivity score")]
    public int ProductivityScore { get; set; }

    [Display(Name = "Supported RAM types (comma-separated)")]
    public string SupportedRamTypesCsv { get; set; } = string.Empty;

    public string Chipset { get; set; } = string.Empty;

    [Display(Name = "Form factor")]
    public string? FormFactor { get; set; }

    [Display(Name = "Supported CPU generations (comma-separated)")]
    public string SupportedCpuGenerationsCsv { get; set; } = string.Empty;

    [Display(Name = "Max RAM MHz")]
    public int? MaxRamFrequencyMhz { get; set; }

    [Display(Name = "RAM type (DDR4/DDR5)")]
    public string RamType { get; set; } = string.Empty;

    [Display(Name = "Capacity (GB)")]
    public int CapacityGb { get; set; }

    [Display(Name = "Frequency (MHz)")]
    public int FrequencyMhz { get; set; }

    [Display(Name = "VRAM (GB)")]
    public int VramGb { get; set; }

    [Display(Name = "Recommended PSU (W)")]
    public int RecommendedPsuWattage { get; set; }

    [Display(Name = "Length (mm)")]
    public int? LengthMm { get; set; }

    [Display(Name = "Raster score")]
    public int RasterScore { get; set; }

    [Display(Name = "Ray tracing score")]
    public int RayTracingScore { get; set; }

    public int Wattage { get; set; }

    [Display(Name = "Efficiency rating")]
    public string EfficiencyRating { get; set; } = string.Empty;

    [Display(Name = "Supported motherboard sizes (comma-separated)")]
    public string SupportedMotherboardSizesCsv { get; set; } = string.Empty;

    [Display(Name = "Max GPU length (mm)")]
    public int? MaxGpuLengthMm { get; set; }

    [Display(Name = "Included fans")]
    public int IncludedFans { get; set; }

    [Display(Name = "Airflow rating (0–100)")]
    public int AirflowRating { get; set; }

    [Display(Name = "Supported sockets (comma-separated)")]
    public string SupportedSocketsCsv { get; set; } = string.Empty;

    [Display(Name = "Cooling capacity (W)")]
    public int CoolingCapacityWatts { get; set; }

    [Display(Name = "Noise (dB)")]
    public decimal NoiseLevelDb { get; set; }
}
