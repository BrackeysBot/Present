using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Resources;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.UnblockUser, CommandDescriptions.UnblockUser, false)]
    [SlashRequireGuild]
    public async Task UnblockUserAsync(InteractionContext context,
        [Option(OptionNames.User, OptionDescriptions.UnblockUser)] DiscordUser user
    )
    {
        var embed = new DiscordEmbedBuilder();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (user is null)
        {
            Logger.Info($"{context.Member} attempted to unblock a null user");
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle(EmbedStrings.InvalidUser);
            embed.WithDescription(EmbedStrings.InvalidUser_Description);
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (!_userExclusionService.IsUserExcluded(context.Guild, user))
        {
            Logger.Info($"{context.Member} attempted to unblock {user}, but this user was not already blocked");
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle(EmbedStrings.NoActionTaken);
            embed.WithDescription(string.Format(EmbedStrings.UnblockUser_AlreadyExcluded, user.Mention));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _userExclusionService.IncludeUserAsync(context.Member, user).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.ExclusionRemoved);
        embed.WithTitle(string.Format(EmbedStrings.UnblockUser_Success, user.Mention));
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
