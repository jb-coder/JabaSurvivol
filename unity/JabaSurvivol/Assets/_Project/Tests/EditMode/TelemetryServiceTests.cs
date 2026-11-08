using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class TelemetryServiceTests
    {
        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey("telemetry.optIn");
            PlayerPrefs.Save();
        }

        [Test]
        public async Task Queue_is_off_by_default_and_does_not_send()
        {
            var handler = new SequenceHandler(HttpStatusCode.OK);
            using var service = CreateService(handler);

            service.Configure(true, 1d);
            service.Enqueue("run_start", "{\"startingRound\":1}");

            Assert.That(service.PendingCount, Is.EqualTo(0));
            Assert.That(await service.FlushAsync(CancellationToken.None), Is.False);
            Assert.That(handler.CallCount, Is.EqualTo(0));
        }

        [Test]
        public async Task Opted_in_events_batch_and_retry_then_clear()
        {
            var handler = new SequenceHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            using var service = CreateService(handler);
            service.Configure(true, 1d);
            service.SetOptIn(true);
            service.Enqueue("run_start", "{\"startingRound\":1}");
            service.Enqueue("zombie_kill", "{\"round\":1,\"archetype\":\"walker\"}");

            var flushed = await service.FlushAsync(CancellationToken.None);

            Assert.That(flushed, Is.True);
            Assert.That(service.PendingCount, Is.EqualTo(0));
            Assert.That(handler.CallCount, Is.EqualTo(3));
            Assert.That(handler.LastBody, Does.Contain("run_start"));
            Assert.That(handler.LastBody, Does.Contain("zombie_kill"));
        }

        [Test]
        public void Opt_in_persists_in_player_prefs()
        {
            using var service = CreateService(new SequenceHandler(HttpStatusCode.OK));
            service.SetOptIn(true);

            Assert.That(PlayerPrefs.GetInt("telemetry.optIn", 0), Is.EqualTo(1));
            using var reloaded = CreateService(new SequenceHandler(HttpStatusCode.OK));
            Assert.That(reloaded.OptedIn, Is.True);
        }

        [Test]
        public void Disabled_telemetry_drops_events()
        {
            using var service = CreateService(new SequenceHandler(HttpStatusCode.OK));
            service.Configure(false, 1d);
            service.SetOptIn(true);

            service.Enqueue("run_start", "{\"startingRound\":1}");

            Assert.That(service.PendingCount, Is.EqualTo(0));
            Assert.That(service.Available, Is.False);
        }

        private static TelemetryService CreateService(HttpMessageHandler handler)
        {
            return new TelemetryService(
                () => "token",
                _ => Task.FromResult(true),
                _ => TimeSpan.Zero,
                _ => true,
                "http://localhost",
                handler);
        }

        private sealed class SequenceHandler : HttpMessageHandler
        {
            private readonly Queue<HttpStatusCode> _statuses;

            public SequenceHandler(params HttpStatusCode[] statuses)
            {
                _statuses = new Queue<HttpStatusCode>(statuses);
            }

            public int CallCount { get; private set; }
            public string LastBody { get; private set; } = string.Empty;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                CallCount++;
                LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
                var status = _statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK;
                return new HttpResponseMessage(status)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
