using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using NHLGameSchedule.Models;

namespace NHLGameSchedule.Services;

public interface INhlScheduleService
{
    Task<IReadOnlyList<NhlGame>> GetHomeGamesAsync(
        IEnumerable<string> teamAbbreviations,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default);
}

public sealed class NhlScheduleService(
    HttpClient httpClient,
    IMemoryCache memoryCache) : INhlScheduleService
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(24);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<IReadOnlyList<NhlGame>> GetHomeGamesAsync(
        IEnumerable<string> teamAbbreviations,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken = default)
    {
        var distinctTeams = teamAbbreviations
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Select(a => a.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (distinctTeams.Length == 0)
        {
            return Array.Empty<NhlGame>();
        }

        var cacheKey = CreateCacheKey(distinctTeams, fromDateUtc, toDateUtc);
        var cachedGames = await memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;

                var tasks = distinctTeams.Select(abbrev =>
                    FetchForTeamAsync(abbrev, fromDateUtc, toDateUtc, cancellationToken));
                var results = await Task.WhenAll(tasks);

                return results.SelectMany(r => r)
                    .OrderBy(g => g.GameDate)
                    .ToArray();
            });

        return cachedGames ?? Array.Empty<NhlGame>();
    }

    private static string CreateCacheKey(
        IEnumerable<string> teamAbbreviations,
        DateTime fromDateUtc,
        DateTime toDateUtc)
    {
        var teams = string.Join(",", teamAbbreviations.Order(StringComparer.OrdinalIgnoreCase));
        return $"nhl-home-games:{teams}:{fromDateUtc:O}:{toDateUtc:O}";
    }

    private async Task<IReadOnlyList<NhlGame>> FetchForTeamAsync(
        string teamAbbrev,
        DateTime fromDateUtc,
        DateTime toDateUtc,
        CancellationToken cancellationToken)
    {
        // Endpoint example:
        // https://api-web.nhle.com/v1/club-schedule/ANA/week/now
        // Response includes "nextStartDate" to page week-by-week.

        var games = new List<NhlGame>();

        var nextWeekStart = fromDateUtc.ToString("yyyy-MM-dd");

        // Safety cap to avoid infinite loops if the API ever returns a bad nextStartDate.
        for (var i = 0; i < 20; i++)
        {
            var url = $"club-schedule/{teamAbbrev.ToUpperInvariant()}/week/{nextWeekStart}";

            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var root = await JsonSerializer.DeserializeAsync<ClubScheduleRoot>(stream, JsonOptions, cancellationToken)
                       ?? new ClubScheduleRoot();

            if (root.Games is not null)
            {
                foreach (var g in root.Games)
                {
                    var startTime = g.StartTimeUtc;
                    if (startTime < fromDateUtc || startTime > toDateUtc)
                    {
                        continue;
                    }

                    var homeAbbrev = g.HomeTeam?.Abbrev ?? string.Empty;
                    var awayAbbrev = g.AwayTeam?.Abbrev ?? string.Empty;

                    // Only include home games for this team.
                    if (!homeAbbrev.Equals(teamAbbrev, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    games.Add(new NhlGame(
                        HomeTeam: homeAbbrev,
                        AwayTeam: awayAbbrev,
                        GameDate: startTime,
                        Arena: g.Venue?.Default ?? "Unknown arena",
                        HomeScore: g.HomeTeam?.Score,
                        AwayScore: g.AwayTeam?.Score,
                        IsFinal: g.GameState is "FINAL" or "OFF" or "OVER",
                        GameId: g.Id,
                        GameCenterLink: CreateGameCenterLink(
                            g.GameCenterLink, awayAbbrev, homeAbbrev, startTime, g.Id)));
                }

            }

            if (string.IsNullOrWhiteSpace(root.NextStartDate))
            {
                break;
            }

            // Stop paging once we've passed the requested window.
            if (DateTime.TryParse(root.NextStartDate, out var nextStartDate) && nextStartDate.ToUniversalTime() > toDateUtc)
            {
                break;
            }

            nextWeekStart = root.NextStartDate;
        }

        return games;
    }

    private static string CreateGameCenterLink(
        string? apiLink,
        string awayTeam,
        string homeTeam,
        DateTime startTimeUtc,
        int gameId)
    {
        if (!string.IsNullOrWhiteSpace(apiLink))
        {
            var path = Uri.TryCreate(apiLink, UriKind.Absolute, out var absoluteUri)
                ? absoluteUri.PathAndQuery
                : apiLink.StartsWith('/') ? apiLink : $"/{apiLink}";

            return new Uri(new Uri("https://www.nhl.com"), path).ToString();
        }

        return $"https://www.nhl.com/gamecenter/{awayTeam.ToLowerInvariant()}-vs-{homeTeam.ToLowerInvariant()}/{startTimeUtc.ToLocalTime():yyyy/MM/dd}/{gameId}";
    }

    // DTOs for api-web.nhle.com club schedule JSON
    private sealed class ClubScheduleRoot
    {
        [JsonPropertyName("nextStartDate")]
        public string? NextStartDate { get; set; }

        [JsonPropertyName("games")]
        public List<ClubScheduleGame>? Games { get; set; }
    }

    private sealed class ClubScheduleGame
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("startTimeUTC")]
        public DateTime StartTimeUtc { get; set; }

        [JsonPropertyName("venue")]
        public ClubScheduleVenue? Venue { get; set; }

        [JsonPropertyName("awayTeam")]
        public ClubScheduleTeam? AwayTeam { get; set; }

        [JsonPropertyName("homeTeam")]
        public ClubScheduleTeam? HomeTeam { get; set; }

        [JsonPropertyName("gameState")]
        public string? GameState { get; set; }

        [JsonPropertyName("gameCenterLink")]
        public string? GameCenterLink { get; set; }
    }

    private sealed class ClubScheduleTeam
    {
        [JsonPropertyName("abbrev")]
        public string? Abbrev { get; set; }

        [JsonPropertyName("score")]
        public int? Score { get; set; }
    }

    private sealed class ClubScheduleVenue
    {
        [JsonPropertyName("default")]
        public string? Default { get; set; }
    }
}
