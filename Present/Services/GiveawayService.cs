using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using CSharpVitamins;
using DSharpPlus;
using DSharpPlus.Entities;
using Present.Configuration;
using Present.Data;
using Present.Resources;
using Humanizer;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NLog;
using SmartFormat;
using X10D.Collections;
using X10D.Core;
using X10D.DSharpPlus;

namespace Present.Services;

/// <summary>
///     Represents a service which manages giveaways.
/// </summary>
internal sealed class GiveawayService : BackgroundService
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ConfigurationService _configurationService;
    private readonly DiscordLogService _discordLogService;
    private readonly RoleExclusionService _roleExclusionService;
    private readonly UserExclusionService _userExclusionService;
    private readonly DiscordClient _discordClient;
    private readonly Random _random;
    private readonly ConcurrentDictionary<ShortGuid, Giveaway> _giveaways = new();

    /// <summary>
    ///     Initializes a new instance of the <see cref="GiveawayService" /> class.
    /// </summary>
    /// <param name="serviceScopeFactory">The service scope factory.</param>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="discordLogService">The Discord log service.</param>
    /// <param name="roleExclusionService">The role exclusion service.</param>
    /// <param name="userExclusionService">The user exclusion service.</param>
    /// <param name="discordClient">The Discord client.</param>
    /// <param name="random">The random number generator.</param>
    public GiveawayService(
        IServiceScopeFactory serviceScopeFactory,
        ConfigurationService configurationService,
        DiscordLogService discordLogService,
        RoleExclusionService roleExclusionService,
        UserExclusionService userExclusionService,
        DiscordClient discordClient,
        Random random
    )
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configurationService = configurationService;
        _discordLogService = discordLogService;
        _roleExclusionService = roleExclusionService;
        _userExclusionService = userExclusionService;
        _discordClient = discordClient;
        _random = random;
    }

    /// <summary>
    ///     Announces the giveaway in the channel which the giveaway is hosted.
    /// </summary>
    /// <param name="giveaway">The giveaway to announce.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task AnnounceGiveawayAsync(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (GetGiveawayChannel(giveaway) is not { } channel)
        {
            Logger.Error($"Giveaway {giveaway.Id} could not be announced; the channel was not found");
            return;
        }

        var componentId = $"join-ga-{giveaway.Id}";
        Logger.Trace("Creating giveaway embed");
        DiscordEmbed embed = CreateGiveawayPublicEmbed(giveaway);

        Logger.Trace($"Creating button component. Button component ID: {componentId}");
        var buttonEmoji = new DiscordComponentEmoji(DiscordEmoji.FromUnicode("🎉"));
        var button = new DiscordButtonComponent(ButtonStyle.Primary, componentId, "Enter Giveaway", emoji: buttonEmoji);

        var builder = new DiscordMessageBuilder();
        builder.AddEmbed(embed);
        builder.AddComponents(button);

        Logger.Info($"Announcing giveaway in {channel}");
        DiscordMessage message = await channel.SendMessageAsync(builder).ConfigureAwait(false);
        giveaway.MessageId = message.Id;
        await UpdateGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        context.Update(giveaway);
        Logger.Debug($"Updated giveaway message ID to {message.Id}");
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Creates a new giveaway using the specified parameters.
    /// </summary>
    /// <param name="options">A <see cref="GiveawayCreationOptions" /> populated with the creation options.</param>
    /// <returns>The newly-created giveaway.</returns>
    /// <exception cref="ArgumentNullException">
    ///     <see cref="GiveawayCreationOptions.Title" /> or <see cref="GiveawayCreationOptions.Description" /> or
    ///     <see cref="GiveawayCreationOptions.Channel" /> or <see cref="GiveawayCreationOptions.Creator" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <see cref="GiveawayCreationOptions.WinnerCount" /> is less than 1.
    /// </exception>
    /// <exception cref="ArgumentException"><see cref="GiveawayCreationOptions.EndTime" /> is in the past.</exception>
    public async Task<Giveaway> CreateGiveawayAsync(GiveawayCreationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options.Title);
        ArgumentNullException.ThrowIfNull(options.Description);
        ArgumentNullException.ThrowIfNull(options.Channel);
        ArgumentNullException.ThrowIfNull(options.Creator);

        if (options.WinnerCount < 1)
            throw new ArgumentOutOfRangeException(nameof(options), ExceptionMessages.WinnerCountMustBeGreaterThan0);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (options.EndTime <= now) throw new ArgumentException(ExceptionMessages.EndTimeMustBeLaterThanStartTime);

        DiscordChannel channel = options.Channel;
        DiscordGuild guild = channel.Guild;

        var giveaway = new Giveaway
        {
            Id = Guid.NewGuid(),
            StartTime = options.StartTime,
            EndTime = options.EndTime,
            CreatorId = options.Creator.Id,
            GuildId = guild.Id,
            ChannelId = channel.Id,
            Title = options.Title,
            Description = options.Description,
            ImageUri = options.ImageUri,
            WinnerCount = options.WinnerCount
        };

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        EntityEntry<Giveaway> entry = await context.AddAsync(giveaway).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);

        giveaway = entry.Entity;

        _giveaways.TryAdd(giveaway.Id, giveaway);
        Logger.Info($"{options.Creator} created giveaway {giveaway.Id} ({giveaway.Title}) in {guild}. " +
                    $"Running {giveaway.StartTime} to {giveaway.EndTime}");
        await LogGiveawayCreationAsync(giveaway).ConfigureAwait(false);
        return giveaway;
    }

    /// <summary>
    ///     Creates an embed builder, populating it with information about the specified giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway whose information to fetch.</param>
    /// <returns>A <see cref="DiscordEmbedBuilder" />, populated with information from <paramref name="giveaway" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public DiscordEmbedBuilder CreateGiveawayInformationEmbed(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
            throw new InvalidOperationException(ExceptionMessages.GuildNotFound);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration)
            ? guildConfiguration.GiveawayColor
            : DiscordColor.CornflowerBlue);

        string description = giveaway.Description;
        if (description.Length > 1024)
            description = $"{description[..1021]}...";

        var jumpLink = new Uri($"https://discord.com/channels/{giveaway.GuildId}/{giveaway.ChannelId}/{giveaway.MessageId}");
        string conjugatedEnd = giveaway.EndTime < DateTimeOffset.UtcNow ? EmbedStrings.Ended : EmbedStrings.Ends;
        var entrantsCount = giveaway.Entrants.Count.ToString("N0");
        var excludedEntrantsCount = giveaway.Entrants.CountWhereNot(e => ValidateUser(e, guild, out _)).ToString("N0");

        embed.AddField(EmbedStrings.Title, giveaway.Title);
        embed.AddField(EmbedStrings.Description, description);
        embed.AddField(EmbedStrings.Id, giveaway.Id, true);
        embed.AddField(EmbedStrings.Channel, MentionUtility.MentionChannel(giveaway.ChannelId), true);
        embed.AddField(EmbedStrings.Message, Formatter.MaskedUrl(giveaway.MessageId.ToString("N0"), jumpLink), true);
        embed.AddField(EmbedStrings.NumberOfWinners, giveaway.WinnerCount, true);
        embed.AddField(conjugatedEnd, Formatter.Timestamp(giveaway.EndTime), true);
        embed.AddField(EmbedStrings.Creator, MentionUtility.MentionUser(giveaway.CreatorId), true);
        embed.AddField(EmbedStrings.Entrants, entrantsCount, true);
        embed.AddField(EmbedStrings.ExcludedEntrants, excludedEntrantsCount, true);
        embed.AddFieldIf(giveaway.ImageUri is not null, "Image", () => Formatter.MaskedUrl("View", giveaway.ImageUri), true);

        List<ulong> winnerIds = giveaway.WinnerIds;
        if (winnerIds.Count > 0)
        {
            string winnerList = string.Join('\n', winnerIds.Select(w => $"• {MentionUtility.MentionUser(w)} ({w})"));
            embed.AddField(EmbedStrings.Winner.ToQuantity(winnerIds.Count), winnerList);
        }

        return embed;
    }

    /// <summary>
    ///     Creates the log embed for the specified giveaway, by taking the information embed and modifying it.
    /// </summary>
    /// <param name="giveaway">The giveaway whose log embed to return.</param>
    /// <returns>The log embed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public DiscordEmbed CreateGiveawayLogEmbed(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        DiscordEmbedBuilder embed = CreateGiveawayInformationEmbed(giveaway);
        embed.WithTitle(EmbedStrings.GiveawayCreated_Title_NoEmoji);
        embed.WithColor(DiscordColor.Green);
        return embed;
    }

    /// <summary>
    ///     Creates an embed, populating it with public information about the specified giveaway. This is the public embed.
    /// </summary>
    /// <param name="giveaway">The giveaway whose information to fetch.</param>
    /// <returns>A <see cref="DiscordEmbed" /> announcing <paramref name="giveaway" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">The guild defined in <paramref name="giveaway" /> was not found.</exception>
    public DiscordEmbed CreateGiveawayPublicEmbed(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
            throw new InvalidOperationException(ExceptionMessages.GuildNotFound);

        var embed = new DiscordEmbedBuilder();
        embed.WithColor(_configurationService.TryGetGuildConfiguration(guild, out GuildConfiguration? guildConfiguration)
            ? guildConfiguration.GiveawayColor
            : DiscordColor.CornflowerBlue);

        bool hasEnded = giveaway.EndTime < DateTimeOffset.UtcNow;
        string title = giveaway.Title.Titleize();
        title = (hasEnded ? EmbedStrings.GiveawayTitle_Ended : EmbedStrings.GiveawayTitle_Ongoing) + title;
        if (title.Length > 255) title = title[..252] + "...";

        embed.WithTitle(title);
        embed.WithDescription(giveaway.Description);
        if (!hasEnded) embed.AddField("\u200B", EmbedStrings.JoinGiveawayFooter);

        embed.AddField(EmbedStrings.NumberOfWinners, giveaway.WinnerCount, true);
        embed.AddField(giveaway.EndTime < DateTimeOffset.UtcNow ? EmbedStrings.Ended : EmbedStrings.Ends,
            Formatter.Timestamp(giveaway.EndTime), true);

        if (giveaway.ImageUri is { } imageUri) embed.WithImageUrl(imageUri);

        return embed;
    }

    /// <summary>
    ///     Ends an ongoing giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway to end.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task EndGiveawayAsync(Giveaway giveaway)
    {
        giveaway.EndHandled = true;
        giveaway.EndTime = DateTimeOffset.UtcNow;

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        context.Update(giveaway);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>
    ///     Attempts to retrieve the giveaway with the specified ID.
    /// </summary>
    /// <param name="id">The ID of the giveaway to retrieve.</param>
    /// <param name="giveaway">
    ///     When this method returns, contains the giveaway with the matching ID, if one was one; otherwise,
    ///     <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> if a matching giveaway was found; otherwise, <see langword="false" />.</returns>
    public bool TryGetGiveaway(ShortGuid id, [NotNullWhen(true)] out Giveaway? giveaway)
    {
        giveaway = null;
        return id != ShortGuid.Empty && _giveaways.TryGetValue(id, out giveaway);
    }

    /// <summary>
    ///     Attempts to find the <see cref="DiscordChannel" /> in which the specified giveaway is being held.
    /// </summary>
    /// <param name="giveaway">The giveaway whose channel to return.</param>
    /// <returns>
    ///     The <see cref="DiscordChannel" /> associated with the giveaway's public message, if it could be found; otherwise,
    ///     <see langword="null" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public DiscordChannel? GetGiveawayChannel(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);
        return _discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild)
            ? guild.GetChannel(giveaway.ChannelId)
            : null;
    }

    /// <summary>
    ///     Attempts to find the <see cref="DiscordMessage" /> representing the log message of this giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway whose log message to return.</param>
    /// <returns>
    ///     The <see cref="DiscordMessage" /> associated with the giveaway's log message, if it could be found; otherwise,
    ///     <see langword="null" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task<DiscordMessage?> GetGiveawayLogMessageAsync(Giveaway giveaway)
    {
        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
        {
            Logger.Trace($"Guild {giveaway.GuildId} was not found (GetGiveawayLogMessageAsync)");
            return null;
        }

        if (!_discordLogService.TryGetLogChannel(guild, out DiscordChannel? channel))
        {
            Logger.Trace("Log channel was not found (GetGiveawayLogMessageAsync)");
            return null;
        }

        try
        {
            return await channel.GetMessageAsync(giveaway.LogMessageId).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.Trace(exception, $"Failed to retrieve log message for giveaway {giveaway.Id} (GetGiveawayLogMessageAsync)");
            return null;
        }
    }

    /// <summary>
    ///     Attempts to find the <see cref="DiscordMessage" /> representing the public message of this giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway whose public message to return.</param>
    /// <returns>
    ///     The <see cref="DiscordMessage" /> associated with the giveaway, if it could be found; otherwise,
    ///     <see langword="null" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task<DiscordMessage?> GetGiveawayPublicMessageAsync(Giveaway giveaway)
    {
        if (GetGiveawayChannel(giveaway) is not { } channel)
            return null;

        try
        {
            return await channel.GetMessageAsync(giveaway.MessageId).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Logger.Trace(exception, $"Failed to retrieve public message for giveaway {giveaway.Id}");
            return null;
        }
    }

    /// <summary>
    ///     Gets the winners of this giveaway, as a list of <see cref="DiscordMember" />.
    /// </summary>
    /// <param name="giveaway">The giveaway whose winners to return.</param>
    /// <returns>A read-only view of the winners as a list of <see cref="DiscordMember" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public IReadOnlyList<DiscordMember> GetWinners(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
            return ArraySegment<DiscordMember>.Empty;

        var winners = new List<DiscordMember>();

        foreach (ulong userId in giveaway.WinnerIds)
        {
            if (guild.Members.TryGetValue(userId, out DiscordMember? member))
                winners.Add(member);
        }

        return winners.AsReadOnly();
    }

    /// <summary>
    ///     Logs the specified giveaway's creation in the specified guild's log channel.
    /// </summary>
    /// <param name="giveaway">The giveaway to log.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task LogGiveawayCreationAsync(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
        {
            Logger.Error($"Could not log giveaway creation; guild {giveaway.GuildId} was not found");
            return;
        }

        DiscordEmbedBuilder embed = CreateGiveawayInformationEmbed(giveaway);
        embed.WithTitle(EmbedStrings.GiveawayCreated_Title_NoEmoji);
        embed.WithColor(DiscordColor.Green);

        DiscordMessage? message = await _discordLogService.LogAsync(guild, embed).ConfigureAwait(false);
        if (message is not null)
        {
            giveaway.LogMessageId = message.Id;

            await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
            await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
            context.Update(giveaway);
            await context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     Logs the specified giveaway's expiration in the specified guild's log channel.
    /// </summary>
    /// <param name="giveaway">The giveaway to log.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task LogGiveawayExpirationAsync(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        DiscordGuild guild = await _discordClient.GetGuildAsync(giveaway.GuildId).ConfigureAwait(false);

        var embed = new DiscordEmbedBuilder();
        embed.WithTitle(EmbedStrings.GiveawayEnded_Title);
        embed.WithColor(DiscordColor.Green);

        string description = giveaway.Description;
        if (description.Length > 1024)
            description = $"{description[..1021]}...";

        var entrantsCount = giveaway.Entrants.Count.ToString("N0");
        var excludedEntrantsCount = giveaway.Entrants.CountWhereNot(e => ValidateUser(e, guild, out _)).ToString("N0");

        embed.AddField(EmbedStrings.Title, giveaway.Title);
        embed.AddField(EmbedStrings.Description, description);
        embed.AddField(EmbedStrings.Id, giveaway.Id, true);
        embed.AddField(EmbedStrings.NumberOfWinners, giveaway.WinnerCount, true);
        embed.AddField(EmbedStrings.Duration, (giveaway.EndTime - giveaway.StartTime).Humanize(), true);
        embed.AddField(EmbedStrings.Entrants, entrantsCount, true);
        embed.AddField(EmbedStrings.ExcludedEntrants, excludedEntrantsCount, true);

        var winners = new List<DiscordMember>();
        foreach (ulong winnerId in giveaway.WinnerIds)
        {
            DiscordMember member = await guild.GetMemberAsync(winnerId).ConfigureAwait(false);
            winners.Add(member);
        }

        if (winners.Count == 0)
        {
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle(EmbedStrings.GiveawayEnded_NoWinners_Title);
            embed.WithDescription(EmbedStrings.GiveawayEnded_NoWinners_Description.FormatSmart(new {giveaway}));
        }
        else
        {
            embed.AddField(EmbedStrings.Winner.ToQuantity(winners.Count),
                string.Join('\n', winners.Select(w => $"• {w.Mention} ({w.Id})")));

            if (winners.Count < giveaway.WinnerCount)
            {
                embed.WithColor(DiscordColor.Orange);
                embed.WithTitle(EmbedStrings.GiveawayEnded_FewWinners_Title);
                embed.WithDescription(EmbedStrings.GiveawayEnded_FewWinners_Description.FormatSmart(new
                    {giveaway, winners = "winner".ToQuantity(winners.Count)}));
            }
            else
            {
                embed.WithColor(DiscordColor.Green);
                embed.WithTitle(EmbedStrings.GiveawayEnded_Title);
                embed.WithDescription(EmbedStrings.GiveawayEnded_Description.FormatSmart(new
                    {giveaway, winners = "winner".ToQuantity(winners.Count)}));
            }
        }

        await _discordLogService.LogAsync(guild, embed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Selects the winners in the specified giveaway. The number of winners is defined by
    ///     <see cref="Giveaway.WinnerCount" />.
    /// </summary>
    /// <param name="giveaway">The giveaway whose winners to select.</param>
    /// <returns>
    ///     The selected winners of the giveaway. The returned list does not include invalid users, users which are no longer in
    ///     the guild, or users with at least one of the excluded role IDs.
    /// </returns>
    public IReadOnlyList<DiscordMember> SelectWinners(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (!_discordClient.Guilds.TryGetValue(giveaway.GuildId, out DiscordGuild? guild))
            return ArraySegment<DiscordMember>.Empty;

        var entrants = new HashSet<ulong>(giveaway.Entrants);
        var winners = new List<DiscordMember>();

        while (entrants.Count > 0 && winners.Count < giveaway.WinnerCount)
        {
            ulong randomUserId = _random.NextFrom(entrants);

            // this is critical. Do Not Remove This Line. if we don't reduce the contestant pool, we'd end up in an infinite loop
            // in the event that there are zero valid winners
            entrants.Remove(randomUserId);

            if (ValidateUser(randomUserId, guild, out DiscordMember? member))
                winners.Add(member);
        }

        return winners.AsReadOnly();
    }

    /// <summary>
    ///     Updates the winners of the specified giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway whose winners to update.</param>
    /// <param name="winners">The winners of the giveaway.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="giveaway" /> or <paramref name="winners" /> is <see langword="null" />.
    /// </exception>
    public async Task UpdateWinnersAsync(Giveaway giveaway, IEnumerable<DiscordUser> winners)
    {
        ArgumentNullException.ThrowIfNull(giveaway);
        ArgumentNullException.ThrowIfNull(winners);

        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        giveaway.WinnerIds = winners.Select(w => w.Id).ToList();
        context.Update(giveaway);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _discordClient.GuildAvailable +=
            // passing withCounts:true forces a REST request which should return us a complete member list
            async (sender, args) => await sender.GetGuildAsync(args.Guild.Id, true).ConfigureAwait(false);

        await UpdateFromDatabaseAsync(stoppingToken).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the public message of the giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway whose message to update.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task UpdateGiveawayLogMessageAsync(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (giveaway.LogMessageId == 0)
        {
            Logger.Warn($"Log message ID for giveaway {giveaway.Id} is 0");
            return;
        }

        DiscordMessage? message = await GetGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);
        if (message is null)
        {
            Logger.Warn($"Could not update log giveaway message for giveaway {giveaway.Id}");
            return;
        }

        DiscordEmbed embed = CreateGiveawayLogEmbed(giveaway);
        await message.ModifyAsync(embed).ConfigureAwait(false);
    }

    /// <summary>
    ///     Updates the public message of the giveaway.
    /// </summary>
    /// <param name="giveaway">The giveaway whose message to update.</param>
    /// <exception cref="ArgumentNullException"><paramref name="giveaway" /> is <see langword="null" />.</exception>
    public async Task UpdateGiveawayPublicMessageAsync(Giveaway giveaway)
    {
        ArgumentNullException.ThrowIfNull(giveaway);

        if (giveaway.MessageId == 0)
        {
            Logger.Warn($"Public message ID for giveaway {giveaway.Id} is 0");
            return;
        }

        DiscordMessage? message = await GetGiveawayPublicMessageAsync(giveaway).ConfigureAwait(false);
        if (message is null)
        {
            Logger.Warn($"Could not update public giveaway message for giveaway {giveaway.Id}");
            return;
        }

        DiscordEmbed embed = CreateGiveawayPublicEmbed(giveaway);
        var builder = new DiscordMessageBuilder();
        builder.ClearComponents();
        builder.Clear();
        builder.AddEmbed(embed);
        await message.ModifyAsync(builder).ConfigureAwait(false);
    }

    /// <summary>
    ///     Validates that the member is a valid participant of giveaways in the specified guild.
    /// </summary>
    /// <param name="member">The member to validate.</param>
    /// <param name="guild">The guild in which to perform the validation check.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="member" /> is a valid a participant of giveaways in
    ///     <paramref name="guild" />; otherwise, <see langword="false" />. 
    /// </returns>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="member" /> or <paramref name="guild" /> is <see langword="null" />.
    /// </exception>
    public bool ValidateMember(DiscordMember member, DiscordGuild guild)
    {
        ArgumentNullException.ThrowIfNull(member);
        ArgumentNullException.ThrowIfNull(guild);

        if (member.Guild != guild) throw new ArgumentException(ExceptionMessages.MemberSameGuild, nameof(member));

        return !_userExclusionService.IsUserExcluded(guild, member) &&
               member.Roles.All(role => !_roleExclusionService.IsRoleExcluded(guild, role));
    }

    /// <summary>
    ///     Validates that the user with the specified ID is a valid participant of giveaways in the specified guild.
    /// </summary>
    /// <param name="userId">The ID of the user to validate.</param>
    /// <param name="guild">The guild in which to perform the validation check.</param>
    /// <param name="member">
    ///     When this method returns, contains the <see cref="DiscordMember" /> with the ID <paramref name="userId" /> that is a
    ///     member of <paramref name="guild" />.
    /// </param>
    /// <returns>
    ///     <see langword="true" /> if the user with the ID <paramref name="userId" /> is a valid a participant of giveaways in
    ///     <paramref name="guild" />; otherwise, <see langword="false" />. 
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="guild" /> is <see langword="null" />.</exception>
    public bool ValidateUser(ulong userId, DiscordGuild guild, [NotNullWhen(true)] out DiscordMember? member)
    {
        ArgumentNullException.ThrowIfNull(guild);
        member = null;
        return userId != 0 && guild.Members.TryGetValue(userId, out member) && ValidateMember(member, guild);
    }

    private async Task UpdateFromDatabaseAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<GiveawayContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

        await foreach (Giveaway giveaway in context.Giveaways)
            _giveaways.TryAdd(giveaway.Id, giveaway);

        Logger.Info($"Loaded {_giveaways.Count} total giveaways from the database");
    }
}
