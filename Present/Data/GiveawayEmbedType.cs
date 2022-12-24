namespace Present.Data;

/// <summary>
///     An enumeration of embed types for a giveaway.
/// </summary>
internal enum GiveawayEmbedType
{
    /// <summary>
    ///     Specifies that the embed should be the public embed.
    /// </summary>
    Public,

    /// <summary>
    ///     Specifies that the embed should appear as a creation embed in the audit log.
    /// </summary>
    AuditCreation,

    /// <summary>
    ///     Specifies that the embed should simply display information about the giveaway.
    /// </summary>
    Information
}
