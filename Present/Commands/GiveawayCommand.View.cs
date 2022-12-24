using CSharpVitamins;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Data;
using Present.Resources;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.View, CommandDescriptions.ViewGiveaway, false)]
    [SlashRequireGuild]
    public async Task ViewAsync(InteractionContext context,
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

        embed = _giveawayService.CreateGiveawayInformationEmbed(giveaway);
        embed.WithTitle(EmbedStrings.GiveawayInformation);

        await context.CreateResponseAsync(embed).ConfigureAwait(false);
    }
}
