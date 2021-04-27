#if __EMBY__
using MediaBrowser.Model.Logging;
#else
using Microsoft.Extensions.Logging;
#endif

namespace ThePornDB.Helpers
{
    internal static class Logger
    {
        private static ILogger Log { get; } = Plugin.Log;

        public static void Info(string text)
        {
#if __EMBY__
            Log?.Info(text);
#else
            Log?.LogInformation(text);
#endif
        }

        public static void Error(string text)
        {
#if __EMBY__
            Log?.Error(text);
#else
            Log?.LogError(text);
#endif
        }

        public static void Debug(string text)
        {
#if __EMBY__
            Log?.Debug(text);
#else
            Log?.LogDebug(text);
#endif
        }

        public static void Warning(string text)
        {
#if __EMBY__
            Log?.Warn(text);
#else
            Log?.LogWarning(text);
#endif
        }
    }
}
