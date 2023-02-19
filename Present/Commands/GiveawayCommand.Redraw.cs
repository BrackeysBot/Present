using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Data;
using Present.Resources;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.Redraw, CommandDescriptions.Redraw, false)]
    [SlashRequireGuild]
    public async Task RedrawAsync(InteractionContext context,
        [Option(OptionNames.Id, OptionDescriptions.RedrawGiveawayId)] long giveawayId,
        [Option(OptionNames.KeepIds, OptionDescriptions.RedrawKeep)] string? keepIds = null
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

        if (_activeGiveawayService.IsGiveawayActive(giveaway))
        {
            embed.WithDescription(string.Format(EmbedStrings.GiveawayIsActive, giveawayId));
            await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
            return;
        }

        var keep = new List<DiscordMember>();
        var invalidKeepIds = new List<string>();

        if (!string.IsNullOrWhiteSpace(keepIds))
        {
            foreach (string keepId in keepIds.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!ulong.TryParse(keepId, out ulong userId))
                {
                    invalidKeepIds.Add(keepId);
                    continue;
                }

                DiscordMember? member = await _giveawayService.ValidateUserAsync(userId, giveaway).ConfigureAwait(false);
                if (member is null) invalidKeepIds.Add(keepId);
                else keep.Add(member);
            }
        }

        IReadOnlyList<DiscordMember> winners = await _giveawayService.SelectWinnersAsync(giveaway, keep).ConfigureAwait(false);
        await _giveawayService.UpdateWinnersAsync(giveaway, winners).ConfigureAwait(false);

        await _giveawayService.EditGiveawayAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.UpdateGiveawayLogMessageAsync(giveaway).ConfigureAwait(false);
        await _giveawayService.UpdateGiveawayPublicMessageAsync(giveaway).ConfigureAwait(false);

        embed = await _giveawayService.CreateGiveawayInformationEmbedAsync(giveaway).ConfigureAwait(false);
        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.GiveawayEdited_Title);

        string? content = invalidKeepIds.Count > 0
            ? string.Format(EmbedStrings.InvalidKeepIds, string.Join(", ", invalidKeepIds.Select(i => $"`{i}`")))
            : null;

        await context.CreateResponseAsync(content, embed).ConfigureAwait(false);
    }
}
