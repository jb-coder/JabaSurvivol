using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class CloudSavePlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;
        private FakeCloudSaveService _cloud;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-cloud-ui", Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            GameBootstrap.AccountServiceFactoryOverride = () => new FakeAccountService();
            _cloud = new FakeCloudSaveService();
            GameBootstrap.CloudSaveServiceFactoryOverride = _ => _cloud;
            GameBootstrap.RemoteConfigOverride = () => new RemoteConfigDto
            {
                version = 1,
                cloudSavesEnabled = true,
                leaderboardsEnabled = true,
                telemetryEnabled = false
            };
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;
            GameBootstrap.AccountServiceFactoryOverride = null;
            GameBootstrap.CloudSaveServiceFactoryOverride = null;
            GameBootstrap.RemoteConfigOverride = null;

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
        public IEnumerator Conflict_opens_the_explicit_choice_dialog()
        {
            _cloud.SyncResult = new CloudSyncResult
            {
                State = CloudSyncState.Conflict,
                Conflict = true,
                Message = string.Empty
            };

            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            Assert.That(_bootstrap.Session.CloudConflictVisibleForTests, Is.True);
            Assert.That(_bootstrap.Session.CloudStatusForTests, Does.Contain("CONFLICT"));
        }

        [UnityTest]
        public IEnumerator Offline_sync_keeps_local_play_and_reports_offline()
        {
            _cloud.SyncResult = new CloudSyncResult
            {
                State = CloudSyncState.Offline,
                Message = string.Empty
            };

            _bootstrap = CreateBootstrap();
            yield return null;
            yield return null;

            Assert.That(_bootstrap.Session.CloudConflictVisibleForTests, Is.False);
            Assert.That(_bootstrap.Session.CloudStatusForTests, Does.Contain("OFFLINE"));
            Assert.That(_bootstrap.Session.State, Is.EqualTo(GameSessionState.MainMenu));
        }

        [UnityTest]
        public IEnumerator Restore_replaces_the_local_profile_atomically()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            _cloud.RestoreResult = new CloudSyncResult
            {
                State = CloudSyncState.Synced,
                Message = "CLOUD SAVE RESTORED",
                RestoredProfile = new Profile { Supplies = 777, Level = 12, CloudSaveRevision = 4 }
            };

            _bootstrap.Session.RestoreCloudForTests();
            yield return null;

            Assert.That(_bootstrap.Session.Profile.Supplies, Is.EqualTo(777));
            Assert.That(_bootstrap.Session.Profile.Level, Is.EqualTo(12));
            var stored = new ProfileStore(GameBootstrap.SavePathOverride).Load();
            Assert.That(stored.Supplies, Is.EqualTo(777));
            Assert.That(stored.CloudSaveRevision, Is.EqualTo(4));
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

        private sealed class FakeCloudSaveService : ICloudSaveService
        {
            public CloudSyncState State { get; private set; } = CloudSyncState.Idle;
            public bool Enabled { get; private set; } = true;
            public long LocalRevision => 0;
            public long CloudRevision => 3;
            public string CloudUpdatedAt => "2026-11-08T12:00:00Z";
            public event Action<CloudSyncState> StateChanged;

            public CloudSyncResult SyncResult { get; set; } = new CloudSyncResult { State = CloudSyncState.Synced };
            public CloudSyncResult BackupResult { get; set; } = new CloudSyncResult { State = CloudSyncState.Synced, Message = "BACKUP COMPLETE" };
            public CloudSyncResult RestoreResult { get; set; } = new CloudSyncResult { State = CloudSyncState.Synced };
            public CloudSyncResult UseCloudResult { get; set; } = new CloudSyncResult { State = CloudSyncState.Synced };
            public CloudSyncResult KeepLocalResult { get; set; } = new CloudSyncResult { State = CloudSyncState.Synced };

            public void Configure(bool enabled) => Enabled = enabled;

            public Task<CloudSyncResult> SyncAsync(Profile profile, CancellationToken cancellationToken) =>
                Complete(SyncResult);

            public Task<CloudSyncResult> BackupAsync(Profile profile, CancellationToken cancellationToken) =>
                Complete(BackupResult);

            public Task<CloudSyncResult> RestoreAsync(Profile profile, CancellationToken cancellationToken) =>
                Complete(RestoreResult);

            public Task<CloudSyncResult> UseCloudAsync(Profile profile, CancellationToken cancellationToken) =>
                Complete(UseCloudResult);

            public Task<CloudSyncResult> KeepLocalAsync(Profile profile, CancellationToken cancellationToken) =>
                Complete(KeepLocalResult);

            private Task<CloudSyncResult> Complete(CloudSyncResult result)
            {
                State = result.State;
                StateChanged?.Invoke(result.State);
                return Task.FromResult(result);
            }
        }
    }
}
