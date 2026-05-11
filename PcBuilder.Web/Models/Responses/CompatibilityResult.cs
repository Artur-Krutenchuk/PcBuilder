namespace PcBuilder.Web.Models.Responses;

public sealed class CompatibilityResult
{
    public bool IsValid => Errors.Count == 0;

    public List<string> Errors { get; init; } = [];

    public List<string> Warnings { get; init; } = [];
}
