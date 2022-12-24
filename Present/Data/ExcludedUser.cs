namespace Present.Data;

/// <summary>
///     Represents a user who is excluded from winning giveaways.
/// </summary>
internal sealed class ExcludedUser : IEquatable<ExcludedUser>
{
    /// <summary>
    ///     Gets or sets the guild ID.
    /// </summary>
    /// <value>The guild ID.</value>
    public ulong GuildId { get; set; }

    /// <summary>
    ///     Gets or sets the reason for the exclusion, if one was provided.
    /// </summary>
    /// <value>The reason for the exclusion.</value>
    public string? Reason { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member which excluded this user.
    /// </summary>
    /// <value>The ID of the responsible staff member.</value>
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Gets or sets the user ID.
    /// </summary>
    /// <value>The user ID.</value>
    public ulong UserId { get; set; }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="ExcludedUser" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first excluded user.</param>
    /// <param name="right">The second excluded user.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator ==(ExcludedUser? left, ExcludedUser? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="ExcludedUser" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first excluded user.</param>
    /// <param name="right">The second excluded user.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator !=(ExcludedUser? left, ExcludedUser? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether this <see cref="ExcludedUser" /> and another <see cref="ExcludedUser" /> are equal.
    /// </summary>
    /// <param name="other">The other excluded user.</param>
    /// <returns>
    ///     <see langword="true" /> if this instance and <paramref name="other" /> are equal; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(ExcludedUser? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GuildId == other.GuildId && UserId == other.UserId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ExcludedUser other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(GuildId, UserId);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
