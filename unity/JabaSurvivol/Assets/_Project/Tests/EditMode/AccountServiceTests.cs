using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using JabaSurvivol.Gameplay;
using NUnit.Framework;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class AccountServiceTests
    {
        private FakeAuthApiClient _api;
        private InMemorySecureTokenStore _store;
        private AccountService _service;

        [SetUp]
        public void SetUp()
        {
            _api = new FakeAuthApiClient();
            _store = new InMemorySecureTokenStore();
            _service = new AccountService(_api, _store);
        }

        [TearDown]
        public void TearDown()
        {
            _service.Dispose();
        }

        [Test]
        public async Task Initialize_without_a_token_stays_signed_out()
        {
            await _service.InitializeAsync(CancellationToken.None);

            Assert.That(_service.State, Is.EqualTo(AccountState.SignedOut));
            Assert.That(_api.RefreshCalls, Is.EqualTo(0));
        }

        [Test]
        public async Task Login_persists_only_the_refresh_token()
        {
            _api.LoginResult = AuthResult.FromTokens(Tokens("access-1", "refresh-1", "Tester"));

            var result = await _service.LoginAsync("Tester", "Password1", CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(_service.State, Is.EqualTo(AccountState.SignedIn));
            Assert.That(_service.AccessToken, Is.EqualTo("access-1"));
            Assert.That(_service.Username, Is.EqualTo("Tester"));
            Assert.That(_store.Load(), Is.EqualTo("refresh-1"));
        }

        [Test]
        public async Task Initialize_rotates_a_persisted_refresh_token()
        {
            _store.Save("refresh-old");
            _api.RefreshResult = AuthResult.FromTokens(Tokens("access-new", "refresh-new", "Tester"));

            await _service.InitializeAsync(CancellationToken.None);

            Assert.That(_service.State, Is.EqualTo(AccountState.SignedIn));
            Assert.That(_service.AccessToken, Is.EqualTo("access-new"));
            Assert.That(_store.Load(), Is.EqualTo("refresh-new"));
        }

        [Test]
        public async Task Rejected_refresh_clears_the_session()
        {
            _store.Save("refresh-old");
            _api.RefreshResult = AuthResult.Failure(401, "unauthorized", "Invalid token.");

            await _service.InitializeAsync(CancellationToken.None);

            Assert.That(_service.State, Is.EqualTo(AccountState.SignedOut));
            Assert.That(_store.Load(), Is.Null);
        }

        [Test]
        public async Task Network_failure_keeps_the_refresh_token_and_reports_offline()
        {
            _store.Save("refresh-old");
            _api.RefreshResult = AuthResult.Failure(0, "network_error", "Unavailable.", networkError: true);

            await _service.InitializeAsync(CancellationToken.None);

            Assert.That(_service.State, Is.EqualTo(AccountState.Offline));
            Assert.That(_store.Load(), Is.EqualTo("refresh-old"));
        }

        [Test]
        public async Task Logout_clears_local_credentials_even_when_the_server_fails()
        {
            _api.LoginResult = AuthResult.FromTokens(Tokens("access-1", "refresh-1", "Tester"));
            await _service.LoginAsync("Tester", "Password1", CancellationToken.None);
            _api.LogoutResult = AuthResult.Failure(0, "network_error", "Unavailable.", networkError: true);

            await _service.LogoutAsync(CancellationToken.None);

            Assert.That(_service.State, Is.EqualTo(AccountState.SignedOut));
            Assert.That(_service.AccessToken, Is.Null);
            Assert.That(_store.Load(), Is.Null);
        }

        [Test]
        public async Task Register_signs_in_after_creating_the_account()
        {
            _api.RegisterResult = AuthResult.FromAccount(new AccountResponseDto
            {
                accountId = Guid.NewGuid().ToString(),
                username = "NewPlayer"
            });
            _api.LoginResult = AuthResult.FromTokens(Tokens("access-1", "refresh-1", "NewPlayer"));

            var result = await _service.RegisterAsync("NewPlayer", "Password1", CancellationToken.None);

            Assert.That(result.Success, Is.True);
            Assert.That(_service.State, Is.EqualTo(AccountState.SignedIn));
            Assert.That(_service.Username, Is.EqualTo("NewPlayer"));
        }

        [Test]
        public void Secure_store_encrypts_the_token_on_windows()
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "jabasurvivol-account",
                Guid.NewGuid().ToString("N"));
            var path = System.IO.Path.Combine(directory, "session.bin");
            try
            {
                var store = new WindowsDpapiTokenStore(path);
                store.Save("refresh-secret-value");

                var fileText = System.IO.File.ReadAllText(path);
                Assert.That(fileText, Does.Not.Contain("refresh-secret-value"));
                Assert.That(store.Load(), Is.EqualTo("refresh-secret-value"));

                store.Clear();
                Assert.That(store.Load(), Is.Null);
            }
            finally
            {
                if (System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.Delete(directory, true);
                }
            }
        }

        [Test]
        public async Task Authenticated_client_retries_once_after_refresh_on_401()
        {
            var handler = new SequenceHandler(
                new HttpResponseMessage(HttpStatusCode.Unauthorized),
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"ok\":true}")
                });
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var accessToken = "old-token";
            var refreshCalls = 0;
            var client = new AuthenticatedApiClient(
                httpClient,
                () => accessToken,
                _ =>
                {
                    refreshCalls++;
                    accessToken = "new-token";
                    return Task.FromResult(true);
                });

            var response = await client.SendAsync(HttpMethod.Get, "api/v1/game/saves/primary", null, CancellationToken.None);

            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(refreshCalls, Is.EqualTo(1));
            Assert.That(handler.Authorizations, Is.EqualTo(new[] { "Bearer old-token", "Bearer new-token" }));
        }

        [Test]
        public async Task Authenticated_client_does_not_retry_when_refresh_fails()
        {
            var handler = new SequenceHandler(new HttpResponseMessage(HttpStatusCode.Unauthorized));
            using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var client = new AuthenticatedApiClient(
                httpClient,
                () => "access",
                _ => Task.FromResult(false));

            var response = await client.SendAsync(HttpMethod.Get, "api/v1/game/saves/primary", null, CancellationToken.None);

            Assert.That(response.StatusCode, Is.EqualTo(401));
            Assert.That(handler.CallCount, Is.EqualTo(1));
        }

        private static TokenResponseDto Tokens(string access, string refresh, string username)
        {
            return new TokenResponseDto
            {
                accessToken = access,
                expiresInSeconds = 900,
                refreshToken = refresh,
                refreshExpiresInSeconds = 2_592_000,
                accountId = Guid.NewGuid().ToString(),
                username = username
            };
        }

        private sealed class FakeAuthApiClient : IAuthApiClient
        {
            public AuthResult RegisterResult { get; set; } = AuthResult.Failure(500, "unexpected", "Unexpected.");
            public AuthResult LoginResult { get; set; } = AuthResult.Failure(500, "unexpected", "Unexpected.");
            public AuthResult RefreshResult { get; set; } = AuthResult.Failure(500, "unexpected", "Unexpected.");
            public AuthResult LogoutResult { get; set; } = AuthResult.Failure(500, "unexpected", "Unexpected.");
            public int RefreshCalls { get; private set; }

            public Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken) =>
                Task.FromResult(RegisterResult);

            public Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken) =>
                Task.FromResult(LoginResult);

            public Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
            {
                RefreshCalls++;
                return Task.FromResult(RefreshResult);
            }

            public Task<AuthResult> LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken) =>
                Task.FromResult(LogoutResult);
        }

        private sealed class SequenceHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage[] _responses;
            private int _index;

            public SequenceHandler(params HttpResponseMessage[] responses)
            {
                _responses = responses;
            }

            public List<string> Authorizations { get; } = new List<string>();
            public int CallCount => _index;

            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                Authorizations.Add(request.Headers.Authorization?.ToString() ?? string.Empty);
                var response = _responses[Math.Min(_index, _responses.Length - 1)];
                _index++;
                return Task.FromResult(response);
            }
        }
    }
}
