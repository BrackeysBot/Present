namespace Present.Configuration;

/// <summary>
///     Represents a guild configuration.
/// </summary>
internal sealed class GuildConfiguration
{
    /// <summary>
    ///     Gets or sets the ID of the log channel.
    /// </summary>
    /// <value>The ID of the log channel.</value>
    public ulong LogChannel { get; set; }

    /// <summary>
    ///     Gets or sets the color of the embed when announcing a new giveaway.
    /// </summary>
    /// <value>The giveaway embed color.</value>
    public int GiveawayColor { get; set; } = 0x7837FF;
}
