using System.Collections.Concurrent;
using System.Timers;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Present.Data;
using Humanizer;
using Microsoft.Extensions.Hosting;
using NLog;
using Timer = System.Timers.Timer;

namespace Present.Services;

/// <summary>
///     Represents a service which manages active giveaways.
/// </summary>
internal sealed class ActiveGiveawayService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly GiveawayService _giveawayService;
    private readonly ConcurrentDictionary<ulong, List<Giveaway>> _activeGiveaways = new();
    private readonly Timer _giveawayTimer = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActiveGiveawayService" /> class.
    /// </summary>
    /// <param name="giveawayService">The giveaway service.</param>
    public ActiveGiveawayService(GiveawayService giveawayService)
    {
        _giveawayService = giveawayService;

        _giveawayTimer.Interval = 1000;
        _giveawayTimer.Elapsed += GiveawayTimerOnElapsed;
    }

    /// <summary>
    ///     Adds an active giveaway to track.
    /// </summary>
    /// <param name="giveaway">The giveaway to track.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public void AddActiveGiveaway(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (giveaway.EndHandled || HasGiveawayExpired(giveaway)) return;
        if (!_activeGiveaways.TryGetValue(giveaway.GuildId, out List<Giveaway>? activeGiveaways))
        {
            Logger.Debug($"Guild {giveaway.GuildId} ({giveaway.Title}) is not tracked. Adding new cache");
            activeGiveaways = new List<Giveaway>();
            _activeGiveaways[giveaway.GuildId] = activeGiveaways;
        }

        Logger.Debug($"Tracking giveaway {giveaway.Id} ({giveaway.Title}) in guild {giveaway.GuildId}");
        activeGiveaways.Add(giveaway);
    }

    /// <summary>
    ///     Returns a list of the active giveaways in the specified guild.
    /// </summary>
    /// <param name="guild">The guild whose active giveaways to return.</param>
    /// <returns>A <see cref="IReadOnlyList{T}" /> of <see cref="Giveaway" /> instances that are considered active.</returns>
    public IReadOnlyList<Giveaway> GetActiveGiveaways(DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(guild);
        return _activeGiveaways.TryGetValue(guild.Id, out List<Giveaway>? activeGiveaways)
            ? activeGiveaways.AsReadOnly()
            : ArraySegment<Giveaway>.Empty;
    }

    /// <summary>
    ///     Returns a value indicating whether the specified giveaway has expired.
    /// </summary>
    /// <param name="giveaway">The giveaway to check.</param>
    /// <returns><see langword="true" /> if the giveaway has expired; otherwise, <see langword="false" />.</returns>
    public bool HasGiveawayExpired(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);
        return giveaway.EndTime <= DateTimeOffset.UtcNow;
    }

    /// <summary>
    ///     Returns a value indicating whether the specified giveaway is active.
    /// </summary>
    /// <param name="giveaway">The giveaway to check.</param>
    /// <returns><see langword="true" /> if the giveaway is active; otherwise, <see langword="false" />.</returns>
    public bool IsGiveawayActive(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        return !giveaway.EndHandled &&
               _activeGiveaways.TryGetValue(giveaway.GuildId, out List<Giveaway>? activeGiveaways) &&
               activeGiveaways.Contains(giveaway);
    }

    /// <summary>
    ///     Removes a giveaway from track.
    /// </summary>
    /// <param name="giveaway">The giveaway to stop tracking.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public void RemoveActiveGiveaway(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_activeGiveaways.TryGetValue(giveaway.GuildId, out List<Giveaway>? activeGiveaways))
        {
            Logger.Warn($"Guild {giveaway.GuildId} ({giveaway.Title}) for giveaway {giveaway.Id} is not tracked!");
            return;
        }

        Logger.Debug($"Removing tracking for giveaway {giveaway.Id} in guild {giveaway.GuildId}");
        activeGiveaways.Remove(giveaway);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.Debug($"Starting timer with interval {_giveawayTimer.Interval}");
        _giveawayTimer.Start();

        UpdateFromDatabase();
        return Task.CompletedTask;
    }

    private async void GiveawayTimerOnElapsed(object? sender, ElapsedEventArgs e)
    {
        await CheckGiveawaysAsync().ConfigureAwait(false);
    }

    private async Task CheckGiveawaysAsync()
    {
        foreach (ulong guildId in _activeGiveaways.Keys)
        {
            List<Giveaway> giveaways = _activeGiveaways[guildId];
            for (int index = giveaways.Count - 1; index >= 0; index--)
            {
                Giveaway giveaway = giveaways[index];
                if (!IsGiveawayActive(giveaway) || !HasGiveawayExpired(giveaway)) continue;

                Logger.Info($"Giveaway {giveaway.Id} ({giveaway.Title}) has ended");
                await HandleExpiredGiveawayAsync(giveaway).ConfigureAwait(false);
            }
        }
    }

    private async Task HandleExpiredGiveawayAsync(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);
        giveaway.EndHandled = true;
        RemoveActiveGiveaway(giveaway);

        IReadOnlyList<DiscordMember> winners = await _giveawayService.SelectWinnersAsync(giveaway).ConfigureAwait(false);
        Logger.Info($"Selected {"winner".ToQuantity(winners.Count)} for giveaway {giveaway.Id} ({giveaway.Title})");
        foreach (DiscordMember winner in winners)
            Logger.Info(winner);

        await _giveawayService.UpdateWinnersAsync(giveaway, winners).ConfigureAwait(false);
        await _giveawayService.LogGiveawayExpirationAsync(giveaway).ConfigureAwait(false);

        try
        {
            await _giveawayService.UpdateGiveawayPublicMessageAsync(giveaway).ConfigureAwait(false);
            await _giveawayService.UpdateGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);
        }
        catch (NotFoundException exception)
        {
            Logger.Error(exception, $"Could not update giveaway message in guild {giveaway.GuildId} for giveaway {giveaway.Id}");
        }
    }

    private void UpdateFromDatabase()
    {
        Logger.Info("Loading active giveaways from the database...");

        foreach (Giveaway giveaway in _giveawayService.Giveaways)
            AddActiveGiveaway(giveaway);

        Logger.Info($"Loaded {"active giveaway".ToQuantity(_activeGiveaways.Count)} from the database");
    }
}
