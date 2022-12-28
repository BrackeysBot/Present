using CSharpVitamins;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Data;
using Present.Resources;
using X10D.DSharpPlus;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.ViewEntrants, CommandDescriptions.ViewEntrants, false)]
    [SlashRequireGuild]
    public async Task ViewEntrantsAsync(InteractionContext context,
        [Option(OptionNames.Id, OptionDescriptions.ViewGiveawayId)] string idRaw
    )
    {
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithTitle(EmbedStrings.InvalidGiveawayId);

        if (!ShortGuid.TryParse(idRaw, out ShortGuid giveawayId))
        {
            embed.WithDescription(string.Format(EmbedStrings.InvalidId, idRaw));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        DiscordGuild guild = context.Guild;
        if (!_giveawayService.TryGetGiveaway(giveawayId, out Giveaway? giveaway) || giveaway.GuildId != guild.Id)
        {
            embed.WithDescription(string.Format(EmbedStrings.NoGiveawayFound, giveawayId));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        embed.WithColor(DiscordColor.CornflowerBlue);
        embed.WithTitle(EmbedStrings.GiveawayEntrants);

        embed.AddField("Entrants", giveaway.Entrants.Count.ToString("0"));
        if (giveaway.Entrants.Count > 0)
            embed.WithDescription(string.Join('\n', giveaway.Entrants.Select(e => $"• {MentionUtility.MentionUser(e)} ({e:0})")));

        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
