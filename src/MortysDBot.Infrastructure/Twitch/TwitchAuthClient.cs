using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MortysDBot.Core.Options;

namespace MortysDBot.Infrastructure.Twitch;

public sealed class TwitchAuthClient : ITwitchAuthClient
{
    private readonly HttpClient _http;
    private readonly TwitchOptions _opt;
    private readonly ILogger<TwitchAuthClient> _log;

    private string? _token;
    private DateTimeOffset _expiresAtUtc;

    public TwitchAuthClient(HttpClient http, IOptions<TwitchOptions> opt, ILogger<TwitchAuthClient> log)
    {
        _http = http;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<string> GetAppAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(_token) && DateTimeOffset.UtcNow < _expiresAtUtc.AddMinutes(-2))
            return _token;

        // Client Credentials Flow :contentReference[oaicite:9]{index=9}
        var form = new Dictionary<string, string>
        {
            ["client_id"] = _opt.ClientId,
            ["client_secret"] = _opt.ClientSecret,
            ["grant_type"] = "client_credentials"
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, "https://id.twitch.tv/oauth2/token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: ct)
                     ?? throw new InvalidOperationException("Twitch token response was empty.");

        _token = payload.AccessToken;
        _expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);

        _log.LogInformation("Fetched Twitch app token (expires in {Seconds}s).", payload.ExpiresIn);
        return _token;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string TokenType { get; set; } = "";
    }
}
