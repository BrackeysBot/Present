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
///     Represents a service which handles user exclusions in giveaways.
/// </summary>
internal sealed class UserExclusionService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly DiscordLogService _discordLogService;
    private readonly ConcurrentDictionary<DiscordGuild, List<ExcludedUser>> _excludedUsers = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="UserExclusionService" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="discordLogService">The Discord log service.</param>
    public UserExclusionService(
        IServiceScopeFactory serviceScopeFactory,
        DiscordClient discordClient,
        DiscordLogService discordLogService
    )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _discordClient = discordClient;
        _discordLogService = discordLogService;
    }

    /// <summary>
    ///     Excludes a user from being able to win giveaways.
    /// </summary>
    /// <param name="staffMember">The staff member responsible for the exclusion.</param>
    /// <param name="user">The user to exclude.</param>
    /// <param name="reason">The reason for the exclusion.</param>
    /// <returns>The excluded user.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="staffMember" /> or <paramref name="user" /> is <see langword="null" />.
    /// </exception>
    public async Task<ExcludedUser> ExcludeUserAsync(DiscordMember staffMember, DiscordUser user, string? reason)
    {
        ArgumentNullException.ThrowIfNull(staffMember);
        ArgumentNullException.ThrowIfNull(user);

        reason = reason?.AsNullIfWhiteSpace();

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        EntityEntry<ExcludedUser> entry = context.Add(new ExcludedUser
        {
            GuildId = staffMember.Guild.Id,
            UserId = user.Id,
            Reason = reason,
            StaffMemberId = staffMember.Id
        });
        await context.SaveChangesAsync().ConfigureAwait(false);

        if (!_excludedUsers.TryGetValue(staffMember.Guild, out List<ExcludedUser>? excludedUsers))
        {
            excludedUsers = new List<ExcludedUser>();
            _excludedUsers[staffMember.Guild] = excludedUsers;
        }

        ExcludedUser excludedUser = entry.Entity;
        excludedUsers.Add(excludedUser);

        Logger.Info($"{user} was excluded by {staffMember} in {staffMember.Guild}. Reason: {reason ?? "<none>"}");

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle("User exclusion added");
        embed.AddField("User", $"{user.Mention} ({user.Id})", true);
        embed.AddField(EmbedStrings.StaffMember, staffMember.Mention, true);
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason), EmbedStrings.Reason, reason);
        embed.WithColor(DiscordColor.Orange);
        await _discordLogService.LogAsync(staffMember.Guild, embed).ConfigureAwait(false);

        return excludedUser;
    }

    /// <summary>
    ///     Gets a read-only view of the user IDs that are excluded.
    /// </summary>
    /// <param name="guild">The guild whose excluded user IDs to return.</param>
    /// <returns>A <see cref="IReadOnlyList{T}" /> of <see cref="ulong" /> representing the excluded user IDs.</returns>
    public IReadOnlyList<ulong> GetExcludedUserIds(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);

        return _excludedUsers.TryGetValue(guild, out List<ExcludedUser>? excludedUsers)
            ? excludedUsers.Select(u => u.UserId).ToArray()
            : ArraySegment<ulong>.Empty;
    }

    /// <summary>
    ///     Includes a user, so that holders are able to win giveaways.
    /// </summary>
    /// <param name="staffMember">The staff member responsible for the inclusion.</param>
    /// <param name="user">The user to include.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="staffMember" /> or <paramref name="user" /> is <see langword="null" />.
    /// </exception>
    public async Task IncludeUserAsync(DiscordMember staffMember, DiscordUser user)
    {
        ArgumentNullException.ThrowIfNull(staffMember);
        ArgumentNullException.ThrowIfNull(user);

        if (!_excludedUsers.TryGetValue(staffMember.Guild, out List<ExcludedUser>? excludedUsers))
            return;

        ExcludedUser? excludedUser = excludedUsers.Find(r => r.UserId == user.Id);
        if (excludedUser is null)
            return;

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        context.Remove(excludedUser);
        await context.SaveChangesAsync().ConfigureAwait(false);

        Logger.Info($"The exclusion on {user} was removed by {staffMember} in {staffMember.Guild}");

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle(EmbedStrings.ExclusionRemoved);
        embed.AddField(EmbedStrings.User, $"{user.Mention} ({user.Id})", true);
        embed.AddField(EmbedStrings.StaffMember, staffMember.Mention, true);
        embed.WithColor(DiscordColor.Green);
        await _discordLogService.LogAsync(staffMember.Guild, embed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Returns a value indicating whether or not the specified user is excluded from winning giveaways.
    /// </summary>
    /// <param name="guild">The guild in which to check.</param>
    /// <param name="user">The user to check.</param>
    /// <returns>
    ///     <see langword="true" /> if the user is excluded from winning giveaways; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="guild" /> or <paramref name="user" /> is <see langword="null" />.
    /// </exception>
    public bool IsUserExcluded(DiscordGuild guild, DiscordUser user)
    {
        ArgumentNullException.ThrowIfNull(guild);
        ArgumentNullException.ThrowIfNull(user);

        if (!_excludedUsers.TryGetValue(guild, out List<ExcludedUser>? excludedUsers))
            return false;

        return excludedUsers.Exists(u => u.UserId == user.Id);
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

        if (!_excludedUsers.TryGetValue(guild, out List<ExcludedUser>? excludedUsers))
        {
            excludedUsers = new List<ExcludedUser>();
            _excludedUsers[guild] = excludedUsers;
        }

        lock (excludedUsers)
            excludedUsers.Clear();

        foreach (ExcludedUser excludedUser in context.ExcludedUsers.Where(u => u.GuildId == guild.Id))
        {
            try
            {
                DiscordUser? user = await _discordClient.GetUserAsync(excludedUser.UserId).ConfigureAwait(false);
                if (user is null)
                {
                    Logger.Warn($"Excluded user {excludedUser.UserId} was null (exclusion in {guild}); this is a bug!");
                    continue;
                }
            }
            catch
            {
                Logger.Warn($"Excluded user {excludedUser.UserId} not found (exclusion in {guild})");
                continue;
            }

            lock (excludedUsers)
                excludedUsers.Add(excludedUser);
        }

        Logger.Info($"Loaded {"excluded user".ToQuantity(excludedUsers.Count)} ");
    }

    private async Task DiscordClientOnGuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
    {
        Logger.Debug($"{e.Guild} available; fetching excluded users from database");
        await UpdateFromDatabaseAsync(e.Guild).ConfigureAwait(false);
    }
}
