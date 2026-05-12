using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PcBuilder.Web.Models;
using PcBuilder.Web.Models.Admin;
using PcBuilder.Web.Models.Components;
using PcBuilder.Web.Models.DTOs;
using PcBuilder.Web.Models.Auth;
using PcBuilder.Web.Repositories;

namespace PcBuilder.Web.Services;

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISavedBuildRepository _savedBuildRepository;
    private readonly IComponentRepository _componentRepository;
    private readonly ICompatibilityService _compatibilityService;

    public AdminDashboardService(
        UserManager<ApplicationUser> userManager,
        ISavedBuildRepository savedBuildRepository,
        IComponentRepository componentRepository,
        ICompatibilityService compatibilityService)
    {
        _userManager = userManager;
        _savedBuildRepository = savedBuildRepository;
        _componentRepository = componentRepository;
        _compatibilityService = compatibilityService;
    }

    public async Task<AdminDashboardViewModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var totalUsers = await _userManager.Users.CountAsync(cancellationToken);
        var builds = (await _savedBuildRepository.GetAllForAdminAsync(cancellationToken)).ToList();
        var components = (await _componentRepository.GetAllAsync(cancellationToken)).ToList();
        var byId = components.ToDictionary(c => c.Id);

        var popular = ComputePopularComponents(builds, byId);
        var issues = ComputeCommonIssues(builds, components);

        return new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            TotalSavedBuilds = builds.Count,
            PopularComponents = popular,
            CommonCompatibilityIssues = issues
        };
    }

    private static IReadOnlyList<PopularComponentRow> ComputePopularComponents(
        List<SavedBuild> builds,
        IReadOnlyDictionary<int, Component> byId)
    {
        var tallies = new Dictionary<string, (string Slot, Component Component, int Count)>(StringComparer.Ordinal);

        void Bump(int? id, string slot)
        {
            if (id is null || !byId.TryGetValue(id.Value, out var c))
            {
                return;
            }

            var key = $"{slot}:{c.Id}";
            if (tallies.TryGetValue(key, out var row))
            {
                tallies[key] = (row.Slot, row.Component, row.Count + 1);
            }
            else
            {
                tallies[key] = (slot, c, 1);
            }
        }

        foreach (var build in builds)
        {
            Bump(build.CpuId, "CPU");
            Bump(build.MotherboardId, "Motherboard");
            Bump(build.RamId, "RAM");
            Bump(build.GpuId, "GPU");
            Bump(build.PsuId, "PSU");
            Bump(build.CaseId, "Case");
            Bump(build.CoolerId, "Cooler");
        }

        return tallies.Values
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Slot)
            .Take(12)
            .Select(x => new PopularComponentRow
            {
                Slot = x.Slot,
                Name = x.Component.Name,
                Manufacturer = x.Component.Manufacturer,
                ComponentId = x.Component.Id,
                UsageCount = x.Count
            })
            .ToList();
    }

    private IReadOnlyList<CompatibilityIssueRow> ComputeCommonIssues(
        List<SavedBuild> builds,
        List<Component> components)
    {
        var errorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var warningCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var build in builds)
        {
            var selected = new SelectedBuild
            {
                CpuId = build.CpuId,
                MotherboardId = build.MotherboardId,
                RamId = build.RamId,
                GpuId = build.GpuId,
                PsuId = build.PsuId,
                CaseId = build.CaseId,
                CoolerId = build.CoolerId
            };

            HydrateSelection(selected, components);
            var result = _compatibilityService.Check(selected);

            foreach (var message in result.Errors)
            {
                errorCounts[message] = errorCounts.GetValueOrDefault(message) + 1;
            }

            foreach (var message in result.Warnings)
            {
                warningCounts[message] = warningCounts.GetValueOrDefault(message) + 1;
            }
        }

        var merged = errorCounts.Select(kv => new CompatibilityIssueRow
            {
                Message = kv.Key,
                Count = kv.Value,
                IsWarning = false
            })
            .Concat(warningCounts.Select(kv => new CompatibilityIssueRow
            {
                Message = kv.Key,
                Count = kv.Value,
                IsWarning = true
            }))
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Message)
            .Take(20)
            .ToList();

        return merged;
    }

    private static void HydrateSelection(SelectedBuild build, IReadOnlyList<Component> components)
    {
        build.Cpu = FindById<Cpu>(components, build.CpuId);
        build.Motherboard = FindById<Motherboard>(components, build.MotherboardId);
        build.Ram = FindById<Ram>(components, build.RamId);
        build.Gpu = FindById<Gpu>(components, build.GpuId);
        build.Psu = FindById<Psu>(components, build.PsuId);
        build.Case = FindById<Case>(components, build.CaseId);
        build.Cooler = FindById<Cooler>(components, build.CoolerId);
    }

    private static TComponent? FindById<TComponent>(IReadOnlyList<Component> components, int? id)
        where TComponent : Component
    {
        if (id is null)
        {
            return null;
        }

        return components.OfType<TComponent>().FirstOrDefault(c => c.Id == id.Value);
    }
}
