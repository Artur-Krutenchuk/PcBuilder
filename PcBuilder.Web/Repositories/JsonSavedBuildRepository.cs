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
            var builds = await ReadAllBuildsAsync(cancellationToken);
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
            var builds = await ReadAllBuildsAsync(cancellationToken);
            return builds.Where(build => build.IsPublic).OrderByDescending(build => build.CreatedAtUtc).ToList();
        }
        finally
        {
            FileLock.Release();
        }
    }

    public async Task<IReadOnlyList<SavedBuild>> GetAllForAdminAsync(CancellationToken cancellationToken = default)
    {
        await FileLock.WaitAsync(cancellationToken);
        try
        {
            return await ReadAllBuildsAsync(cancellationToken);
        }
        finally
        {
            FileLock.Release();
        }
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

            var builds = await ReadAllBuildsAsync(cancellationToken);
            var buildsList = builds.ToList();
            buildsList.Insert(0, build);
            await WriteAllBuildsAtomicAsync(filePath, buildsList, cancellationToken);
            _logger.LogInformation("Saved build {BuildId} for user {UserId}", build.Id, build.UserId);
        }
        finally
        {
            FileLock.Release();
        }
    }

    private async Task WriteAllBuildsAtomicAsync(string filePath, List<SavedBuild> builds, CancellationToken cancellationToken)
    {
        var appTempPath = Path.Combine(_environment.ContentRootPath, "Data", ".tmp");
        if (!Directory.Exists(appTempPath))
        {
            Directory.CreateDirectory(appTempPath);
        }

        var tempPath = Path.Combine(appTempPath, $"savedbuilds-{Guid.NewGuid():N}.tmp.json");
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
                try
                {
                    File.Delete(tempPath);
                }
                catch
                {
                    _logger.LogWarning("Failed to clean up temporary file: {Path}", tempPath);
                }
            }

            throw;
        }
    }

    private async Task<List<SavedBuild>> ReadAllBuildsAsync(CancellationToken cancellationToken)
    {
        var filePath = GetFilePath();
        if (!File.Exists(filePath))
        {
            _logger.LogInformation("SavedBuilds file does not exist: {Path}", filePath);
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            var builds = await JsonSerializer.DeserializeAsync<List<SavedBuild>>(stream, cancellationToken: cancellationToken);
            _logger.LogDebug("Read {Count} saved builds from {Path}", builds?.Count ?? 0, filePath);
            return builds ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize builds from {Path}", filePath);
            return [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error reading builds from {Path}", filePath);
            return [];
        }
    }

    private string GetFilePath()
    {
        return Path.Combine(_environment.ContentRootPath, "Data", "savedbuilds.json");
    }
}
