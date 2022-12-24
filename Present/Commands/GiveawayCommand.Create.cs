using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Configuration;
using Present.Data;
using Present.Interactivity;
using Present.Resources;
using X10D.DSharpPlus;

namespace Present.Commands;

internal sealed partial class GiveawayCommand
{
    [SlashCommand(CommandNames.Create, CommandDescriptions.Create, false)]
    [SlashRequireGuild]
    public async Task CreateAsync(InteractionContext context,
        [Option(OptionNames.Channel, OptionDescriptions.Channel)] DiscordChannel channel,
        [Option(OptionNames.WinnerCount, OptionDescriptions.WinnerCount)] long winnerCount
    )
    {
        var modal = new DiscordModalBuilder(context.Client);
        modal.WithTitle("Create Giveaway");

        DiscordModalTextInput titleInput = modal.AddInput(EmbedStrings.Title, "e.g. Halo Infinite: Steam Key", maxLength: 255);
        DiscordModalTextInput descriptionInput = modal.AddInput(EmbedStrings.Description, "Add a brief description here",
            maxLength: 4000, inputStyle: TextInputStyle.Paragraph);
        DiscordModalTextInput timeInput = modal.AddInput("End Time", "e.g 1w *OR* 1681945200");
        DiscordModalTextInput imageInput = modal.AddInput("Thumbnail", "Optional. An image URL", isRequired: false);

        DiscordModalResponse response =
            await modal.Build().RespondToAsync(context.Interaction, TimeSpan.FromMinutes(10)).ConfigureAwait(false);

        if (response == DiscordModalResponse.Timeout)
        {
            Logger.Warn("Modal responded with a timeout. Giveaway creation has been cancelled");
            return;
        }

        var followup = new DiscordFollowupMessageBuilder();
        var embed = new DiscordEmbedBuilder();
        embed.WithColor(DiscordColor.Red);
        embed.WithTitle(EmbedStrings.GiveawayCreationFailed);

        if (string.IsNullOrWhiteSpace(titleInput.Value))
        {
            Logger.Warn("Provided title was empty. Giveaway creation has been cancelled");
            embed.WithDescription(EmbedStrings.GiveawayCreation_NoTitle);
            followup.AsEphemeral();
            followup.AddEmbed(embed);
            await context.FollowUpAsync(followup).ConfigureAwait(false);
            return;
        }

        if (string.IsNullOrWhiteSpace(descriptionInput.Value))
        {
            Logger.Warn("Provided description was empty. Giveaway creation has been cancelled");
            embed.WithDescription(EmbedStrings.GiveawayCreation_NoDescription);
            followup.AsEphemeral();
            followup.AddEmbed(embed);
            await context.FollowUpAsync(followup).ConfigureAwait(false);
            return;
        }

        DateTimeOffset endTime = await ValidateTimeStamp(context, timeInput, embed, followup).ConfigureAwait(false);
        if (endTime == default) return;

        Uri? imageUri = null;
        if (!string.IsNullOrWhiteSpace(imageInput.Value) && Uri.IsWellFormedUriString(imageInput.Value, UriKind.Absolute))
            imageUri = new Uri(imageInput.Value);

        string title = titleInput.Value;
        string description = descriptionInput.Value;

        DiscordMember creator = context.Member;
        var options = new GiveawayCreationOptions
        {
            Title = title,
            Description = description,
            WinnerCount = (int) winnerCount,
            EndTime = endTime,
            Channel = channel,
            Creator = creator,
            ImageUri = imageUri
        };

        Giveaway giveaway = await _giveawayService.CreateGiveawayAsync(options).ConfigureAwait(false);
        _activeGiveawayService.AddActiveGiveaway(giveaway);
        await _giveawayService.AnnounceGiveawayAsync(giveaway).ConfigureAwait(false);

        embed.WithColor(DiscordColor.Green);
        embed.WithTitle(EmbedStrings.GiveawayCreated_Title);
        embed.WithDescription(string.Format(EmbedStrings.GiveawayCreated_Description, giveaway.Title, channel.Mention));
        if (_configurationService.TryGetGuildConfiguration(context.Guild, out GuildConfiguration? guildConfiguration))
        {
            string logChannelMention = MentionUtility.MentionChannel(guildConfiguration.LogChannel);
            embed.Description += string.Format(EmbedStrings.LogChannel_MoreDetails, logChannelMention);
        }

        followup.AddEmbed(embed);
        await context.FollowUpAsync(followup).ConfigureAwait(false);
    }

    private static async Task<DateTimeOffset> ValidateTimeStamp(
        InteractionContext context,
        DiscordModalTextInput timeInput,
        DiscordEmbedBuilder embed,
        DiscordFollowupMessageBuilder followup
    )
    {
        if (!TimeStampUtility.TryParse(timeInput.Value, out DateTimeOffset endTime))
        {
            Logger.Warn($"Provided time was invalid ({timeInput.Value}). Giveaway creation has been cancelled");
            embed.WithDescription(EmbedStrings.GiveawayCreation_InvalidTimestamp);
            followup.AsEphemeral();
            followup.AddEmbed(embed);
            await context.FollowUpAsync(followup).ConfigureAwait(false);
            return default;
        }

        if (endTime < DateTimeOffset.UtcNow)
        {
            Logger.Warn($"Provided time ({timeInput.Value}) is in the past. Giveaway creation has been cancelled");
            embed.WithDescription(EmbedStrings.GiveawayCreation_FutureTimestamp);
            followup.AsEphemeral();
            followup.AddEmbed(embed);
            await context.FollowUpAsync(followup).ConfigureAwait(false);
            return default;
        }

        return endTime;
    }
}
