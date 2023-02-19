using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Data;
using Present.Resources;
using SmartFormat;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.End, CommandDescriptions.End, false)]
    [SlashRequireGuild]
    public async Task EndAsync(InteractionContext context,
        [Option(OptionNames.Id, OptionDescriptions.EndGiveawayId)] long giveawayId
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

        _activeGiveawayService.RemoveActiveGiveaway(giveaway);
        await _giveawayService.EndGiveawayAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.LogGiveawayExpirationAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.UpdateGiveawayPublicMessageAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.UpdateGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Orange);
        embed.WithTitle(EmbedStrings.GiveawayEnded_Title);
        embed.WithDescription(EmbedStrings.GiveawayEnded_Description.FormatSmart(new {giveaway, winners = "0 winners"}));
        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
