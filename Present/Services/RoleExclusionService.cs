using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Present.Data;
using Present.Resources;
using Humanizer;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using X10D.DSharpPlus;
using X10D.Text;

namespace Present.Services;

/// <summary>
///     Represents a service which handles role exclusions in giveaways.
/// </summary>
internal sealed class RoleExclusionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DiscordLogService _discordLogService;
    private readonly DiscordClient _discordClient;
    private readonly ConcurrentDictionary<DiscordGuild, List<ExcludedRole>> _excludedRoles = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="RoleExclusionService" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <param name="discordLogService">The Discord log service.</param>
    /// <param name="discordClient">The Discord client.</param>
    public RoleExclusionService(
        IServiceScopeFactory serviceScopeFactory,
        DiscordLogService discordLogService,
        DiscordClient discordClient
    )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _discordLogService = discordLogService;
        _discordClient = discordClient;
    }

    /// <summary>
    ///     Excludes a role from being able to win giveaways.
    /// </summary>
    /// <param name="staffMember">The staff member responsible for the exclusion.</param>
    /// <param name="role">The role to exclude.</param>
    /// <param name="reason">The reason for the exclusion.</param>
    /// <returns>The excluded role.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="staffMember" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public async Task<ExcludedRole> ExcludeRoleAsync(DiscordMember staffMember, DiscordRole role, string? reason)
    {
        ArgumentNullException.ThrowIfNull(staffMember);
        ArgumentNullException.ThrowIfNull(role);

        reason = reason?.AsNullIfWhiteSpace();

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        EntityEntry<ExcludedRole> entry = context.Add(new ExcludedRole
        {
            GuildId = staffMember.Guild.Id,
            RoleId = role.Id,
            Reason = reason?.AsNullIfWhiteSpace(),
            StaffMemberId = staffMember.Id
        });
        await context.SaveChangesAsync().ConfigureAwait(false);

        if (!_excludedRoles.TryGetValue(staffMember.Guild, out List<ExcludedRole>? excludedRoles))
        {
            excludedRoles = new List<ExcludedRole>();
            _excludedRoles[staffMember.Guild] = excludedRoles;
        }

        ExcludedRole excludedRole = entry.Entity;
        excludedRoles.Add(excludedRole);

        Logger.Info($"{role} was excluded by {staffMember} in {staffMember.Guild}. Reason: {reason ?? "<none>"}");

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle(EmbedStrings.RoleExclusionAdded);
        embed.AddField(EmbedStrings.Role, $"{role.Mention} ({role.Id})", true);
        embed.AddField(EmbedStrings.StaffMember, staffMember.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason), EmbedStrings.Reason, reason);
        embed.WithColor(DiscordColor.Orange);
        await _discordLogService.LogAsync(staffMember.Guild, embed).ConfigureAwait(false);

        return excludedRole;
    }

    /// <summary>
    ///     Gets a read-only view of the role IDs that are excluded.
    /// </summary>
    /// <param name="guild">The guild whose excluded role IDs to return.</param>
    /// <returns>A <see cref="IReadOnlyList{T}" /> of <see cref="ulong" /> representing the excluded role IDs.</returns>
    public IReadOnlyList<ulong> GetExcludedUserIds(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        return _excludedRoles.TryGetValue(guild, out List<ExcludedRole>? excludedRoles)
            ? excludedRoles.Select(u => u.RoleId).ToArray()
            : ArraySegment<ulong>.Empty;
    }

    /// <summary>
    ///     Includes a role, so that holders are able to win giveaways.
    /// </summary>
    /// <param name="staffMember">The staff member responsible for the inclusion.</param>
    /// <param name="role">The role to include.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="staffMember" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public async Task IncludeRoleAsync(DiscordMember staffMember, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(staffMember);
        ArgumentNullException.ThrowIfNull(role);

        if (!_excludedRoles.TryGetValue(staffMember.Guild, out List<ExcludedRole>? excludedRoles))
            return;

        ExcludedRole? excludedRole = excludedRoles.Find(r => r.RoleId == role.Id);
        if (excludedRole is null)
            return;

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        context.Remove(excludedRole);
        await context.SaveChangesAsync().ConfigureAwait(false);

        Logger.Info($"The exclusion on {role} was removed by {staffMember} in {staffMember.Guild}");

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("Role exclusion removed");
        embed.AddField(EmbedStrings.Role, $"{role.Mention} ({role.Id})", true);
        embed.AddField(EmbedStrings.StaffMember, staffMember.Mention, true);
        embed.WithColor(DiscordColor.Green);
        await _discordLogService.LogAsync(staffMember.Guild, embed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns a value indicating whether or not the specified role is excluded from winning giveaways.
    /// </summary>
    /// <param name="guild">The guild in which to check.</param>
    /// <param name="role">The role to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the user is excluded from winning giveaways; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="role" /> is <see langword="null" />.
    /// </exception>
    public bool IsRoleExcluded(DiscordGuild guild, DiscordRole role)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(role);

        return _excludedRoles.TryGetValue(guild, out List<ExcludedRole>? excludedRoles) &&
               excludedRoles.Exists(u => u.RoleId == role.Id);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable += DiscordClientOnGuildAvailable;
        return Task.CompletedTask;
    }

    private async Task UpdateFromDatabaseAsync(DiscordGuild guild)
    {
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();

        if (!_excludedRoles.TryGetValue(guild, out List<ExcludedRole>? excludedRoles))
        {
            excludedRoles = new List<ExcludedRole>();
            _excludedRoles[guild] = excludedRoles;
        }

        lock (excludedRoles)
            excludedRoles.Clear();

        foreach (ExcludedRole excludedRole in context.ExcludedRoles.Where(u => u.GuildId == guild.Id))
        {
            if (guild.GetRole(excludedRole.RoleId) is null)
            {
                Logger.Warn($"Excluded role {excludedRole.RoleId} not found (exclusion in {guild})");
                continue;
            }

            lock (excludedRoles)
                excludedRoles.Add(excludedRole);
        }

        Logger.Info($"Loaded {"excluded role".ToQuantity(excludedRoles.Count)} ");
    }

    private async Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Logger.Debug($"{e.Guild} available; fetching excluded roles from database");
        await UpdateFromDatabaseAsync(e.Guild).ConfigureAwait(false);
    }
}
