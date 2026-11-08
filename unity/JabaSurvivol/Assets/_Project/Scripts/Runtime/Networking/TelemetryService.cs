using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    public interface ITelemetryService
    {
        bool OptedIn { get; }
        int PendingCount { get; }
        bool Available { get; }
        void Configure(bool enabled, double sampleRate);
        void SetOptIn(bool optedIn);
        void Enqueue(string eventType, string payloadJson);
        Task<bool> FlushAsync(CancellationToken cancellationToken);
    }

    public sealed class TelemetryService : ITelemetryService, IDisposable
    {
        private const string OptInKey = "telemetry.optIn";
        private const int MaximumBatchSize = 100;
        private const int MaximumBodyBytes = 64 * 1024;
        private const int MaximumPayloadBytes = 4 * 1024;

        private static readonly HashSet<string> AllowedTypes = new(StringComparer.Ordinal)
        {
            "run_start",
            "run_end",
            "round_clear",
            "zombie_kill",
            "boss_defeat",
            "level_up",
            "reward_selection",
            "weapon_switch"
        };

        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;
        private readonly Func<string> _accessToken;
        private readonly Func<CancellationToken, Task<bool>> _refreshAsync;
        private readonly Func<int, TimeSpan> _retryDelay;
        private readonly Func<double, bool> _sampler;
        private readonly List<TelemetryEvent> _events = new();
        private readonly object _sync = new();
        private readonly SemaphoreSlim _flushGate = new(1, 1);
        private readonly Guid _sessionId = Guid.NewGuid();
        private bool _enabled;
        private double _sampleRate;

        public TelemetryService(
            Func<string> accessToken,
            Func<CancellationToken, Task<bool>> refreshAsync,
            Func<int, TimeSpan> retryDelay = null,
            Func<double, bool> sampler = null,
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
            _retryDelay = retryDelay ?? (attempt => TimeSpan.FromSeconds(1 << attempt));
            _sampler = sampler ?? (rate => UnityEngine.Random.value <= rate);
            OptedIn = PlayerPrefs.GetInt(OptInKey, 0) == 1;
        }

        public bool OptedIn { get; private set; }

        public int PendingCount
        {
            get
            {
                lock (_sync)
                {
                    return _events.Count;
                }
            }
        }

        public bool Available => _enabled;

        public void Configure(bool enabled, double sampleRate)
        {
            _enabled = enabled;
            _sampleRate = Math.Max(0d, Math.Min(1d, sampleRate));
        }

        public void SetOptIn(bool optedIn)
        {
            OptedIn = optedIn;
            PlayerPrefs.SetInt(OptInKey, optedIn ? 1 : 0);
            PlayerPrefs.Save();
            if (!optedIn)
            {
                lock (_sync)
                {
                    _events.Clear();
                }
            }
        }

        public void Enqueue(string eventType, string payloadJson)
        {
            if (!_enabled || !OptedIn || !AllowedTypes.Contains(eventType))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(payloadJson) ||
                Encoding.UTF8.GetByteCount(payloadJson) > MaximumPayloadBytes)
            {
                return;
            }

            if (!_sampler(_sampleRate))
            {
                return;
            }

            lock (_sync)
            {
                _events.Add(new TelemetryEvent(
                    Guid.NewGuid(),
                    eventType,
                    DateTimeOffset.UtcNow,
                    _sessionId,
                    payloadJson));
            }
        }

        public async Task<bool> FlushAsync(CancellationToken cancellationToken)
        {
            if (!_enabled || !OptedIn)
            {
                return false;
            }

            await _flushGate.WaitAsync(cancellationToken);
            try
            {
                while (true)
                {
                    var batch = TakeBatch();
                    if (batch.Count == 0)
                    {
                        return true;
                    }

                    var body = BuildEnvelope(batch);
                    var sent = false;
                    var lastStatus = 0;
                    for (var attempt = 0; attempt < 3 && !sent; attempt++)
                    {
                        var status = await SendAsync(body, _accessToken(), cancellationToken);
                        lastStatus = status;
                        if (status == 200)
                        {
                            sent = true;
                            break;
                        }

                        if (status == 401 && await _refreshAsync(cancellationToken))
                        {
                            continue;
                        }

                        if (attempt < 2)
                        {
                            await Task.Delay(_retryDelay(attempt), cancellationToken);
                        }
                    }

                    if (!sent)
                    {
                        if (lastStatus == 422)
                        {
                            lock (_sync)
                            {
                                foreach (var telemetryEvent in batch)
                                {
                                    _events.Remove(telemetryEvent);
                                }
                            }

                            continue;
                        }

                        return false;
                    }

                    lock (_sync)
                    {
                        foreach (var telemetryEvent in batch)
                        {
                            _events.Remove(telemetryEvent);
                        }
                    }
                }
            }
            finally
            {
                _flushGate.Release();
            }
        }

        public void Dispose()
        {
            _flushGate.Dispose();
            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }

        private List<TelemetryEvent> TakeBatch()
        {
            var batch = new List<TelemetryEvent>();
            lock (_sync)
            {
                foreach (var telemetryEvent in _events)
                {
                    batch.Add(telemetryEvent);
                    if (batch.Count == MaximumBatchSize)
                    {
                        break;
                    }
                }
            }

            while (batch.Count > 0 && Encoding.UTF8.GetByteCount(BuildEnvelope(batch)) > MaximumBodyBytes)
            {
                batch.RemoveAt(batch.Count - 1);
            }

            return batch;
        }

        private static string BuildEnvelope(IReadOnlyList<TelemetryEvent> events)
        {
            var builder = new StringBuilder("{\"events\":[");
            for (var index = 0; index < events.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(',');
                }

                var telemetryEvent = events[index];
                builder.Append("{\"eventId\":\"")
                    .Append(telemetryEvent.EventId)
                    .Append("\",\"eventType\":\"")
                    .Append(telemetryEvent.EventType)
                    .Append("\",\"occurredAt\":\"")
                    .Append(telemetryEvent.OccurredAt.ToString("O"))
                    .Append("\",\"sessionId\":\"")
                    .Append(telemetryEvent.SessionId)
                    .Append("\",\"payload\":")
                    .Append(telemetryEvent.Payload)
                    .Append('}');
            }

            builder.Append("]}");
            return builder.ToString();
        }

        private async Task<int> SendAsync(
            string body,
            string accessToken,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/telemetry/events");
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                return (int)response.StatusCode;
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

        private sealed class TelemetryEvent
        {
            public TelemetryEvent(Guid eventId, string eventType, DateTimeOffset occurredAt, Guid sessionId, string payload)
            {
                EventId = eventId;
                EventType = eventType;
                OccurredAt = occurredAt;
                SessionId = sessionId;
                Payload = payload;
            }

            public Guid EventId { get; }
            public string EventType { get; }
            public DateTimeOffset OccurredAt { get; }
            public Guid SessionId { get; }
            public string Payload { get; }
        }
    }
}
