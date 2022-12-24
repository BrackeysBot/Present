namespace Present.Interactivity;

/// <summary>
///     An enumeration of modal responses.
/// </summary>
internal enum DiscordModalResponse
{
    /// <summary>
    ///     The modal responded with a success.
    /// </summary>
    Success,

    /// <summary>
    ///     The user did not respond to the modal within the allotted time.
    /// </summary>
    Timeout
}
