using System.Text.Json;
using PcBuilder.Web.Models;

namespace PcBuilder.Web.Repositories;

public sealed class JsonSavedBuildRepository : ISavedBuildRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<JsonSavedBuildRepository> _logger;

    public JsonSavedBuildRepository(IWebHostEnvironment environment, ILogger<JsonSavedBuildRepository> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SavedBuild>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            return [];
        }

        await using var stream = File.OpenRead(filePath);
        var builds = await JsonSerializer.DeserializeAsync<List<SavedBuild>>(stream, cancellationToken: cancellationToken);
        return builds ?? [];
    }

    public async Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        var filePath = GetFilePath();
        var directoryPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        var builds = (await GetAllAsync(cancellationToken)).ToList();
        builds.Insert(0, build);

        await using var stream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(stream, builds, SerializerOptions, cancellationToken);
        _logger.LogInformation("Saved build {BuildId} at {CreatedAtUtc}.", build.Id, build.CreatedAtUtc);
    }

    private string GetFilePath()
    {
        return Path.Combine(_environment.ContentRootPath, "Data", "savedbuilds.json");
    }
}
