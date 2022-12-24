using DSharpPlus.Entities;
using Present.Resources;

namespace Present.Data;

/// <summary>
///     Represents a structure that provides giveaway creation options.
/// </summary>
public readonly struct GiveawayCreationOptions
{
    private readonly int _winnerCount;
    private readonly string _title;
    private readonly string _description;
    private readonly DiscordUser _creator;
    private readonly DiscordChannel _channel;

    /// <summary>
    ///     Initializes a new instance of the <see cref="GiveawayCreationOptions" /> structure.
    /// </summary>
    public GiveawayCreationOptions()
    {
        _channel = null!;
        _creator = null!;
        _description = null!;
        StartTime = DateTimeOffset.UtcNow;
        EndTime = default;
        ImageUri = null;
        _title = null!;
        _winnerCount = 1;
    }

    /// <summary>
    ///     Gets or initializes the channel in which the giveaway is hosted.
    /// </summary>
    /// <value>A <see cref="DiscordChannel" /> representing the channel.</value>
    public DiscordChannel Channel
    {
        get => _channel;
        init => _channel = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Gets or initializes the user which created the giveaway.
    /// </summary>
    /// <value>A <see cref="DiscordUser" /> representing the channel.</value>
    public DiscordUser Creator
    {
        get => _creator;
        init => _creator = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Gets or initializes the description of the giveaway.
    /// </summary>
    /// <value>The description of the giveaway.</value>
    public string Description
    {
        get => _description;
        init
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));
            _description = value;
        }
    }

    /// <summary>
    ///     Gets or initializes the giveaway's end date time.
    /// </summary>
    /// <value>A <see cref="DateTimeOffset" /> representing the end date and time.</value>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>
    ///     Gets or initializes the image URI to display in the giveaway embed.
    /// </summary>
    /// <value>A <see cref="Uri" /> pointing to the image that should be displayed.</value>
    public Uri? ImageUri { get; init; }

    /// <summary>
    ///     Gets or initializes the title of the giveaway.
    /// </summary>
    /// <value>The title of the giveaway.</value>
    public string Title
    {
        get => _title;
        init
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentNullException(nameof(value));
            _title = value;
        }
    }

    /// <summary>
    ///     Gets or initializes the giveaway's start date time.
    /// </summary>
    /// <value>A <see cref="DateTimeOffset" /> representing the start date and time.</value>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>
    ///     Gets or initializes the winner count.
    /// </summary>
    /// <value>The winner count.</value>
    public int WinnerCount
    {
        get => _winnerCount;
        init
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), ExceptionMessages.WinnerCountMustBeGreaterThan0);
            _winnerCount = value;
        }
    }
}
