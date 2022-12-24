using DSharpPlus.SlashCommands;
using Present.Resources;
using Present.Services;
using NLog;

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
}
