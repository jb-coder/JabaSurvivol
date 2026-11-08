using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    [Serializable]
    public sealed class CloudSaveDto
    {
        public string slot;
        public long revision;
        public int schemaVersion;
        public string updatedAt;
        public string etag;
        public Profile document;
    }

    [Serializable]
    public sealed class CloudSaveWriteDto
    {
        public long baseRevision;
        public Profile document;
    }

    [Serializable]
    public sealed class CloudSaveProblemDto
    {
        public string code;
        public long currentRevision;
        public string currentUpdatedAt;
        public string detail;
    }

    public enum CloudSaveApiStatus
    {
        Success,
        NotFound,
        Conflict,
        Unauthorized,
        NetworkError,
        Error
    }

    public sealed class CloudSaveApiResult
    {
        public CloudSaveApiStatus Status { get; set; }
        public CloudSaveDto Save { get; set; }
        public long CurrentRevision { get; set; }
        public string CurrentUpdatedAt { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }

    public interface ICloudSaveApiClient
    {
        Task<CloudSaveApiResult> GetAsync(CancellationToken cancellationToken);
        Task<CloudSaveApiResult> PutAsync(long baseRevision, Profile profile, CancellationToken cancellationToken);
    }

    public sealed class CloudSaveApiClient : ICloudSaveApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;
        private readonly Func<string> _accessToken;
        private readonly Func<CancellationToken, Task<bool>> _refreshAsync;

        public CloudSaveApiClient(
            Func<string> accessToken,
            Func<CancellationToken, Task<bool>> refreshAsync,
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
        }

        public async Task<CloudSaveApiResult> GetAsync(CancellationToken cancellationToken)
        {
            return await SendAsync(HttpMethod.Get, "api/v1/game/saves/primary", null, cancellationToken);
        }

        public async Task<CloudSaveApiResult> PutAsync(
            long baseRevision,
            Profile profile,
            CancellationToken cancellationToken)
        {
            var body = JsonUtility.ToJson(new CloudSaveWriteDto
            {
                baseRevision = baseRevision,
                document = profile
            });
            return await SendAsync(HttpMethod.Put, "api/v1/game/saves/primary", body, cancellationToken);
        }

        public void Dispose()
        {
            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }

        private async Task<CloudSaveApiResult> SendAsync(
            HttpMethod method,
            string path,
            string body,
            CancellationToken cancellationToken)
        {
            var response = await SendOnceAsync(method, path, body, _accessToken(), cancellationToken);
            if (response.Status == CloudSaveApiStatus.Unauthorized)
            {
                if (!await _refreshAsync(cancellationToken))
                {
                    return response;
                }

                response = await SendOnceAsync(method, path, body, _accessToken(), cancellationToken);
            }

            return response;
        }

        private async Task<CloudSaveApiResult> SendOnceAsync(
            HttpMethod method,
            string path,
            string body,
            string accessToken,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var text = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return new CloudSaveApiResult { Status = CloudSaveApiStatus.NotFound };
                }

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return new CloudSaveApiResult { Status = CloudSaveApiStatus.Unauthorized };
                }

                if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    var problem = ParseProblem(text);
                    return new CloudSaveApiResult
                    {
                        Status = CloudSaveApiStatus.Conflict,
                        CurrentRevision = problem?.currentRevision ?? 0,
                        CurrentUpdatedAt = problem?.currentUpdatedAt ?? string.Empty,
                        ErrorCode = problem?.code ?? "revision_conflict"
                    };
                }

                if (!response.IsSuccessStatusCode)
                {
                    var problem = ParseProblem(text);
                    return new CloudSaveApiResult
                    {
                        Status = CloudSaveApiStatus.Error,
                        ErrorCode = problem?.code ?? $"http_{(int)response.StatusCode}",
                        Save = null
                    };
                }

                return new CloudSaveApiResult
                {
                    Status = CloudSaveApiStatus.Success,
                    Save = string.IsNullOrWhiteSpace(text) ? null : JsonUtility.FromJson<CloudSaveDto>(text)
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return new CloudSaveApiResult { Status = CloudSaveApiStatus.NetworkError };
            }
        }

        private static CloudSaveProblemDto ParseProblem(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<CloudSaveProblemDto>(text);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
