using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Data;
using Present.Resources;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.SetWinners, CommandDescriptions.End, false)]
    [SlashRequireGuild]
    public async Task SetWinnersAsync(InteractionContext context,
        [Option(OptionNames.Id, OptionDescriptions.EndGiveawayId)] long giveawayId,
        [Option(OptionNames.WinnerCount, OptionDescriptions.WinnerCount)] long winnerCount
    )
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithTitle(EmbedStrings.InvalidGiveawayId);

        DiscordGuild guild = context.Guild;
        if (!_giveawayService.TryGetGiveaway(giveawayId, out Giveaway? giveaway) || giveaway.GuildId != guild.Id)
        {
            embed.WithDescription(string.Format(EmbedStrings.NoGiveawayFound, giveawayId));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (!_activeGiveawayService.IsGiveawayActive(giveaway))
        {
            embed.WithDescription(string.Format(EmbedStrings.GiveawayIsNotActive, giveawayId));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        if (winnerCount == giveaway.WinnerCount)
        {
            embed.WithDescription(string.Format(EmbedStrings.GiveawayUnchanged, giveawayId));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        giveaway.WinnerCount = (int) winnerCount;

        await _giveawayService.EditGiveawayAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.UpdateGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.UpdateGiveawayPublicMessageAsync(giveaway).ConfigureAwait(false);

        embed = _giveawayService.CreateGiveawayInformationEmbed(giveaway);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.GiveawayEdited_Title);
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
