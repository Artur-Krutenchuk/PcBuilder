using PcBuilder.Web.Models.Components;

namespace PcBuilder.Web.Models.DTOs;

public sealed class SelectedBuild
{

    public int? CpuId { get; set; }
    public int? MotherboardId { get; set; }
    public int? RamId { get; set; }
    public int? GpuId { get; set; }
    public int? PsuId { get; set; }
    public int? CaseId { get; set; }
    public int? CoolerId { get; set; }

    public Cpu? Cpu { get; set; }
    public Motherboard? Motherboard { get; set; }
    public Ram? Ram { get; set; }
    public Gpu? Gpu { get; set; }
    public Psu? Psu { get; set; }
    public Case? Case { get; set; }
    public Cooler? Cooler { get; set; }
}
