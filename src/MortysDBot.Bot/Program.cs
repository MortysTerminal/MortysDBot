using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MortysDBot.Core.Options;
using MortysDBot.Bot.Options;
using MortysDBot.Bot.Services;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Konfiguration
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddUserSecrets<Program>(optional: true);


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

await builder.Build().RunAsync();
