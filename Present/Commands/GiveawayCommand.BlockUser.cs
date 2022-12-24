using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Resources;
using X10D.DSharpPlus;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.BlockUser, CommandDescriptions.BlockUser, false)]
    [SlashRequireGuild]
    public async Task BlockUserAsync(InteractionContext context,
        [Option(OptionNames.User, OptionDescriptions.BlockUser)] DiscordUser user,
        [Option(OptionNames.Reason, OptionDescriptions.BlockReason)] string? reason = null
    )
    {
        var embed = new DiscordEmbedBuilder();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (user is null)
        {
            Logger.Warn($"{context.Member} attempted to block a null user");
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle(EmbedStrings.InvalidUser);
            embed.WithDescription(EmbedStrings.InvalidUser_Description);
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (_userExclusionService.IsUserExcluded(context.Guild, user))
        {
            Logger.Info($"{context.Member} attempted to block {user}, but this user is already blocked");
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle(EmbedStrings.AlreadyExcluded);
            embed.WithDescription(string.Format(EmbedStrings.BlockUser_AlreadyExcluded, user.Mention));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _userExclusionService.ExcludeUserAsync(context.Member, user, reason).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.ExclusionAdded);
        embed.WithDescription(string.Format(EmbedStrings.BlockUser_Success, user.Mention));
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason), EmbedStrings.Reason, reason);
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
