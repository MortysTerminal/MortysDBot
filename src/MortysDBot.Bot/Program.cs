using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MortysDBot.Core.Options;
using MortysDBot.Infrastructure.Twitch;
using MortysDBot.Bot.Options;
using MortysDBot.Bot.Services;
using Serilog;
using Microsoft.EntityFrameworkCore;
using MortysDBot.Infrastructure.Data;

var builder = Host.CreateApplicationBuilder(args);

// Konfiguration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);

// DbContext
builder.Services.AddDbContext<MortysDbotDbContext>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("MortysDBot");
    if (string.IsNullOrWhiteSpace(cs))
        throw new InvalidOperationException("ConnectionStrings:MortysDBot must be set.");
    opt.UseNpgsql(cs);
});

// Serilog (liest "Serilog" Section aus appsettings.json)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Services.AddSerilog(); // NICHT chainen

// Options
builder.Services.AddOptions<DiscordOptions>()
    .Bind(builder.Configuration.GetSection(DiscordOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.Token), "Discord:Token must be set.")
    .ValidateOnStart();

// Module
builder.Services.AddSingleton<MortysDBot.Core.IBotInfo, MortysDBot.Bot.Services.BotInfo>();

// Twitch Clients
builder.Services.AddOptions<TwitchOptions>()
    .Bind(builder.Configuration.GetSection(TwitchOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.ClientId), "Twitch:ClientId must be set.")
    .Validate(o => !string.IsNullOrWhiteSpace(o.ClientSecret), "Twitch:ClientSecret must be set.")
    .Validate(o => o.Channels is { Count: > 0 }, "Twitch:Channels must contain at least one channel.")
    .Validate(o => o.Channels.All(c => !c.Enabled || !string.IsNullOrWhiteSpace(c.BroadcasterId)),
        "All enabled Twitch channels must have a BroadcasterId.")
    .ValidateOnStart();


builder.Services.AddOptions<ScheduleSyncOptions>()
    .Bind(builder.Configuration.GetSection(ScheduleSyncOptions.SectionName))
    .Validate(o => o.IntervalMinutes >= 1 && o.IntervalMinutes <= 1440, "ScheduleSync:IntervalMinutes must be 1..1440")
    .Validate(o => o.MaxDaysAhead >= 1 && o.MaxDaysAhead <= 30, "ScheduleSync:MaxDaysAhead must be 1..30")
    .ValidateOnStart();

builder.Services.AddHttpClient<ITwitchAuthClient, TwitchAuthClient>();
builder.Services.AddHttpClient<ITwitchScheduleClient, TwitchScheduleClient>();



// Add OpsSecurity
builder.Services.AddOptions<OpsSecurityOptions>()
    .Bind(builder.Configuration.GetSection(OpsSecurityOptions.SectionName))
    .ValidateOnStart();



// Discord
builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    GatewayIntents = GatewayIntents.Guilds,
    LogGatewayIntentWarnings = false
}));

builder.Services.AddSingleton(sp =>
{
    var client = sp.GetRequiredService<DiscordSocketClient>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("Discord.Interactions");

    var service = new InteractionService(client, new InteractionServiceConfig
    {
        UseCompiledLambda = true,
        ThrowOnError = false
    });

    // Logs aus InteractionService in unser Microsoft-Logging (Serilog) umleiten
    service.Log += msg =>
    {
        var level = msg.Severity switch
        {
            Discord.LogSeverity.Critical => LogLevel.Critical,
            Discord.LogSeverity.Error => LogLevel.Error,
            Discord.LogSeverity.Warning => LogLevel.Warning,
            Discord.LogSeverity.Info => LogLevel.Information,
            Discord.LogSeverity.Verbose => LogLevel.Debug,
            Discord.LogSeverity.Debug => LogLevel.Debug,
            _ => LogLevel.Information
        };

        logger.Log(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    };

    // Wenn ein SlashCommand ausgeführt wurde: Erfolg/Fehler loggen
    service.SlashCommandExecuted += (command, context, result) =>
    {
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "SlashCommand ok: {Command} | User={UserId} | Guild={GuildId} | Channel={ChannelId}",
                command.Name,
                context.User.Id,
                (context.Guild?.Id),
                context.Channel.Id);
        }
        else
        {
            logger.LogWarning(
                "SlashCommand failed: {Command} | User={UserId} | Guild={GuildId} | Error={Error} | Reason={Reason}",
                command?.Name ?? "<unknown>",
                context.User.Id,
                (context.Guild?.Id),
                result.Error?.ToString(),
                result.ErrorReason);
        }

        return Task.CompletedTask;
    };

    return service;
});

// Hosted Service
builder.Services.AddHostedService<DiscordBotHostedService>();
builder.Services.AddSingleton<IDiscordScheduledEventService, DiscordScheduledEventService>();
//builder.Services.AddHostedService<MortysDBot.Bot.HostedServices.TwitchScheduleDryRunWorker>();
builder.Services.AddHostedService<MortysDBot.Bot.HostedServices.TwitchScheduleSyncWorker>();


var app = builder.Build();

// DB migrations beim Start anwenden (Docker-friendly)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MortysDbotDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();
