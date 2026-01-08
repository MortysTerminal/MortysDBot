using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MortysDBot.Core.Options;

namespace MortysDBot.Infrastructure.Twitch;

public sealed class TwitchScheduleClient : ITwitchScheduleClient
{
    private readonly HttpClient _http;
    private readonly ITwitchAuthClient _auth;
    private readonly TwitchOptions _opt;
    private readonly ILogger<TwitchScheduleClient> _log;

    public TwitchScheduleClient(HttpClient http, ITwitchAuthClient auth, IOptions<TwitchOptions> opt, ILogger<TwitchScheduleClient> log)
    {
        _http = http;
        _auth = auth;
        _opt = opt.Value;
        _log = log;
    }

    public async Task<IReadOnlyList<TwitchScheduleSegment>> GetSegmentsAsync(string broadcasterId, CancellationToken ct)
    {
        var token = await _auth.GetAppAccessTokenAsync(ct);

        using var req = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.twitch.tv/helix/schedule?broadcaster_id={Uri.EscapeDataString(broadcasterId)}");

        req.Headers.Add("Client-Id", _opt.ClientId);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        using var resp = await _http.SendAsync(req, ct);

        // 404 => schedule not found / not set up => treat as empty schedule
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _log.LogInformation("No Twitch schedule found for broadcasterId={BroadcasterId} (404). Treating as empty.", broadcasterId);
            return Array.Empty<TwitchScheduleSegment>();
        }

        // sonst normal: Fehler werfen
        resp.EnsureSuccessStatusCode();


        var payload = await resp.Content.ReadFromJsonAsync<ScheduleResponse>(cancellationToken: ct)
                      ?? throw new InvalidOperationException("Twitch schedule response was empty.");

        var segments = payload.Data?.Segments ?? new List<SegmentDto>();

        var mapped = segments.Select(s =>
        {
            var title = s.Title ?? "";
            var category = s.Category?.Name ?? "";
            return new TwitchScheduleSegment(
                s.Id ?? "",
                DateTimeOffset.Parse(s.StartTime),
                DateTimeOffset.Parse(s.EndTime),
                title,
                category);
        }).Where(x => !string.IsNullOrWhiteSpace(x.Id)).ToList();

        _log.LogInformation("Fetched {Count} schedule segments from Twitch for broadcasterId={BroadcasterId}.",
            mapped.Count, broadcasterId);

        return mapped;
    }


    private sealed class ScheduleResponse
    {
        [JsonPropertyName("data")] public DataDto? Data { get; set; }
    }

    private sealed class DataDto
    {
        [JsonPropertyName("segments")] public List<SegmentDto>? Segments { get; set; }
    }

    private sealed class SegmentDto
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("start_time")] public string StartTime { get; set; } = "";
        [JsonPropertyName("end_time")] public string EndTime { get; set; } = "";
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("category")] public CategoryDto? Category { get; set; }
    }

    private sealed class CategoryDto
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
    }
}
