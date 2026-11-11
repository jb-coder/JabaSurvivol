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
        void Reset();
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

        /// <summary>
        /// Clears any pending conflict and revision state when the signed-in
        /// account changes, so one account can never apply another account's
        /// cloud document.
        /// </summary>
        public void Reset()
        {
            _conflict = null;
            LocalRevision = 0;
            SetState(Enabled ? CloudSyncState.SignedOut : CloudSyncState.Disabled);
        }

        public async Task<CloudSyncResult> SyncAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                return Result(CloudSyncState.Disabled, Localization.Get("cloud.disabled"));
            }

            if (_account.State != AccountState.SignedIn || string.IsNullOrWhiteSpace(_account.AccessToken))
            {
                return Result(CloudSyncState.SignedOut, Localization.Get("cloud.signedout"));
            }

            LocalRevision = profile.CloudSaveRevision;
            SetState(CloudSyncState.Syncing);

            var cloud = await _api.GetAsync(cancellationToken);
            switch (cloud.Status)
            {
                case CloudSaveApiStatus.NotFound:
                    return await UploadAsync(profile, 0, cancellationToken);
                case CloudSaveApiStatus.NetworkError:
                    return Result(CloudSyncState.Offline, Localization.Get("cloud.offline"));
                case CloudSaveApiStatus.Unauthorized:
                    return Result(CloudSyncState.SignedOut, Localization.Get("cloud.signedout"));
                case CloudSaveApiStatus.Success when cloud.Save is null:
                    return Result(CloudSyncState.Offline, Localization.Get("cloud.offline"));
                case CloudSaveApiStatus.Success:
                    if (cloud.Save.revision == profile.CloudSaveRevision)
                    {
                        return await UploadAsync(profile, cloud.Save.revision, cancellationToken);
                    }

                    return SetConflict(cloud.Save);
                case CloudSaveApiStatus.Conflict:
                case CloudSaveApiStatus.Error:
                default:
                    return Result(CloudSyncState.Offline, Localization.Get("cloud.offline"));
            }
        }

        public async Task<CloudSyncResult> BackupAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                return Result(CloudSyncState.Disabled, Localization.Get("cloud.disabled"));
            }

            if (_account.State != AccountState.SignedIn || string.IsNullOrWhiteSpace(_account.AccessToken))
            {
                return Result(CloudSyncState.SignedOut, Localization.Get("cloud.signedout"));
            }

            LocalRevision = profile.CloudSaveRevision;
            SetState(CloudSyncState.Syncing);
            return await UploadAsync(profile, profile.CloudSaveRevision, cancellationToken);
        }

        public async Task<CloudSyncResult> RestoreAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (!Enabled)
            {
                return Result(CloudSyncState.Disabled, Localization.Get("cloud.disabled"));
            }

            if (_account.State != AccountState.SignedIn || string.IsNullOrWhiteSpace(_account.AccessToken))
            {
                return Result(CloudSyncState.SignedOut, Localization.Get("cloud.signedout"));
            }

            SetState(CloudSyncState.Syncing);
            var cloud = await _api.GetAsync(cancellationToken);
            return cloud.Status switch
            {
                CloudSaveApiStatus.Success when cloud.Save?.document is not null => ApplyRestore(cloud.Save),
                CloudSaveApiStatus.NotFound => Result(CloudSyncState.Idle, Localization.Get("cloud.msg.nosave")),
                CloudSaveApiStatus.NetworkError => Result(CloudSyncState.Offline, Localization.Get("cloud.offline")),
                CloudSaveApiStatus.Unauthorized => Result(CloudSyncState.SignedOut, Localization.Get("cloud.signedout")),
                _ => Result(CloudSyncState.Offline, Localization.Get("cloud.msg.restoreFailed"))
            };
        }

        public async Task<CloudSyncResult> UseCloudAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (_conflict is null)
            {
                return Result(CloudSyncState.Offline, Localization.Get("cloud.msg.noconflict"));
            }

            if (_conflict.document is null)
            {
                var fetched = await _api.GetAsync(cancellationToken);
                if (fetched.Status != CloudSaveApiStatus.Success || fetched.Save?.document is null)
                {
                    return Result(CloudSyncState.Offline, Localization.Get("cloud.msg.loadFailed"));
                }

                _conflict = fetched.Save;
            }

            return ApplyRestore(_conflict);
        }

        public async Task<CloudSyncResult> KeepLocalAsync(Profile profile, CancellationToken cancellationToken)
        {
            if (_conflict is null)
            {
                return Result(CloudSyncState.Offline, Localization.Get("cloud.msg.noconflict"));
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
                        if (!TryPersist(profile))
                        {
                            return Result(CloudSyncState.Offline, Localization.Get("cloud.msg.backupFailed"));
                        }

                        LocalRevision = result.Save.revision;
                        _conflict = null;
                        return Result(CloudSyncState.Synced, Localization.Get("cloud.msg.backup"));
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
                        return Result(CloudSyncState.SignedOut, Localization.Get("cloud.signedout"));
                    case CloudSaveApiStatus.NetworkError when attempt < 2:
                        await Task.Delay(_retryDelay(attempt), cancellationToken);
                        break;
                    case CloudSaveApiStatus.NetworkError:
                        return Result(CloudSyncState.Offline, Localization.Get("cloud.offline"));
                    default:
                        return Result(CloudSyncState.Offline, Localization.Get("cloud.msg.backupFailed"));
                }
            }

            return Result(CloudSyncState.Offline, Localization.Get("cloud.offline"));
        }

        private CloudSyncResult ApplyRestore(CloudSaveDto cloud)
        {
            var restored = cloud.document;
            restored.Validate();
            restored.CloudSaveRevision = cloud.revision;
            if (!TryPersist(restored))
            {
                return Result(CloudSyncState.Offline, Localization.Get("cloud.msg.restoreFailed"));
            }

            LocalRevision = cloud.revision;
            _conflict = null;
            SetState(CloudSyncState.Synced);
            return new CloudSyncResult
            {
                State = CloudSyncState.Synced,
                Message = Localization.Get("cloud.msg.restored"),
                RestoredProfile = restored
            };
        }

        private bool TryPersist(Profile profile)
        {
            try
            {
                _store.Save(profile);
                return true;
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogWarning($"Cloud save persistence failed: {exception.GetType().Name}");
                return false;
            }
        }

        private CloudSyncResult SetConflict(CloudSaveDto cloud)
        {
            _conflict = cloud;
            SetState(CloudSyncState.Conflict);
            return new CloudSyncResult
            {
                State = CloudSyncState.Conflict,
                Conflict = true,
                Message = Localization.Get("cloud.conflict")
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
