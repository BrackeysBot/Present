using System.Text;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Present.Resources;
using Present.Services;
using Humanizer;
using X10D.DSharpPlus;

namespace Present.Commands;

/// <summary>
///     Represents a class which implements the <c>info</c> command.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class InfoCommand : ApplicationCommandModule
{
    private readonly BotService _botService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="InfoCommand" /> class.
    /// </summary>
    /// <param name="botService">The bot service.</param>
    public InfoCommand(BotService botService)
    {
        _botService = botService;
    }

    [SlashCommand(CommandNames.Info, CommandDescriptions.Info)]
    [SlashRequireGuild]
    public async Task InfoAsync(InteractionContext context)
    {
        DiscordClient client = context.Client;
        DiscordMember member = (await client.CurrentUser.GetAsMemberOfAsync(context.Guild).ConfigureAwait(false))!;
        string botVersion = _botService.Version;

        var embed = new DiscordEmbedBuilder();
        embed.WithAuthor(member);
        embed.WithColor(member.Color);
        embed.WithThumbnail(member.AvatarUrl);
        embed.WithTitle($"Present v{botVersion}");
        embed.AddField("Ping", client.Ping, true);
        embed.AddField("Uptime", (DateTimeOffset.UtcNow - _botService.StartedAt).Humanize(), true);

        var builder = new StringBuilder();
        builder.AppendLine($"Present: {botVersion}");
        builder.AppendLine($"D#+: {client.VersionString}");
        builder.AppendLine($"Gateway: {client.GatewayVersion}");
        builder.AppendLine($"CLR: {Environment.Version.ToString(3)}");
        builder.AppendLine($"Host: {Environment.OSVersion}");

        embed.AddField("Version", Formatter.BlockCode(builder.ToString()));

        await context.CreateResponseAsync(embed, true).ConfigureAwait(false);
    }
}
