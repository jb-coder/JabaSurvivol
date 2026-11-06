using System;
using System.Collections.Generic;
using System.Globalization;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// English-only string table for the walking skeleton. Keys are stable so
    /// the localization phase can replace the storage without touching UI code.
    /// </summary>
    public static class Localization
    {
        private static readonly Dictionary<string, string> Entries = new(StringComparer.Ordinal)
        {
            ["app.title"] = "JABASURVIVOL",
            ["menu.subtitle"] = "SURVIVE THE NIGHT. KEEP EVERYTHING.",
            ["menu.start"] = "START RUN",
            ["menu.progress"] = "SUPPLIES {0}    LEVEL {1}    BEST ROUND {2}",
            ["menu.controls"] = "LEFT STICK MOVE    RIGHT STICK AIM & FIRE",
            ["hud.health"] = "HP {0}/{1}",
            ["hud.round"] = "ROUND {0}/{1}",
            ["hud.enemies"] = "ENEMIES REMAINING {0}",
            ["hud.supplies"] = "SUPPLIES {0}",
            ["hud.level"] = "LEVEL {0}",
            ["hud.kills"] = "KILLS {0}",
            ["hud.pause"] = "II",
            ["pause.title"] = "PAUSED",
            ["pause.resume"] = "RESUME",
            ["pause.menu"] = "MAIN MENU",
            ["gameover.title"] = "YOU DIED",
            ["gameover.summary"] = "ROUND {0}    KILLS {1}\nSUPPLIES EARNED {2}",
            ["gameover.restart"] = "RESTART",
            ["gameover.menu"] = "MAIN MENU",
            ["round.clear"] = "ROUND {0} CLEARED"
        };

        public static string Get(string key)
        {
            return Entries.TryGetValue(key, out var value) ? value : key;
        }

        public static string Format(string key, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arguments);
        }
    }
}
