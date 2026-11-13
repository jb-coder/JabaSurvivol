using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Gameplay;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class LeaderboardServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            Localization.SetLocale("en");
        }

        [TearDown]
        public void TearDown()
        {
            Localization.SetLocale("en");
        }

        [Test]
        public async Task Load_parses_entries_and_request_path()
        {
            var handler = new QueueHandler(
                Json(HttpStatusCode.OK,
                    "{\"board\":\"highest-round\",\"entries\":[{\"rank\":1,\"username\":\"Top\",\"highestRound\":9,\"totalKills\":500,\"durationSeconds\":900,\"achievedAt\":\"2026-11-08T12:00:00Z\"}]}"));
            using var service = CreateService(handler);

            var result = await service.LoadAsync(CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Entries.Length, Is.EqualTo(1));
            Assert.That(result.Entries[0].username, Is.EqualTo("Top"));
            Assert.That(result.Entries[0].highestRound, Is.EqualTo(9));
            Assert.That(handler.LastPath, Does.Contain("highest-round?limit=50"));
        }

        [Test]
        public async Task Load_returns_empty_on_not_found()
        {
            using var service = CreateService(new QueueHandler(new HttpResponseMessage(HttpStatusCode.NotFound)));

            var result = await service.LoadAsync(CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Empty, Is.True);
        }

        [Test]
        public async Task Load_maps_server_errors()
        {
            using var service = CreateService(new QueueHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

            var result = await service.LoadAsync(CancellationToken.None);

            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public async Task Load_error_message_is_localized()
        {
            Localization.SetLocale("es");
            using var service = CreateService(new QueueHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)));

            var result = await service.LoadAsync(CancellationToken.None);

            Assert.That(result.ErrorMessage, Is.EqualTo(Localization.Get("leaderboard.error")));
        }

        [Test]
        public async Task Submit_posts_once_and_accepts_200()
        {
            var handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK));
            using var service = CreateService(handler);

            var accepted = await service.SubmitAsync(Guid.NewGuid(), 5, 100, 600, CancellationToken.None);

            Assert.That(accepted, Is.True);
            Assert.That(handler.CallCount, Is.EqualTo(1));
            Assert.That(handler.LastBody, Does.Contain("highestRound"));
        }

        [Test]
        public async Task Submit_refreshes_once_on_401()
        {
            var handler = new QueueHandler(
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new HttpResponseMessage(HttpStatusCode.OK));
            var refreshed = 0;
            using var service = CreateService(handler, _ =>
            {
                refreshed++;
                return Task.FromResult(true);
            });

            var accepted = await service.SubmitAsync(Guid.NewGuid(), 5, 100, 600, CancellationToken.None);

            Assert.That(accepted, Is.True);
            Assert.That(refreshed, Is.EqualTo(1));
            Assert.That(handler.CallCount, Is.EqualTo(2));
        }

        [Test]
        public async Task Submit_returns_false_on_conflict_or_network_error()
        {
            using var service = CreateService(new QueueHandler(new HttpResponseMessage(HttpStatusCode.Conflict)));

            Assert.That(
                await service.SubmitAsync(Guid.NewGuid(), 5, 100, 600, CancellationToken.None),
                Is.False);
        }

        private static LeaderboardService CreateService(
            HttpMessageHandler handler,
            Func<CancellationToken, Task<bool>> refresh = null)
        {
            return new LeaderboardService(
                () => "token",
                refresh ?? (_ => Task.FromResult(true)),
                () => true,
                "http://localhost",
                handler);
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string json)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        private sealed class QueueHandler : HttpMessageHandler
        {
            private readonly Queue<HttpResponseMessage> _responses;

            public QueueHandler(params HttpResponseMessage[] responses)
            {
                _responses = new Queue<HttpResponseMessage>(responses);
            }

            public int CallCount { get; private set; }
            public string LastPath { get; private set; } = string.Empty;
            public string LastBody { get; private set; } = string.Empty;

            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                CallCount++;
                LastPath = request.RequestUri?.PathAndQuery ?? string.Empty;
                LastBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync();
                return _responses.Count > 0
                    ? _responses.Dequeue()
                    : new HttpResponseMessage(HttpStatusCode.OK);
            }
        }
    }
}
