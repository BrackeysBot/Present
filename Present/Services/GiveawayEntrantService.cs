using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Present.Data;
using Present.Resources;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;

namespace Present.Services;

/// <summary>
///     Represents a service which listens for users joining giveaways, and removes them from active giveaways if they leave.
/// </summary>
internal sealed class GiveawayEntrantService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly DiscordClient _discordClient;
    private readonly ActiveGiveawayService _activeGiveawayService;
    private readonly GiveawayService _giveawayService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GiveawayEntrantService" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="activeGiveawayService">The active giveaway service.</param>
    /// <param name="giveawayService">The giveaway service.</param>
    public GiveawayEntrantService(
        IServiceScopeFactory serviceScopeFactory,
        DiscordClient discordClient,
        ActiveGiveawayService activeGiveawayService,
        GiveawayService giveawayService)
    {
        _discordClient = discordClient;
        _activeGiveawayService = activeGiveawayService;
        _giveawayService = giveawayService;
        _serviceScopeFactory = serviceScopeFactory;
    }

    /// <summary>
    ///     Adds the specified member to the specified giveaway.
    /// </summary>
    /// <param name="member">The member to add.</param>
    /// <param name="giveaway">The giveaway to which the member will be added.</param>
    /// <returns>
    ///     <see langword="true" /> if the user was successfully added to the giveaway; otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="giveaway" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///     <paramref name="member" /> is not in the same guild as the <paramref name="giveaway" />.
    /// </exception>
    public async Task<bool> AddUserToGiveawayAsync(DiscordMember member, Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(giveaway);

        if (member.Guild.Id != giveaway.GuildId)
            throw new ArgumentException(ExceptionMessages.MemberSameGuild);

        Logger.Info($"Attempting to join {member} giveaway {giveaway.Id} ({giveaway.Title})");

        if (giveaway.Entrants.Contains(member.Id))
        {
            Logger.Info($"Failed to add {member} to giveaway; they have already joined");
            return false;
        }

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        giveaway.Entrants.Add(member.Id);
        context.Update(giveaway);
        await context.SaveChangesAsync().ConfigureAwait(false);

        await _giveawayService.UpdateGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);

        Logger.Info($"{member} joined giveaway {giveaway.Id} ({giveaway.Title})");
        return true;
    }

    /// <summary>
    ///     Returns a value indicating whether the specified member is an entrant of the specified giveaway.
    /// </summary>
    /// <param name="member">The member to check.</param>
    /// <param name="giveaway">The giveaway whose entrant list to search.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> has entered <paramref name="giveaway" />; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public bool IsEntrantOf(DiscordMember member, Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(giveaway);
        return giveaway.Entrants.Contains(member.Id);
    }

    /// <summary>
    ///     Returns a read-only view of every active giveaway this member is a part of.
    /// </summary>
    /// <param name="member">The member to check.</param>
    /// <returns>A read-only view of the active giveaways that <paramref name="member" /> has entered.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public IReadOnlyList<Giveaway> GetActiveGiveaways(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var giveaways = new List<Giveaway>();

        foreach (Giveaway giveaway in _activeGiveawayService.GetActiveGiveaways(member.Guild))
        {
            if (IsEntrantOf(member, giveaway))
                giveaways.Add(giveaway);
        }

        return giveaways.AsReadOnly();
    }

    /// <summary>
    ///     Gets the entrants of this giveaway, as a list of <see cref="DiscordMember" />.
    /// </summary>
    /// <param name="giveaway">The giveaway whose entrants to return.</param>
    /// <returns>A read-only view of the entrants as a list of <see cref="DiscordMember" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public IReadOnlyList<DiscordMember> GetEntrants(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
            return ArraySegment<DiscordMember>.Empty;

        var winners = new List<DiscordMember>();

        foreach (ulong userId in giveaway.Entrants)
        {
            if (guild.Members.TryGetValue(userId, out DiscordMember? member))
                winners.Add(member);
        }

        return winners.AsReadOnly();
    }

    /// <summary>
    ///     Removes the specified member from all giveaways in the guild they are in.
    /// </summary>
    /// <param name="member">The member to remove from giveaways.</param>
    /// <exception cref="ArgumentNullException"><paramref name="member" /> is <see langword="null" />.</exception>
    public async Task RemoveFromActiveGiveawaysAsync(DiscordMember member)
    {
        ArgumentNullException.ThrowIfNull(member);

        var giveaways = new List<Giveaway>();
        var tasks = new List<Task>();

        foreach (Giveaway giveaway in GetActiveGiveaways(member))
        {
            ulong memberId = member.Id;
            if (!giveaway.Entrants.Contains(memberId)) continue;
            giveaway.Entrants.Remove(memberId);
            giveaways.Add(giveaway);
            tasks.Add(_giveawayService.UpdateGiveawayLogMessageAsync(giveaway));
        }

        if (tasks.Count > 0) await Task.WhenAll(tasks).ConfigureAwait(false);
        if (giveaways.Count == 0) return;
        Logger.Info($"Removing {member} from {"giveaway".ToQuantity(giveaways.Count)} in {member.Guild}");

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        context.UpdateRange(giveaways);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Removes the specified user from the giveaway, if they are an entrant.
    /// </summary>
    /// <param name="user">The user to remove.</param>
    /// <param name="giveaway">The giveaway whose entrants to update.</param>
    public async Task RemoveFromGiveawayAsync(DiscordUser user, Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(giveaway);

        ulong userId = user.Id;
        if (!giveaway.Entrants.Contains(userId)) return;
        giveaway.Entrants.Remove(userId);

        Logger.Info($"Removing {user} from giveaway {giveaway.Id}");
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        context.Update(giveaway);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.ComponentInteractionCreated += DiscordClientOnComponentInteractionCreated;
        _discordClient.GuildMemberRemoved += DiscordClientOnGuildMemberRemoved;
        return Task.CompletedTask;
    }

    private async Task DiscordClientOnGuildMemberRemoved(DiscordClient sender, GuildMemberRemoveEventArgs e)
    {
        DiscordMember member = e.Member;
        Logger.Info($"{member} left guild; they will be removed from all active giveaways");
        await RemoveFromActiveGiveawaysAsync(member).ConfigureAwait(false);
    }

    private async Task DiscordClientOnComponentInteractionCreated(DiscordClient sender, ComponentInteractionCreateEventArgs e)
    {
        if (e.User is not DiscordMember member) return; // interaction happened outside of guild
        if (!e.Id.StartsWith("join-ga-")) return;       // not a valid giveaway button
        if (!long.TryParse(e.Id[8..], out long giveawayId))
        {
            Logger.Warn("Component starting with prefix 'join-ga-' was not one of ours. We shouldn't receive this message!");
            return;
        }

        await HandleJoinAsync(e.Interaction, giveawayId, member).ConfigureAwait(false);
    }

    private async Task HandleJoinAsync(DiscordInteraction interaction, long giveawayId, DiscordMember member)
    {
        const InteractionResponseType responseType = InteractionResponseType.ChannelMessageWithSource;
        var builder = new DiscordInteractionResponseBuilder();
        builder.AsEphemeral();

        if (!_giveawayService.TryGetGiveaway(giveawayId, out Giveaway? giveaway))
        {
            Logger.Warn($"Component interaction by {interaction.User} for ID {giveawayId} maps to invalid giveaway");
            builder.WithContent(ResponseMessages.ErrorJoiningGiveaway);
            await interaction.CreateResponseAsync(responseType, builder).ConfigureAwait(false);
            return;
        }

        if (!_activeGiveawayService.IsGiveawayActive(giveaway) || _activeGiveawayService.HasGiveawayExpired(giveaway))
        {
            Logger.Warn($"{interaction.User} attempted to join expired giveaway {giveawayId} ({giveaway.Title}). " +
                        "This should not be possible. This is a bug!");
            builder.WithContent(ResponseMessages.JoiningEndedGiveaway);
            await interaction.CreateResponseAsync(responseType, builder).ConfigureAwait(false);
            return;
        }

        bool result = await AddUserToGiveawayAsync(member, giveaway).ConfigureAwait(false);
        builder.WithContent(result ? ResponseMessages.JoinedGiveaway : ResponseMessages.AlreadyJoinedGiveaway);
        await interaction.CreateResponseAsync(responseType, builder).ConfigureAwait(false);
    }
}
