using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    public interface IAuthApiClient
    {
        Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken);
        Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken);
        Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
        Task<AuthResult> LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken);
    }

    /// <summary>
    /// HTTP client for the identity API. Tokens are never logged. ProblemDetails
    /// codes are surfaced to the UI without exposing server internals.
    /// </summary>
    public sealed class AuthApiClient : IAuthApiClient, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;

        public AuthApiClient(string baseUrl, HttpMessageHandler handler = null)
        {
            var normalized = (baseUrl ?? ApiConfig.BaseUrl).TrimEnd('/');
            _httpClient = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            _ownsClient = handler is null;
            _httpClient.BaseAddress = new Uri(normalized + "/", UriKind.Absolute);
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public Task<AuthResult> RegisterAsync(string username, string password, CancellationToken cancellationToken)
        {
            return SendAsync<AccountResponseDto>(
                HttpMethod.Post,
                "api/v1/identity/register",
                new RegisterRequestDto { username = username, password = password },
                accessToken: null,
                cancellationToken);
        }

        public Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken)
        {
            return SendAsync<TokenResponseDto>(
                HttpMethod.Post,
                "api/v1/identity/login",
                new LoginRequestDto { username = username, password = password },
                accessToken: null,
                cancellationToken);
        }

        public Task<AuthResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
        {
            return SendAsync<TokenResponseDto>(
                HttpMethod.Post,
                "api/v1/identity/refresh",
                new RefreshRequestDto { refreshToken = refreshToken },
                accessToken: null,
                cancellationToken);
        }

        public Task<AuthResult> LogoutAsync(
            string accessToken,
            string refreshToken,
            CancellationToken cancellationToken)
        {
            return SendAsync<AccountResponseDto>(
                HttpMethod.Post,
                "api/v1/identity/logout",
                new LogoutRequestDto { refreshToken = refreshToken },
                accessToken,
                cancellationToken);
        }

        public void Dispose()
        {
            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }

        private async Task<AuthResult> SendAsync<T>(
            HttpMethod method,
            string path,
            object body,
            string accessToken,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                request.Content = new StringContent(
                    JsonUtility.ToJson(body),
                    Encoding.UTF8,
                    "application/json");

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var text = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return ToFailure(response.StatusCode, text);
                }

                if (typeof(T) == typeof(AccountResponseDto))
                {
                    return AuthResult.FromAccount(JsonUtility.FromJson<AccountResponseDto>(text));
                }

                return AuthResult.FromTokens(JsonUtility.FromJson<TokenResponseDto>(text));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return AuthResult.Failure(0, "timeout", "The request timed out.", networkError: true);
            }
            catch (HttpRequestException)
            {
                return AuthResult.Failure(0, "network_error", "The server cannot be reached.", networkError: true);
            }
            catch (Exception)
            {
                return AuthResult.Failure(0, "client_error", "The request could not be completed.");
            }
        }

        private static AuthResult ToFailure(System.Net.HttpStatusCode statusCode, string text)
        {
            var problem = string.IsNullOrWhiteSpace(text)
                ? null
                : JsonUtility.FromJson<ProblemResponseDto>(text);

            var code = string.IsNullOrWhiteSpace(problem?.code)
                ? $"http_{(int)statusCode}"
                : problem.code;
            var message = problem?.detail;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = problem?.title;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "The request could not be completed.";
            }

            return AuthResult.Failure((int)statusCode, code, message);
        }
    }

    public sealed class AuthenticatedResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool NetworkError { get; set; }
    }

    /// <summary>
    /// Sends authenticated requests and retries once after a successful token
    /// refresh when the API returns 401.
    /// </summary>
    public sealed class AuthenticatedApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly Func<string> _accessTokenProvider;
        private readonly Func<CancellationToken, Task<bool>> _refreshAsync;

        public AuthenticatedApiClient(
            HttpClient httpClient,
            Func<string> accessTokenProvider,
            Func<CancellationToken, Task<bool>> refreshAsync)
        {
            _httpClient = httpClient;
            _accessTokenProvider = accessTokenProvider;
            _refreshAsync = refreshAsync;
        }
        public async Task<AuthenticatedResponse> SendAsync(
            HttpMethod method,
            string path,
            string jsonBody,
            CancellationToken cancellationToken)
        {
            var response = await SendOnceAsync(method, path, jsonBody, _accessTokenProvider(), cancellationToken);
            if (response.StatusCode != 401)
            {
                return response;
            }

            if (!await _refreshAsync(cancellationToken))
            {
                return response;
            }

            return await SendOnceAsync(method, path, jsonBody, _accessTokenProvider(), cancellationToken);
        }

        private async Task<AuthenticatedResponse> SendOnceAsync(
            HttpMethod method,
            string path,
            string jsonBody,
            string accessToken,
            CancellationToken cancellationToken)
        {
            try
            {
                using var request = new HttpRequestMessage(method, path);
                if (!string.IsNullOrWhiteSpace(jsonBody))
                {
                    request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
                }

                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                return new AuthenticatedResponse
                {
                    StatusCode = (int)response.StatusCode,
                    Body = response.Content is null ? string.Empty : await response.Content.ReadAsStringAsync()
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                return new AuthenticatedResponse { StatusCode = 0, NetworkError = true };
            }
            catch (OperationCanceledException)
            {
                return new AuthenticatedResponse { StatusCode = 0, NetworkError = true };
            }
        }
    }
}
