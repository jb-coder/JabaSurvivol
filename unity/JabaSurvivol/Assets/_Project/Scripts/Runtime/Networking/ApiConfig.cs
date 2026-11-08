using System;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Resolves the API base URL. Production uses HTTPS only. Debug builds may
    /// override the URL from the command line or an Android intent extra for
    /// the local Aspire backend, but only for loopback or emulator hosts.
    /// </summary>
    public static class ApiConfig
    {
        public const string ProductionBaseUrl = "https://api.jabasurvivol.com";

        public static string BaseUrl { get; private set; } = ProductionBaseUrl;

        public static void Initialize()
        {
            var overrideUrl = ReadOverride();
            BaseUrl = Normalize(overrideUrl) ?? ProductionBaseUrl;
        }

        private static string ReadOverride()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], "-api-base-url", StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[index + 1];
                }
            }

            if (Application.platform == RuntimePlatform.Android)
            {
                try
                {
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    using var intent = activity.Call<AndroidJavaObject>("getIntent");
                    return intent?.Call<string>("getStringExtra", "apiBaseUrl");
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value) ||
                !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri))
            {
                return null;
            }

            if (uri.Scheme == Uri.UriSchemeHttps)
            {
                return uri.ToString().TrimEnd('/');
            }

            if (uri.Scheme == Uri.UriSchemeHttp && IsLocalHost(uri.Host))
            {
                return uri.ToString().TrimEnd('/');
            }

            return null;
        }

        private static bool IsLocalHost(string host)
        {
            return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(host, "10.0.2.2", StringComparison.OrdinalIgnoreCase);
        }
    }
}
