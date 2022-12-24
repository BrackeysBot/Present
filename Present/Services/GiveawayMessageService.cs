using DSharpPlus;

namespace Present.Services;

/// <summary>
///     Represents a service which manages the posting of giveaway messages, both to public and log channels.
/// </summary>
internal sealed class GiveawayMessageService
{
    private readonly DiscordClient _discordClient;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GiveawayMessageService" /> class.
    /// </summary>
    /// <param name="discordClient">The Discord client.</param>
    public GiveawayMessageService(DiscordClient discordClient)
    {
        _discordClient = discordClient;
    }
}
