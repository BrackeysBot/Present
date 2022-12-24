namespace Present.Data;

/// <summary>
///     An enumeration of results when attempting to add a user to a giveaway.
/// </summary>
internal enum GiveawayJoinResult
{
    /// <summary>
    ///     The user joined the giveaway successfully.
    /// </summary>
    Success,

    /// <summary>
    ///     The user is already an entrant to this giveaway.
    /// </summary>
    AlreadyAdded
}
