using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Resources;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.UnblockRole, CommandDescriptions.UnblockRole, false)]
    [SlashRequireGuild]
    public async Task UnblockRoleAsync(InteractionContext context,
        [Option(OptionNames.Role, OptionDescriptions.UnblockRole)] DiscordRole role
    )
    {
        var embed = new DiscordEmbedBuilder();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (role is null)
        {
            Logger.Info($"{context.Member} attempted to unblock a null role");
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle(EmbedStrings.InvalidRole);
            embed.WithDescription(EmbedStrings.InvalidRole_Description);
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (!_roleExclusionService.IsRoleExcluded(context.Guild, role))
        {
            Logger.Info($"{context.Member} attempted to unblock {role}, but this role was not already blocked");
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle(EmbedStrings.NoActionTaken);
            embed.WithDescription(string.Format(EmbedStrings.UnblockRole_AlreadyExcluded, role.Mention));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _roleExclusionService.IncludeRoleAsync(context.Member, role).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.ExclusionRemoved);
        embed.WithTitle(string.Format(EmbedStrings.UnblockRole_Success, role.Mention));
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
