using System;
using System.Threading;
using System.Threading.Tasks;

namespace JabaSurvivol.Gameplay
{
    public interface IAccountService
    {
        AccountState State { get; }
        string Username { get; }
        Guid? AccountId { get; }
        string AccessToken { get; }
        event Action<AccountState> StateChanged;

        Task InitializeAsync(CancellationToken cancellationToken);
        Task<AccountActionResult> RegisterAsync(string username, string password, CancellationToken cancellationToken);
        Task<AccountActionResult> LoginAsync(string username, string password, CancellationToken cancellationToken);
        Task LogoutAsync(CancellationToken cancellationToken);
        Task<bool> RefreshAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Owns the account session. Access tokens stay in memory; refresh tokens
    /// are written only through the platform secure store. Local progression is
    /// never touched by authentication failures.
    /// </summary>
    public sealed class AccountService : IAccountService, IDisposable
    {
        private readonly IAuthApiClient _api;
        private readonly ISecureTokenStore _store;
        private readonly SemaphoreSlim _refreshGate = new(1, 1);
        private string _refreshToken;
        private string _accessToken;
        private Guid? _accountId;
        private string _username = string.Empty;

        public AccountService(IAuthApiClient api, ISecureTokenStore store)
        {
            _api = api ?? throw new ArgumentNullException(nameof(api));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public AccountState State { get; private set; } = AccountState.SignedOut;

        public string Username => _username;

        public Guid? AccountId => _accountId;

        public string AccessToken => _accessToken;

        public event Action<AccountState> StateChanged;

        public static IAccountService CreateDefault()
        {
            ApiConfig.Initialize();
            return new AccountService(new AuthApiClient(ApiConfig.BaseUrl), SecureTokenStore.CreateDefault());
        }

        public async Task InitializeAsync(CancellationToken cancellationToken)
        {
            var stored = _store.Load();
            if (string.IsNullOrWhiteSpace(stored))
            {
                SetState(AccountState.SignedOut);
                return;
            }

            _refreshToken = stored;
            await RefreshAsync(cancellationToken);
        }

        public async Task<AccountActionResult> RegisterAsync(
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            if (!ValidateCredentials(username, password, out var error))
            {
                return new AccountActionResult { Success = false, State = State, Message = error };
            }

            SetState(AccountState.SigningIn);
            var register = await _api.RegisterAsync(username, password, cancellationToken);
            if (!register.Success)
            {
                SetState(register.NetworkError ? AccountState.Offline : AccountState.SignedOut);
                return Failure(register);
            }

            return await LoginAsync(username, password, cancellationToken);
        }

        public async Task<AccountActionResult> LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            if (!ValidateCredentials(username, password, out var error))
            {
                return new AccountActionResult { Success = false, State = State, Message = error };
            }

            SetState(AccountState.SigningIn);
            var login = await _api.LoginAsync(username, password, cancellationToken);
            if (!login.Success || login.Tokens is null)
            {
                SetState(login.NetworkError ? AccountState.Offline : AccountState.SignedOut);
                return Failure(login);
            }

            ApplyTokens(login.Tokens);
            return new AccountActionResult
            {
                Success = true,
                State = State,
                Message = Localization.Format("account.welcome", _username)
            };
        }

        public async Task LogoutAsync(CancellationToken cancellationToken)
        {
            var accessToken = _accessToken;
            var refreshToken = _refreshToken;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                try
                {
                    await _api.LogoutAsync(accessToken, refreshToken, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    // Logout clears local credentials even when the server is unreachable.
                }
            }

            ClearSession();
        }

        public async Task<bool> RefreshAsync(CancellationToken cancellationToken)
        {
            await _refreshGate.WaitAsync(cancellationToken);
            try
            {
                if (string.IsNullOrWhiteSpace(_refreshToken))
                {
                    SetState(AccountState.SignedOut);
                    return false;
                }

                var result = await _api.RefreshAsync(_refreshToken, cancellationToken);
                if (result.Success && result.Tokens is not null)
                {
                    ApplyTokens(result.Tokens);
                    return true;
                }

                if (result.NetworkError)
                {
                    SetState(AccountState.Offline);
                    return false;
                }

                ClearSession();
                return false;
            }
            finally
            {
                _refreshGate.Release();
            }
        }

        public void Dispose()
        {
            _refreshGate.Dispose();
        }

        private void ApplyTokens(TokenResponseDto tokens)
        {
            _accessToken = tokens.accessToken;
            _refreshToken = tokens.refreshToken;
            _username = tokens.username ?? string.Empty;
            _accountId = Guid.TryParse(tokens.accountId, out var accountId) ? accountId : null;
            if (!string.IsNullOrWhiteSpace(_refreshToken))
            {
                _store.Save(_refreshToken);
            }

            SetState(AccountState.SignedIn);
        }

        private void ClearSession()
        {
            _accessToken = null;
            _refreshToken = null;
            _accountId = null;
            _username = string.Empty;
            _store.Clear();
            SetState(AccountState.SignedOut);
        }

        private void SetState(AccountState state)
        {
            if (State == state)
            {
                return;
            }

            State = state;
            StateChanged?.Invoke(state);
        }

        private static bool ValidateCredentials(string username, string password, out string error)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            {
                error = Localization.Get("account.error.username");
                return false;
            }

            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            {
                error = Localization.Get("account.error.password");
                return false;
            }

            error = string.Empty;
            return true;
        }

        private AccountActionResult Failure(AuthResult result)
        {
            var message = result.ErrorCode switch
            {
                "username_taken" => Localization.Get("account.error.taken"),
                "invalid_credentials" => Localization.Get("account.error.invalid"),
                "unauthorized" or "http_401" => Localization.Get("account.error.invalid"),
                "invalid_username" => Localization.Get("account.error.username"),
                "invalid_password" => Localization.Get("account.error.password"),
                "invalid_request" or "http_400" => Localization.Get("account.error.validation"),
                "rate_limited" or "http_429" => Localization.Get("account.error.rateLimited"),
                "network_error" or "timeout" => Localization.Get("account.error.network"),
                _ => Localization.Get("account.error.generic")
            };

            if (string.IsNullOrWhiteSpace(message))
            {
                message = Localization.Get("account.error.generic");
            }

            return new AccountActionResult
            {
                Success = false,
                State = State,
                Message = message
            };
        }
    }
}
