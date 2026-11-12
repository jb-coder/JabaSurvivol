using System;

namespace JabaSurvivol.Gameplay
{
    [Serializable]
    public sealed class RegisterRequestDto
    {
        public string username;
        public string password;
    }

    [Serializable]
    public sealed class LoginRequestDto
    {
        public string username;
        public string password;
    }

    [Serializable]
    public sealed class RefreshRequestDto
    {
        public string refreshToken;
    }

    [Serializable]
    public sealed class LogoutRequestDto
    {
        public string refreshToken;
    }

    [Serializable]
    public sealed class AccountResponseDto
    {
        public string accountId;
        public string username;
    }

    [Serializable]
    public sealed class TokenResponseDto
    {
        public string accessToken;
        public int expiresInSeconds;
        public string refreshToken;
        public int refreshExpiresInSeconds;
        public string accountId;
        public string username;
    }

    [Serializable]
    public sealed class ProblemResponseDto
    {
        public string title;
        public int status;
        public string detail;
        public string code;
    }

    public sealed class AuthResult
    {
        public bool Success { get; set; }
        public bool NetworkError { get; set; }
        public int StatusCode { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public AccountResponseDto Account { get; set; }
        public TokenResponseDto Tokens { get; set; }

        public static AuthResult FromAccount(AccountResponseDto account)
        {
            return new AuthResult
            {
                Success = true,
                Account = account,
                StatusCode = 200
            };
        }

        public static AuthResult FromTokens(TokenResponseDto tokens)
        {
            return new AuthResult
            {
                Success = true,
                Tokens = tokens,
                StatusCode = 200
            };
        }

        public static AuthResult Failure(int statusCode, string code, string message, bool networkError = false)
        {
            return new AuthResult
            {
                Success = false,
                StatusCode = statusCode,
                ErrorCode = code,
                ErrorMessage = message,
                NetworkError = networkError
            };
        }
    }

    public enum AccountState
    {
        SignedOut,
        SigningIn,
        SignedIn,
        Offline
    }

    public sealed class AccountActionResult
    {
        public bool Success { get; set; }
        public AccountState State { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
