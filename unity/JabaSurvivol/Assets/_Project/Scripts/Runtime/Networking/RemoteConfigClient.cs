using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    [Serializable]
    public sealed class RemoteConfigDto
    {
        public int version;
        public string minimumClientVersion;
        public bool maintenanceMode;
        public bool cloudSavesEnabled;
        public bool leaderboardsEnabled;
        public bool telemetryEnabled;
        public double telemetrySampleRate;
    }

    public sealed class RemoteConfigClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;
        private RemoteConfigDto _cached;

        public RemoteConfigClient(string baseUrl = null, HttpMessageHandler handler = null)
        {
            var normalized = (baseUrl ?? ApiConfig.BaseUrl).TrimEnd('/');
            _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _ownsClient = handler is null;
            _httpClient.BaseAddress = new Uri(normalized + "/", UriKind.Absolute);
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
        }

        public async Task<RemoteConfigDto> GetAsync(CancellationToken cancellationToken)
        {
            if (_cached != null)
            {
                return _cached;
            }

            try
            {
                using var response = await _httpClient.GetAsync("api/v1/game/config", cancellationToken);
                if (!response.IsSuccessStatusCode || response.Content is null)
                {
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                _cached = JsonUtility.FromJson<RemoteConfigDto>(json);
                return _cached;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }
    }
}
