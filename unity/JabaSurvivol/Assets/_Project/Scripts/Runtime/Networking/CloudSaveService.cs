using System;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Core;

namespace JabaSurvivol.Gameplay
{
    public enum CloudSyncState
    {
        Disabled,
        SignedOut,
        Idle,
        Syncing,
        Synced,
        Conflict,
        Offline
    }

    public sealed class CloudSyncResult
    {
        public CloudSyncState State { get; set; }
        public string Message { get; set; } = string.Empty;
        public Profile RestoredProfile { get; set; }
        public bool Conflict { get; set; }
    }

    public interface ICloudSaveService
    {
        CloudSyncState State { get; }
        bool Enabled { get; }
        long LocalRevision { get; }
        long CloudRevision { get; }
        string CloudUpdatedAt { get; }
        event Action<CloudSyncState> StateChanged;

        void Configure(bool enabled);
        Task<CloudSyncResult> SyncAsync(Profile profile, CancellationToken cancellationToken);
        Task<CloudSyncResult> BackupAsync(Profile profile, CancellationToken cancellationToken);
        Task<CloudSyncResult> RestoreAsync(Profile profile, CancellationToken cancellationToken);
        Task<CloudSyncResult> UseCloudAsync(Profile profile, CancellationToken cancellationToken);
        Task<CloudSyncResult> KeepLocalAsync(Profile profile, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Backup and restore for the single primary cloud slot. Local progression
    /// remains authoritative until the player explicitly restores the cloud
    /// copy. Failed requests never delete or overwrite the local profile.
    /// </summary>
    public sealed class CloudSaveService : ICloudSaveService
    {
        private readonly ICloudSaveApiClient _api;
        private readonly ProfileStore _store;
        private readonly IAccountService _account;
        private readonly Func<int, TimeSpan> _retryDelay;
        private CloudSaveDto _conflict;

        public CloudSaveService(
            ICloudSaveApiClient api,
            ProfileStore store,
            IAccountService account,
            Func<int, TimeSpan> retryDelay = null)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _account = account ?? throw new ArgumentNullException(nameof(account));
            _retryDelay = retryDelay ?? (attempt => TimeSpan.FromSeconds(1 << attempt));
        }

        public CloudSyncState State { get; private set; } = CloudSyncState.SignedOut;

        public bool Enabled { get; private set; } = true;

        public long LocalRevision { get; private set; }

        public long CloudRevision => _conflict?.revision ?? LocalRevision;

        public string CloudUpdatedAt => _conflict?.updatedAt ?? string.Empty;

        public event Action<CloudSyncState> StateChanged;

        public void Configure(bool enabled)
        {
            Enabled = enabled;
            if (!enabled)
            {
                SetState(CloudSyncState.Disabled);
            }
        }

        public async Task<CloudSyncResult> SyncAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                return Result(CloudSyncState.Disabled, "CLOUD SAVES DISABLED");
            }

            if (_account.State != AccountState.SignedIn || string.IsNullOrWhiteSpace(_account.AccessToken))
            {
                return Result(CloudSyncState.SignedOut, "SIGN IN TO BACK UP");
            }

            LocalRevision = profile.CloudSaveRevision;
            SetState(CloudSyncState.Syncing);

            var cloud = await _api.GetAsync(cancellationToken);
            switch (cloud.Status)
            {
                case CloudSaveApiStatus.NotFound:
                    return await UploadAsync(profile, 0, cancellationToken);
                case CloudSaveApiStatus.NetworkError:
                    return Result(CloudSyncState.Offline, "CLOUD UNAVAILABLE - LOCAL SAVE KEPT");
                case CloudSaveApiStatus.Unauthorized:
                    return Result(CloudSyncState.SignedOut, "SIGN IN TO BACK UP");
                case CloudSaveApiStatus.Success when cloud.Save is null:
                    return Result(CloudSyncState.Offline, "CLOUD UNAVAILABLE - LOCAL SAVE KEPT");
                case CloudSaveApiStatus.Success:
                    if (cloud.Save.revision == profile.CloudSaveRevision)
                    {
                        return await UploadAsync(profile, cloud.Save.revision, cancellationToken);
                    }

                    return SetConflict(cloud.Save);
                case CloudSaveApiStatus.Conflict:
                case CloudSaveApiStatus.Error:
                default:
                    return Result(CloudSyncState.Offline, "CLOUD UNAVAILABLE - LOCAL SAVE KEPT");
            }
        }

        public async Task<CloudSyncResult> BackupAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                return Result(CloudSyncState.Disabled, "CLOUD SAVES DISABLED");
            }

            if (_account.State != AccountState.SignedIn || string.IsNullOrWhiteSpace(_account.AccessToken))
            {
                return Result(CloudSyncState.SignedOut, "SIGN IN TO BACK UP");
            }

            LocalRevision = profile.CloudSaveRevision;
            SetState(CloudSyncState.Syncing);
            return await UploadAsync(profile, profile.CloudSaveRevision, cancellationToken);
        }

        public async Task<CloudSyncResult> RestoreAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                return Result(CloudSyncState.Disabled, "CLOUD SAVES DISABLED");
            }

            if (_account.State != AccountState.SignedIn || string.IsNullOrWhiteSpace(_account.AccessToken))
            {
                return Result(CloudSyncState.SignedOut, "SIGN IN TO RESTORE");
            }

            SetState(CloudSyncState.Syncing);
            var cloud = await _api.GetAsync(cancellationToken);
            return cloud.Status switch
            {
                CloudSaveApiStatus.Success when cloud.Save?.document is not null => ApplyRestore(cloud.Save),
                CloudSaveApiStatus.NotFound => Result(CloudSyncState.Idle, "NO CLOUD SAVE FOUND"),
                CloudSaveApiStatus.NetworkError => Result(CloudSyncState.Offline, "CLOUD UNAVAILABLE - LOCAL SAVE KEPT"),
                CloudSaveApiStatus.Unauthorized => Result(CloudSyncState.SignedOut, "SIGN IN TO RESTORE"),
                _ => Result(CloudSyncState.Offline, "CLOUD SAVE COULD NOT BE RESTORED")
            };
        }

        public async Task<CloudSyncResult> UseCloudAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (_conflict is null)
            {
                return Result(CloudSyncState.Offline, "NO CLOUD CONFLICT TO RESOLVE");
            }

            if (_conflict.document is null)
            {
                var fetched = await _api.GetAsync(cancellationToken);
                if (fetched.Status != CloudSaveApiStatus.Success || fetched.Save?.document is null)
                {
                    return Result(CloudSyncState.Offline, "CLOUD SAVE COULD NOT BE LOADED");
                }

                _conflict = fetched.Save;
            }

            return ApplyRestore(_conflict);
        }

        public async Task<CloudSyncResult> KeepLocalAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (_conflict is null)
            {
                return Result(CloudSyncState.Offline, "NO CLOUD CONFLICT TO RESOLVE");
            }

            SetState(CloudSyncState.Syncing);
            return await UploadAsync(profile, _conflict.revision, cancellationToken);
        }

        private async Task<CloudSyncResult> UploadAsync(
            Profile profile,
            long baseRevision,
            CancellationToken cancellationToken)
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                var result = await _api.PutAsync(baseRevision, profile, cancellationToken);
                switch (result.Status)
                {
                    case CloudSaveApiStatus.Success when result.Save is not null:
                        profile.CloudSaveRevision = result.Save.revision;
                        _store.Save(profile);
                        LocalRevision = result.Save.revision;
                        _conflict = null;
                        return Result(CloudSyncState.Synced, "BACKUP COMPLETE");
                    case CloudSaveApiStatus.Conflict:
                        var current = await _api.GetAsync(cancellationToken);
                        return SetConflict(current.Status == CloudSaveApiStatus.Success && current.Save is not null
                            ? current.Save
                            : new CloudSaveDto
                            {
                                revision = result.CurrentRevision,
                                updatedAt = result.CurrentUpdatedAt
                            });
                    case CloudSaveApiStatus.Unauthorized:
                        return Result(CloudSyncState.SignedOut, "SIGN IN TO BACK UP");
                    case CloudSaveApiStatus.NetworkError when attempt < 2:
                        await Task.Delay(_retryDelay(attempt), cancellationToken);
                        break;
                    case CloudSaveApiStatus.NetworkError:
                        return Result(CloudSyncState.Offline, "CLOUD UNAVAILABLE - LOCAL SAVE KEPT");
                    default:
                        return Result(CloudSyncState.Offline, "BACKUP FAILED - LOCAL SAVE KEPT");
                }
            }

            return Result(CloudSyncState.Offline, "CLOUD UNAVAILABLE - LOCAL SAVE KEPT");
        }

        private CloudSyncResult ApplyRestore(CloudSaveDto cloud)
        {
            var restored = cloud.document;
            restored.Validate();
            restored.CloudSaveRevision = cloud.revision;
            _store.Save(restored);
            LocalRevision = cloud.revision;
            _conflict = null;
            SetState(CloudSyncState.Synced);
            return new CloudSyncResult
            {
                State = CloudSyncState.Synced,
                Message = "CLOUD SAVE RESTORED",
                RestoredProfile = restored
            };
        }

        private CloudSyncResult SetConflict(CloudSaveDto cloud)
        {
            _conflict = cloud;
            SetState(CloudSyncState.Conflict);
            return new CloudSyncResult
            {
                State = CloudSyncState.Conflict,
                Conflict = true,
                Message = "LOCAL AND CLOUD SAVES DIFFER"
            };
        }

        private CloudSyncResult Result(CloudSyncState state, string message)
        {
            SetState(state);
            return new CloudSyncResult { State = state, Message = message };
        }

        private void SetState(CloudSyncState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
