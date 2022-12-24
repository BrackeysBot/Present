using CSharpVitamins;

namespace Present.Data;

/// <summary>
///     Represents a giveaway.
/// </summary>
internal sealed class Giveaway : IEquatable<Giveaway>
{
    /// <summary>
    ///     Gets or sets the ID of the channel in which this giveaway was announced.
    /// </summary>
    /// <value>The channel ID.</value>
    public ulong ChannelId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the entrants in this giveaway.
    /// </summary>
    /// <value>A list containing the entrants' user IDs.</value>
    public List<ulong> Entrants { get; set; } = new();

    /// <summary>
    ///     Gets or sets the ID of the user who created this giveaway.
    /// </summary>
    /// <value>The creator's user ID.</value>
    public ulong CreatorId { get; set; }

    /// <summary>
    ///     Gets or sets the description of this giveaway.
    /// </summary>
    /// <value>The description.</value>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a value indicating whether the end of this giveaway has been handled.
    /// </summary>
    /// <value><see langword="true" /> if the end has been handled; otherwise, <see langword="false" />.</value>
    public bool EndHandled { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this giveaway ends.
    /// </summary>
    /// <value>The end date and time.</value>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    ///     Gets or sets the guild ID of the giveaway.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the giveaway.
    /// </summary>
    /// <value>The giveaway ID.</value>
    public ShortGuid Id { get; set; }

    /// <summary>
    ///     Gets or sets the image URI for this giveaway.
    /// </summary>
    /// <value>The image URI.</value>
    public Uri? ImageUri { get; set; }

    /// <summary>
    ///     Gets or sets the log message ID of the giveaway.
    /// </summary>
    /// <value>The log message ID.</value>
    public ulong LogMessageId { get; set; }

    /// <summary>
    ///     Gets or sets the message ID of the giveaway.
    /// </summary>
    /// <value>The message ID.</value>
    public ulong MessageId { get; set; }

    /// <summary>
    ///     Gets or sets the date and time at which this giveaway starts.
    /// </summary>
    /// <value>The start date and time.</value>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    ///     Gets or sets the title of this giveaway.
    /// </summary>
    /// <value>The title.</value>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the number of potential winners in this giveaway.
    /// </summary>
    /// <value>The number of potential winners.</value>
    public int WinnerCount { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the ID of the user who won this giveaway.
    /// </summary>
    /// <value>The winner's user ID, or <see langword="null" /> if this giveaway is ongoing.</value>
    public List<ulong> WinnerIds { get; set; } = new();

    /// <summary>
    ///     Returns a value indicating whether two <see cref="Giveaway" /> instances are equal.
    /// </summary>
    /// <param name="left">The first giveaway.</param>
    /// <param name="right">The second giveaway.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator ==(Giveaway? left, Giveaway? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="Giveaway" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first giveaway.</param>
    /// <param name="right">The second giveaway.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator !=(Giveaway? left, Giveaway? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether this <see cref="Giveaway" /> and another <see cref="Giveaway" /> are equal.
    /// </summary>
    /// <param name="other">The other giveaway.</param>
    /// <returns>
    ///     <see langword="true" /> if this instance and <paramref name="other" /> are equal; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Giveaway? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is Giveaway other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        return Id.GetHashCode();
    }
}
