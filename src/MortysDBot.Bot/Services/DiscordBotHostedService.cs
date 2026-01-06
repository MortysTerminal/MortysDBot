using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MortysDBot.Bot.Options;

namespace MortysDBot.Bot.Services;

public sealed class DiscordBotHostedService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly DiscordOptions _options;
    private readonly IHostEnvironment _env;
    private readonly ILogger<DiscordBotHostedService> _logger;

    public DiscordBotHostedService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        IOptions<DiscordOptions> options,
        IHostEnvironment env,
        ILogger<DiscordBotHostedService> logger)
    {
        _client = client;
        _interactions = interactions;
        _services = services;
        _options = options.Value;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var version = typeof(DiscordBotHostedService).Assembly.GetName().Version?.ToString() ?? "unknown";

        _logger.LogInformation(
            "Starting MortysDBot | Version={Version} | Env={Env} | GuildId={GuildId} | Machine={Machine}",
            version,
            _env.EnvironmentName,
            _options.GuildId,
            Environment.MachineName);


        _client.Log += msg =>
        {
            _logger.Log(
                msg.Severity switch
                {
                    LogSeverity.Critical => LogLevel.Critical,
                    LogSeverity.Error => LogLevel.Error,
                    LogSeverity.Warning => LogLevel.Warning,
                    LogSeverity.Info => LogLevel.Information,
                    LogSeverity.Verbose => LogLevel.Debug,
                    LogSeverity.Debug => LogLevel.Debug,
                    _ => LogLevel.Information
                },
                msg.Exception,
                "{Source}: {Message}",
                msg.Source,
                msg.Message
            );
            return Task.CompletedTask;
        };

        _client.Ready += OnReadyAsync;
        _client.InteractionCreated += OnInteractionCreatedAsync;

        await _client.LoginAsync(TokenType.Bot, _options.Token);
        await _client.StartAsync();

        _logger.LogInformation("Discord client started. Waiting for shutdown signal...");
        _logger.LogInformation("MortysDBot started.");

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // expected on shutdown
        }
        finally
        {
            _logger.LogInformation("Shutdown requested. Stopping Discord client...");
            await _client.StopAsync();
            _logger.LogInformation("Discord client stopped.");
            _logger.LogInformation("MortysDBot stopped.");
        }
    }

    private async Task OnReadyAsync()
    {
        // Discover modules
        await _interactions.AddModulesAsync(typeof(MortysDBot.Modules.ModuleAssemblyMarker).Assembly, _services);

        // Register commands
        if (_options.GuildId != 0)
        {
            await _interactions.RegisterCommandsToGuildAsync(_options.GuildId);
            _logger.LogInformation("Registered commands to guild {GuildId}.", _options.GuildId);
        }
        else
        {
            await _interactions.RegisterCommandsGloballyAsync();
            _logger.LogInformation("Registered commands globally.");
        }

        try
        {
            Directory.CreateDirectory("health");
            await File.WriteAllTextAsync("health/ready.txt", DateTimeOffset.UtcNow.ToString("O"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write health file.");
        }


    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        try
        {
            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while handling interaction {InteractionId}.", interaction.Id);

            // Optional: Wenn du wirklich keinerlei User-Message willst, lass das komplett weg.
            // Falls du es früher drin hattest, kannst du es auch entfernen.
            if (interaction.Type == InteractionType.ApplicationCommand)
            {
                try
                {
                    await interaction.RespondAsync("❌ Fehler beim Ausführen des Commands.", ephemeral: true);
                }
                catch
                {
                    // ignore
                }
            }
        }
    }
}
