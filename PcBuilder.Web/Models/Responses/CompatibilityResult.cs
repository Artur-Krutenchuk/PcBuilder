namespace PcBuilder.Web.Models.Responses;

public sealed class CompatibilityResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; init; } = [];

    public List<string> Warnings { get; init; } = [];

    public int GamingScore { get; set; }
    public int WorkstationScore { get; set; }
    public int EfficiencyScore { get; set; }
}
