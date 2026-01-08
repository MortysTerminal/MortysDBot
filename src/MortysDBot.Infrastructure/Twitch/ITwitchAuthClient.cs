namespace MortysDBot.Infrastructure.Twitch;

public interface ITwitchAuthClient
{
    Task<string> GetAppAccessTokenAsync(CancellationToken ct);
}
