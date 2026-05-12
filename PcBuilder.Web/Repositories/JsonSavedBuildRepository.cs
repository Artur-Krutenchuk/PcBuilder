using System.Text.Json;
using PcBuilder.Web.Models;

namespace PcBuilder.Web.Repositories;

public sealed class JsonSavedBuildRepository : ISavedBuildRepository
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

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

    public async Task<IReadOnlyList<SavedBuild>> GetAllAsync(string userId, CancellationToken cancellationToken = default)
    {
        await FileLock.WaitAsync(cancellationToken);
        try
        {
            var builds = await ReadAllBuildsCoreAsync(cancellationToken);
            return builds.Where(build => string.Equals(build.UserId, userId, StringComparison.Ordinal)).ToList();
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<IReadOnlyList<SavedBuild>> GetPublicAsync(CancellationToken cancellationToken = default)
    {
        await FileLock.WaitAsync(cancellationToken);
        try
        {
            var builds = await ReadAllBuildsCoreAsync(cancellationToken);
            return builds.Where(build => build.IsPublic).OrderByDescending(build => build.CreatedAtUtc).ToList();
        }
        finally
        {
            FileLock.Release();
        }
    }

        await using var stream = File.OpenRead(filePath);
        var builds = await JsonSerializer.DeserializeAsync<List<SavedBuild>>(stream, cancellationToken: cancellationToken);
        return builds ?? [];
    }

    public async Task SaveAsync(SavedBuild build, CancellationToken cancellationToken = default)
    {
        await FileLock.WaitAsync(cancellationToken);
        try
        {
            var filePath = GetFilePath();
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

        var builds = (await GetAllAsync(cancellationToken)).ToList();
        builds.Insert(0, build);

    private async Task WriteAllBuildsAtomicAsync(string filePath, List<SavedBuild> builds, CancellationToken cancellationToken)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"savedbuilds-{Guid.NewGuid():N}.tmp.json");
        await using (var stream = File.Create(tempPath))
        {
            await JsonSerializer.SerializeAsync(stream, builds, SerializerOptions, cancellationToken);
        }

        try
        {
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    private async Task<List<SavedBuild>> ReadAllBuildsAsync(CancellationToken cancellationToken)
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

    private string GetFilePath()
    {
        return Path.Combine(_environment.ContentRootPath, "Data", "savedbuilds.json");
    }
}
