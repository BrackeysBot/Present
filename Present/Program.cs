using DSharpPlus;
using Present.Data;
using Present.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using X10D.Hosting.DependencyInjection;

Directory.CreateDirectory("data");

await Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(builder => builder.AddJsonFile("data/config.json", true, true))
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddNLog();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(new DiscordClient(new DiscordConfiguration
        {
            Token = Environment.GetEnvironmentVariable("DISCORD_TOKEN"),
            LoggerFactory = new NLogLoggerFactory(),
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMembers | DiscordIntents.MessageContents
        }));

        services.AddHostedSingleton<LoggingService>();

        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<Random>();

        services.AddHostedSingleton<DiscordLogService>();
        services.AddHostedSingleton<GiveawayService>();
        services.AddHostedSingleton<ActiveGiveawayService>();
        services.AddHostedSingleton<GiveawayEntrantService>();
        services.AddHostedSingleton<RoleExclusionService>();
        services.AddHostedSingleton<UserExclusionService>();

        services.AddDbContext<GiveawayContext>();

        services.AddHostedSingleton<BotService>();
    })
    .UseConsoleLifetime()
    .RunConsoleAsync();
