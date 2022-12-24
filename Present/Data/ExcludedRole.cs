namespace Present.Data;

/// <summary>
///     Represents a role who is excluded from winning giveaways.
/// </summary>
internal sealed class ExcludedRole : IEquatable<ExcludedRole>
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
    ///     Gets or sets the role ID.
    /// </summary>
    /// <value>The role ID.</value>
    public ulong RoleId { get; set; }

    /// <summary>
    ///     Gets or sets the ID of the staff member which excluded this role.
    /// </summary>
    /// <value>The ID of the responsible staff member.</value>
    public ulong StaffMemberId { get; set; }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="ExcludedRole" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first excluded role.</param>
    /// <param name="right">The second excluded role.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator ==(ExcludedRole? left, ExcludedRole? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether two <see cref="ExcludedRole" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first excluded role.</param>
    /// <param name="right">The second excluded role.</param>
    /// <returns>
    ///     <see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise,
    ///     <see langword="false" />.
    /// </returns>
    public static bool operator !=(ExcludedRole? left, ExcludedRole? right)
    {
        return !Equals(left, right);
    }

    /// <summary>
    ///     Returns a value indicating whether this <see cref="ExcludedRole" /> and another <see cref="ExcludedRole" /> are equal.
    /// </summary>
    /// <param name="other">The other excluded role.</param>
    /// <returns>
    ///     <see langword="true" /> if this instance and <paramref name="other" /> are equal; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(ExcludedRole? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GuildId == other.GuildId && RoleId == other.RoleId;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is ExcludedRole other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // ReSharper disable NonReadonlyMemberInGetHashCode
        return HashCode.Combine(GuildId, RoleId);
        // ReSharper restore NonReadonlyMemberInGetHashCode
    }
}
