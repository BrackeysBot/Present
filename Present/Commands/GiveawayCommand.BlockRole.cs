using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Resources;
using X10D.DSharpPlus;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.BlockRole, CommandDescriptions.BlockRole, false)]
    [SlashRequireGuild]
    public async Task BlockRoleAsync(InteractionContext context,
        [Option(OptionNames.Role, OptionDescriptions.BlockRole)] DiscordRole role,
        [Option(OptionNames.Reason, OptionDescriptions.BlockReason)] string? reason = null
    )
    {
        var embed = new DiscordEmbedBuilder();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (role is null)
        {
            Logger.Warn($"{context.Member} attempted to block a null role");
            embed.WithColor(DiscordColor.Red);
            embed.WithTitle(EmbedStrings.InvalidRole);
            embed.WithDescription(EmbedStrings.InvalidRole_Description);
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (_roleExclusionService.IsRoleExcluded(context.Guild, role))
        {
            Logger.Info($"{context.Member} attempted to block {role}, but this role is already blocked");
            embed.WithColor(DiscordColor.Orange);
            embed.WithTitle(EmbedStrings.AlreadyExcluded);
            embed.WithDescription(string.Format(EmbedStrings.BlockRole_AlreadyExcluded, role.Mention));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        await _roleExclusionService.ExcludeRoleAsync(context.Member, role, reason).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.ExclusionAdded);
        embed.WithDescription(string.Format(EmbedStrings.BlockRole_Success, role.Mention));
        embed.AddFieldIf(!string.IsNullOrWhiteSpace(reason), EmbedStrings.Reason, reason);
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
