using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Core;
using JabaSurvivol.Gameplay;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class CloudSaveServiceTests
    {
        private string _directory;
        private ProfileStore _store;
        private FakeCloudSaveApi _api;
        private FakeAccountService _account;
        private CloudSaveService _service;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-cloud", Guid.NewGuid().ToString("N"));
            _store = new ProfileStore(Path.Combine(_directory, ProfileStore.FileName));
            _api = new FakeCloudSaveApi();
            _account = new FakeAccountService();
            _service = new CloudSaveService(_api, _store, _account, _ => TimeSpan.Zero);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        [Test]
        public async Task First_sync_uploads_with_revision_zero()
        {
            var profile = new Profile { Supplies = 40 };
            _api.GetResults.Enqueue(new CloudSaveApiResult { Status = CloudSaveApiStatus.NotFound });
            _api.PutResults.Enqueue(Saved(1));

            var result = await _service.SyncAsync(profile, CancellationToken.None);

            Assert.That(result.State, Is.EqualTo(CloudSyncState.Synced));
            Assert.That(_api.LastBaseRevision, Is.EqualTo(0));
            Assert.That(profile.CloudSaveRevision, Is.EqualTo(1));
            Assert.That(_store.Load().CloudSaveRevision, Is.EqualTo(1));
        }

        [Test]
        public async Task Equal_revision_uploads_local_changes()
        {
            var profile = new Profile { Supplies = 20, CloudSaveRevision = 3 };
            _api.GetResults.Enqueue(Cloud(3, new Profile { Supplies = 20 }));
            _api.PutResults.Enqueue(Saved(4));

            var result = await _service.SyncAsync(profile, CancellationToken.None);

            Assert.That(result.State, Is.EqualTo(CloudSyncState.Synced));
            Assert.That(_api.LastBaseRevision, Is.EqualTo(3));
            Assert.That(profile.CloudSaveRevision, Is.EqualTo(4));
        }

        [Test]
        public async Task Newer_cloud_revision_opens_a_conflict_without_overwriting()
        {
            var profile = new Profile { Supplies = 20, CloudSaveRevision = 2 };
            _api.GetResults.Enqueue(Cloud(5, new Profile { Supplies = 99, CloudSaveRevision = 5 }));

            var result = await _service.SyncAsync(profile, CancellationToken.None);

            Assert.That(result.State, Is.EqualTo(CloudSyncState.Conflict));
            Assert.That(result.Conflict, Is.True);
            Assert.That(profile.CloudSaveRevision, Is.EqualTo(2));
            Assert.That(_api.PutCalls, Is.EqualTo(0));
            Assert.That(_service.CloudRevision, Is.EqualTo(5));
        }

        [Test]
        public async Task Keeping_local_uploads_with_the_cloud_base_revision()
        {
            var profile = new Profile { Supplies = 20, CloudSaveRevision = 2 };
            _api.GetResults.Enqueue(Cloud(5, new Profile { Supplies = 99 }));
            await _service.SyncAsync(profile, CancellationToken.None);
            _api.PutResults.Enqueue(Saved(6));

            var result = await _service.KeepLocalAsync(profile, CancellationToken.None);

            Assert.That(result.State, Is.EqualTo(CloudSyncState.Synced));
            Assert.That(_api.LastBaseRevision, Is.EqualTo(5));
            Assert.That(profile.CloudSaveRevision, Is.EqualTo(6));
        }

        [Test]
        public async Task Using_cloud_restores_atomically_and_updates_revision()
        {
            var profile = new Profile { Supplies = 20, CloudSaveRevision = 2 };
            var cloudProfile = new Profile { Supplies = 500, Level = 7 };
            _api.GetResults.Enqueue(Cloud(5, cloudProfile));
            await _service.SyncAsync(profile, CancellationToken.None);

            var result = await _service.UseCloudAsync(profile, CancellationToken.None);

            Assert.That(result.RestoredProfile, Is.Not.Null);
            Assert.That(result.RestoredProfile.Supplies, Is.EqualTo(500));
            Assert.That(result.RestoredProfile.CloudSaveRevision, Is.EqualTo(5));
            var stored = _store.Load();
            Assert.That(stored.Supplies, Is.EqualTo(500));
            Assert.That(stored.CloudSaveRevision, Is.EqualTo(5));
        }

        [Test]
        public async Task Network_failure_retries_bounded_and_keeps_local_progress()
        {
            var profile = new Profile { Supplies = 321, CloudSaveRevision = 1 };
            _store.Save(profile);
            _api.GetResults.Enqueue(Cloud(1, new Profile { Supplies = 321 }));
            _api.PutResults.Enqueue(new CloudSaveApiResult { Status = CloudSaveApiStatus.NetworkError });
            _api.PutResults.Enqueue(new CloudSaveApiResult { Status = CloudSaveApiStatus.NetworkError });
            _api.PutResults.Enqueue(new CloudSaveApiResult { Status = CloudSaveApiStatus.NetworkError });

            var result = await _service.SyncAsync(profile, CancellationToken.None);

            Assert.That(result.State, Is.EqualTo(CloudSyncState.Offline));
            Assert.That(_api.PutCalls, Is.EqualTo(3));
            Assert.That(profile.Supplies, Is.EqualTo(321));
            Assert.That(_store.Load().Supplies, Is.EqualTo(321));
        }

        [Test]
        public async Task Disabled_cloud_saves_do_not_call_the_api()
        {
            _service.Configure(false);
            var profile = new Profile();

            var result = await _service.SyncAsync(profile, CancellationToken.None);

            Assert.That(result.State, Is.EqualTo(CloudSyncState.Disabled));
            Assert.That(_api.GetCalls, Is.EqualTo(0));
        }

        private static CloudSaveApiResult Saved(long revision)
        {
            return new CloudSaveApiResult
            {
                Status = CloudSaveApiStatus.Success,
                Save = new CloudSaveDto { revision = revision }
            };
        }

        private static CloudSaveApiResult Cloud(long revision, Profile profile)
        {
            return new CloudSaveApiResult
            {
                Status = CloudSaveApiStatus.Success,
                Save = new CloudSaveDto
                {
                    revision = revision,
                    updatedAt = "2026-11-08T12:00:00Z",
                    document = profile
                }
            };
        }

        private sealed class FakeCloudSaveApi : ICloudSaveApiClient
        {
            public Queue<CloudSaveApiResult> GetResults { get; } = new Queue<CloudSaveApiResult>();
            public Queue<CloudSaveApiResult> PutResults { get; } = new Queue<CloudSaveApiResult>();
            public int GetCalls { get; private set; }
            public int PutCalls { get; private set; }
            public long LastBaseRevision { get; private set; }

            public Task<CloudSaveApiResult> GetAsync(CancellationToken cancellationToken)
            {
                GetCalls++;
                return Task.FromResult(GetResults.Count > 0
                    ? GetResults.Dequeue()
                    : new CloudSaveApiResult { Status = CloudSaveApiStatus.NotFound });
            }

            public Task<CloudSaveApiResult> PutAsync(
                long baseRevision,
                Profile profile,
                CancellationToken cancellationToken)
            {
                PutCalls++;
                LastBaseRevision = baseRevision;
                return Task.FromResult(PutResults.Count > 0
                    ? PutResults.Dequeue()
                    : new CloudSaveApiResult { Status = CloudSaveApiStatus.NetworkError });
            }
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
    }
}
