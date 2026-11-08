using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.PlayMode
{
    public sealed class AccountUiPlayModeTests
    {
        private string _directory;
        private GameBootstrap _bootstrap;
        private FakeAccountService _account;

        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "jabasurvivol-account-ui", Guid.NewGuid().ToString("N"));
            GameBootstrap.SavePathOverride = Path.Combine(_directory, ProfileStore.FileName);
            _account = new FakeAccountService();
            GameBootstrap.AccountServiceFactoryOverride = () => _account;
        }

        [TearDown]
        public void TearDown()
        {
            GameBootstrap.SavePathOverride = null;
            GameBootstrap.AccountServiceFactoryOverride = null;

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
        public IEnumerator Account_panel_reflects_signed_out_and_signed_in_states()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("NOT SIGNED IN"));
            _bootstrap.Session.ShowAccountForTests();
            Assert.That(_bootstrap.Session.AccountPanelVisibleForTests, Is.True);

            _account.SetState(AccountState.SignedIn, "Tester");
            yield return null;

            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("SIGNED IN AS Tester"));

            _account.SetState(AccountState.Offline, string.Empty);
            yield return null;

            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("OFFLINE"));
        }

        [UnityTest]
        public IEnumerator Login_success_updates_the_account_ui_and_logout_returns_to_signed_out()
        {
            _bootstrap = CreateBootstrap();
            yield return null;
            _bootstrap.Session.ShowAccountForTests();

            _bootstrap.Session.SubmitLoginForTests("Tester", "Password1");
            yield return null;

            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("SIGNED IN AS Tester"));
            Assert.That(_bootstrap.Session.AccountStatusForTests, Is.Not.Empty);

            _bootstrap.Session.LogoutForTests();
            yield return null;

            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("NOT SIGNED IN"));
        }

        [UnityTest]
        public IEnumerator Refresh_failure_shows_offline_state()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            _account.SetState(AccountState.Offline, string.Empty);
            yield return null;

            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("OFFLINE"));
            Assert.That(_bootstrap.Session.AccountServiceForTests.State, Is.EqualTo(AccountState.Offline));
        }
        [UnityTest]
        public IEnumerator Refresh_network_failure_through_account_service_shows_offline()
        {
            var store = new InMemorySecureTokenStore();
            store.Save("refresh-token");
            var service = new AccountService(new FailingRefreshAuthApiClient(), store);
            GameBootstrap.AccountServiceFactoryOverride = () => service;

            _bootstrap = CreateBootstrap();
            yield return null;

            Assert.That(service.State, Is.EqualTo(AccountState.Offline));
            Assert.That(_bootstrap.Session.MainMenuAccountStateForTests, Does.Contain("OFFLINE"));
        }
        [UnityTest]
        public IEnumerator Starting_a_run_without_an_account_keeps_local_play_available()
        {
            _bootstrap = CreateBootstrap();
            yield return null;

            _bootstrap.Session.StartRun();
            yield return null;

            Assert.That(_bootstrap.Session.State, Is.EqualTo(GameSessionState.Playing));
            Assert.That(_account.State, Is.EqualTo(AccountState.SignedOut));
        }

        private GameBootstrap CreateBootstrap()
        {
            var bootstrapObject = new GameObject("GameBootstrap");
            return bootstrapObject.AddComponent<GameBootstrap>();
        }

        private sealed class FailingRefreshAuthApiClient : IAuthApiClient
        {
            public Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken) =>
                Task.FromResult(AuthResult.Failure(0, "network_error", "Unavailable.", true));

            public Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken) =>
                Task.FromResult(AuthResult.Failure(0, "network_error", "Unavailable.", true));

            public Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken) =>
                Task.FromResult(AuthResult.Failure(0, "network_error", "Unavailable.", true));

            public Task<AuthResult> LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken) =>
                Task.FromResult(AuthResult.Failure(0, "network_error", "Unavailable.", true));
        }
        private sealed class FakeAccountService : IAccountService
        {
            public AccountState State { get; private set; } = AccountState.SignedOut;
            public string Username { get; private set; } = string.Empty;
            public Guid? AccountId { get; private set; }
            public string AccessToken => State == AccountState.SignedIn ? "test-access-token" : null;
            public event Action<AccountState> StateChanged;

            public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

            public Task<AccountActionResult> RegisterAsync(
                string username,
                string password,
                CancellationToken cancellationToken)
            {
                return LoginAsync(username, password, cancellationToken);
            }

            public Task<AccountActionResult> LoginAsync(
                string username,
                string password,
                CancellationToken cancellationToken)
            {
                SetState(AccountState.SignedIn, username);
                return Task.FromResult(new AccountActionResult
                {
                    Success = true,
                    State = State,
                    Message = "WELCOME " + username
                });
            }

            public Task LogoutAsync(CancellationToken cancellationToken)
            {
                SetState(AccountState.SignedOut, string.Empty);
                return Task.CompletedTask;
            }

            public Task<bool> RefreshAsync(CancellationToken cancellationToken) => Task.FromResult(false);

            public void SetState(AccountState state, string username)
            {
                State = state;
                Username = username;
                AccountId = state == AccountState.SignedIn ? Guid.NewGuid() : null;
                StateChanged?.Invoke(state);
            }
        }
    }
}
