using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class LeaderboardTelemetryPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;
        private FakeLeaderboardService _leaderboard;
        private TelemetryService _telemetry;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-lb-ui", Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            GameBootstrap.AccountServiceFactoryOverride = () => new FakeAccountService();
            _leaderboard = new FakeLeaderboardService();
            GameBootstrap.LeaderboardServiceFactoryOverride = _ => _leaderboard;
            _telemetry = new TelemetryService(
                () => "token",
                _ => Task.FromResult(true),
                _ => TimeSpan.Zero,
                _ => true,
                "http://localhost",
                new OkHandler());
            GameBootstrap.TelemetryServiceFactoryOverride = _ => _telemetry;
            GameBootstrap.RemoteConfigOverride = () => new RemoteConfigDto
            {
                version = 1,
                cloudSavesEnabled = false,
                leaderboardsEnabled = true,
                telemetryEnabled = true,
                telemetrySampleRate = 1d
            };
            PlayerPrefs.DeleteKey("telemetry.optIn");
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;
            GameBootstrap.AccountServiceFactoryOverride = null;
            GameBootstrap.LeaderboardServiceFactoryOverride = null;
            GameBootstrap.TelemetryServiceFactoryOverride = null;
            GameBootstrap.RemoteConfigOverride = null;
            PlayerPrefs.DeleteKey("telemetry.optIn");
            PlayerPrefs.Save();

            if (_bootstrap != null)
            {
                UnityEngine.Object.DestroyImmediate(_bootstrap.gameObject);
                _bootstrap = null;
            }

            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [UnityTest]
        public IEnumerator Leaderboard_empty_state_is_shown()
        {
            _leaderboard.LoadResult = new LeaderboardResult { Success = true, Empty = true };
            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            _bootstrap.Session.ShowLeaderboardForTests();
            yield return null;

            Assert.That(_bootstrap.Session.LeaderboardVisibleForTests, Is.True);
            Assert.That(_bootstrap.Session.LeaderboardStatusForTests, Does.Contain("NO RUNS YET"));
        }

        [UnityTest]
        public IEnumerator Leaderboard_populated_board_renders_rank_and_stats()
        {
            var entries = new LeaderboardEntryDto[50];
            for (var index = 0; index < entries.Length; index++)
            {
                entries[index] = new LeaderboardEntryDto
                {
                    rank = index + 1,
                    username = $"Player{index + 1:00}",
                    highestRound = 10 - (index % 10),
                    totalKills = 1000 - index,
                    durationSeconds = 100 + index,
                    achievedAt = "2026-11-08T12:00:00Z"
                };
            }

            _leaderboard.LoadResult = new LeaderboardResult { Success = true, Entries = entries };
            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            _bootstrap.Session.ShowLeaderboardForTests();
            yield return null;

            var text = _bootstrap.Session.LeaderboardTextForTests;
            Assert.That(text, Does.Contain("1. Player01"));
            Assert.That(text, Does.Contain("50. Player50"));
            Assert.That(text, Does.Contain("R10"));
            Assert.That(text, Does.Contain("K1000"));
            Assert.That(text, Does.Contain("100s"));
        }

        [UnityTest]
        public IEnumerator Telemetry_queue_retries_in_play_mode()
        {
            var handler = new SequenceHandler(
                HttpStatusCode.InternalServerError,
                HttpStatusCode.InternalServerError,
                HttpStatusCode.OK);
            _telemetry.Dispose();
            _telemetry = new TelemetryService(
                () => "token",
                _ => Task.FromResult(true),
                _ => TimeSpan.Zero,
                _ => true,
                "http://localhost",
                handler);
            GameBootstrap.TelemetryServiceFactoryOverride = _ => _telemetry;
            _telemetry.Configure(true, 1d);
            _telemetry.SetOptIn(true);
            _telemetry.Enqueue("run_start", "{\"startingRound\":1}");

            var flushTask = _telemetry.FlushAsync(CancellationToken.None);
            while (!flushTask.IsCompleted)
            {
                yield return null;
            }

            var flushed = flushTask.Result;

            Assert.That(flushed, Is.True);
            Assert.That(_telemetry.PendingCount, Is.EqualTo(0));
            Assert.That(handler.CallCount, Is.EqualTo(3));
        }
        [UnityTest]
        public IEnumerator Leaderboard_disabled_flag_shows_disabled_state()
        {
            GameBootstrap.RemoteConfigOverride = () => new RemoteConfigDto
            {
                version = 1,
                cloudSavesEnabled = false,
                leaderboardsEnabled = false,
                telemetryEnabled = false
            };
            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            _bootstrap.Session.ShowLeaderboardForTests();
            yield return null;

            Assert.That(_bootstrap.Session.LeaderboardStatusForTests, Does.Contain("DISABLED"));
        }

        [UnityTest]
        public IEnumerator Leaderboard_submission_failure_does_not_block_death_or_progress()
        {
            _leaderboard.SubmitResult = false;
            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;
            var session = _bootstrap.Session;
            session.StartRun();
            session.ApplyDamage(100000f);
            yield return null;
            yield return null;

            Assert.That(session.State, Is.EqualTo(GameSessionState.GameOver));
            Assert.That(_leaderboard.SubmitCalls, Is.EqualTo(1));
            Assert.That(session.Profile.TotalRuns, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Telemetry_opt_in_persists_and_toggles()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            Assert.That(_bootstrap.Session.TelemetryVisibleForTests, Is.True);
            Assert.That(_bootstrap.Session.TelemetryOptedInForTests, Is.False);

            _bootstrap.Session.ToggleTelemetryForTests();
            yield return null;

            Assert.That(_bootstrap.Session.TelemetryOptedInForTests, Is.True);
            Assert.That(PlayerPrefs.GetInt("telemetry.optIn", 0), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Telemetry_disabled_flag_hides_the_toggle()
        {
            GameBootstrap.RemoteConfigOverride = () => new RemoteConfigDto
            {
                version = 1,
                cloudSavesEnabled = false,
                leaderboardsEnabled = true,
                telemetryEnabled = false
            };
            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            Assert.That(_bootstrap.Session.TelemetryVisibleForTests, Is.False);
        }

        private GameBootstrap CreateBootstrap()
        {
            var bootstrapObject = new GameObject("GameBootstrap");
            return bootstrapObject.AddComponent<GameBootstrap>();
        }

        private sealed class FakeAccountService : IAccountService
        {
            public AccountState State => AccountState.SignedIn;
            public string Username => "Tester";
            public Guid? AccountId => Guid.NewGuid();
            public string AccessToken => "token";
            public event Action<AccountState> StateChanged;

            public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task<AccountActionResult> RegisterAsync(string username, string password, CancellationToken cancellationToken) =>
                Task.FromResult(new AccountActionResult { Success = true, State = State });
            public Task<AccountActionResult> LoginAsync(string username, string password, CancellationToken cancellationToken) =>
                Task.FromResult(new AccountActionResult { Success = true, State = State });
            public Task LogoutAsync(CancellationToken cancellationToken) => Task.CompletedTask;
            public Task<bool> RefreshAsync(CancellationToken cancellationToken) => Task.FromResult(true);
        }

        private sealed class FakeLeaderboardService : ILeaderboardService
        {
            public LeaderboardResult LoadResult { get; set; } = new LeaderboardResult { Success = true, Empty = true };
            public bool SubmitResult { get; set; } = true;
            public int SubmitCalls { get; private set; }

            public Task<LeaderboardResult> LoadAsync(CancellationToken cancellationToken) =>
                Task.FromResult(LoadResult);

            public Task<bool> SubmitAsync(
                Guid runId,
                int highestRound,
                int totalKills,
                int durationSeconds,
                CancellationToken cancellationToken)
            {
                SubmitCalls++;
                return Task.FromResult(SubmitResult);
            }
        }

        private sealed class SequenceHandler : HttpMessageHandler
        {
            private readonly Queue<HttpStatusCode> _statuses;

            public SequenceHandler(params HttpStatusCode[] statuses)
            {
                _statuses = new Queue<HttpStatusCode>(statuses);
            }

            public int CallCount { get; private set; }

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                CallCount++;
                var status = _statuses.Count > 0 ? _statuses.Dequeue() : HttpStatusCode.OK;
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            }
        }
        private sealed class OkHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });
            }
        }
    }
}
