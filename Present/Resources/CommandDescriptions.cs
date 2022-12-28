namespace Present.Resources;

internal static class CommandDescriptions
{
    public const string BlockRole = "Blocks a role from being able to win giveaways.";
    public const string BlockUser = "Blocks a user from being able to win giveaways.";
    public const string Create = "Creates a new giveaway.";
    public const string End = "Forcefully ends an ongoing giveaway. This bypasses the winner selection.";
    public const string Giveaways = "Manages giveaways.";
    public const string Info = "Displays information about the bot.";
    public const string Redraw = "Draws new winners for an ended giveaway, optionally keeping selected existing winners.";
    public const string SetWinners = "Updates the number of winners in an ongoing giveaway.";
    public const string UnblockRole = "Unblocks a role, so they are able to win giveaways.";
    public const string UnblockUser = "Unblocks a user, so they are able to win giveaways.";
    public const string ViewGiveaway = "View the details of a giveaway.";
}
