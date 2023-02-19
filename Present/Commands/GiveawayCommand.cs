using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Present.Resources;
using Present.Services;
using NLog;
using Present.Interactivity;

namespace Present.Commands;

/// <summary>
///     Represents a class which implements the <c>giveaway</c> command group.
/// </summary>
[SlashCommandGroup(CommandNames.Giveaway, CommandDescriptions.Giveaways, false)]
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed partial class GiveawayCommand : ApplicationCommandModule
{
    private readonly ConfigurationService _configurationService;
    private readonly GiveawayService _giveawayService;
    private readonly ActiveGiveawayService _activeGiveawayService;
    private readonly RoleExclusionService _roleExclusionService;
    private readonly UserExclusionService _userExclusionService;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Initializes a new instance of the <see cref="GiveawayCommand" /> class.
    /// </summary>
    /// <param name="configurationService">The configuration service.</param>
    /// <param name="giveawayService">The giveaway service.</param>
    /// <param name="activeGiveawayService">The active giveaway service.</param>
    /// <param name="roleExclusionService">The role exclusion service.</param>
    /// <param name="userExclusionService">The user exclusion service.</param>
    public GiveawayCommand(
        ConfigurationService configurationService,
        GiveawayService giveawayService,
        ActiveGiveawayService activeGiveawayService,
        RoleExclusionService roleExclusionService,
        UserExclusionService userExclusionService
    )
    {
        _configurationService = configurationService;
        _giveawayService = giveawayService;
        _activeGiveawayService = activeGiveawayService;
        _roleExclusionService = roleExclusionService;
        _userExclusionService = userExclusionService;
    }

    private static async Task<DateTimeOffset> ValidateTimeStamp(
        InteractionContext context,
        DiscordModalTextInput timeInput,
        DiscordEmbedBuilder embed,
        DiscordFollowupMessageBuilder followup
    )
    {
        if (!TimeStampUtility.TryParse(timeInput.Value, out DateTimeOffset endTime))
        {
            Logger.Warn($"Provided time was invalid ({timeInput.Value}). Giveaway creation has been cancelled");
            embed.WithDescription(EmbedStrings.GiveawayCreation_InvalidTimestamp);
            followup.AsEphemeral();
            followup.AddEmbed(embed);
            await context.FollowUpAsync(followup).ConfigureAwait(false);
            return default;
        }

        if (endTime < DateTimeOffset.UtcNow)
        {
            Logger.Warn($"Provided time ({timeInput.Value}) is in the past. Giveaway creation has been cancelled");
            embed.WithDescription(EmbedStrings.GiveawayCreation_FutureTimestamp);
            followup.AsEphemeral();
            followup.AddEmbed(embed);
            await context.FollowUpAsync(followup).ConfigureAwait(false);
            return default;
        }

        return endTime;
    }
}
