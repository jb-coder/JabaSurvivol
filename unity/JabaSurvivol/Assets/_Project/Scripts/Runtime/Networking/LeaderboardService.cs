using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    [Serializable]
    public sealed class LeaderboardEntryDto
    {
        public int rank;
        public string username;
        public int highestRound;
        public int totalKills;
        public int durationSeconds;
        public string achievedAt;
    }

    [Serializable]
    public sealed class LeaderboardResponseDto
    {
        public string board;
        public LeaderboardEntryDto[] entries;
    }

    [Serializable]
    public sealed class LeaderboardSubmissionDto
    {
        public string runId;
        public int highestRound;
        public int totalKills;
        public int durationSeconds;
        public string clientVersion;
    }

    [Serializable]
    public sealed class LeaderboardSubmissionResponseDto
    {
        public bool accepted;
        public string runId;
        public int highestRound;
        public int totalKills;
        public int durationSeconds;
        public string achievedAt;
    }

    public sealed class LeaderboardResult
    {
        public bool Success { get; set; }
        public bool Empty { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public LeaderboardEntryDto[] Entries { get; set; } = Array.Empty<LeaderboardEntryDto>();
    }

    public interface ILeaderboardService
    {
        Task<LeaderboardResult> LoadAsync(CancellationToken cancellationToken);
        Task<bool> SubmitAsync(
            Guid runId,
            int highestRound,
            int totalKills,
            int durationSeconds,
            CancellationToken cancellationToken);
    }

    public sealed class LeaderboardService : ILeaderboardService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;
        private readonly Func<string> _accessToken;
        private readonly Func<CancellationToken, Task<bool>> _refreshAsync;
        private readonly Func<bool> _enabled;

        public LeaderboardService(
            Func<string> accessToken,
            Func<CancellationToken, Task<bool>> refreshAsync,
            Func<bool> enabled,
            string baseUrl = null,
            HttpMessageHandler handler = null)
        {
            var normalized = (baseUrl ?? ApiConfig.BaseUrl).TrimEnd('/');
            _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _ownsClient = handler is null;
            _httpClient.BaseAddress = new Uri(normalized + "/", UriKind.Absolute);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _accessToken = accessToken;
            _refreshAsync = refreshAsync;
            _enabled = enabled;
        }

        public async Task<LeaderboardResult> LoadAsync(CancellationToken cancellationToken)
        {
            if (!_enabled())
            {
                return new LeaderboardResult { Success = false, ErrorMessage = Localization.Get("leaderboard.disabled") };
            }

            try
            {
                using var response = await _httpClient.GetAsync(
                    "api/v1/game/leaderboards/highest-round?limit=50",
                    cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new LeaderboardResult { Success = true, Empty = true };
                }

                if (!response.IsSuccessStatusCode || response.Content is null)
                {
                    return new LeaderboardResult { Success = false, ErrorMessage = Localization.Get("leaderboard.error") };
                }

                var json = await response.Content.ReadAsStringAsync();
                var board = JsonUtility.FromJson<LeaderboardResponseDto>(json);
                var entries = board?.entries ?? Array.Empty<LeaderboardEntryDto>();
                return new LeaderboardResult
                {
                    Success = true,
                    Empty = entries.Length == 0,
                    Entries = entries
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return new LeaderboardResult { Success = false, ErrorMessage = Localization.Get("leaderboard.error") };
            }
        }

        public async Task<bool> SubmitAsync(
            Guid runId,
            int highestRound,
            int totalKills,
            int durationSeconds,
            CancellationToken cancellationToken)
        {
            if (!_enabled())
            {
                return false;
            }

            var body = JsonUtility.ToJson(new LeaderboardSubmissionDto
            {
                runId = runId.ToString(),
                highestRound = highestRound,
                totalKills = totalKills,
                durationSeconds = durationSeconds,
                clientVersion = Application.version
            });

            var response = await SendSubmissionAsync(body, _accessToken(), cancellationToken);
            if (response == HttpStatusCode.Unauthorized && await _refreshAsync(cancellationToken))
            {
                response = await SendSubmissionAsync(body, _accessToken(), cancellationToken);
            }

            return response == HttpStatusCode.OK;
        }

        public void Dispose()
        {
            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }

        private async Task<HttpStatusCode> SendSubmissionAsync(
            string body,
            string accessToken,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    "api/v1/game/leaderboards/highest-round/runs");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                return response.StatusCode;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
