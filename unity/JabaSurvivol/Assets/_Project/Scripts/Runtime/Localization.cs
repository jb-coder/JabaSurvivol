using System;
using System.Collections.Generic;
using System.Globalization;
using JabaSurvivol.Core;

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
            ["menu.armory"] = "ARMORY",
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
            ["pause.armory"] = "ARMORY",
            ["armory.title"] = "ARMORY",
            ["armory.close"] = "CLOSE",
            ["armory.supplies"] = "SUPPLIES {0}",
            ["armory.buy"] = "BUY {0}",
            ["armory.equip"] = "EQUIP",
            ["armory.equipped"] = "EQUIPPED",
            ["armory.purchased"] = "{0} PURCHASED AND EQUIPPED",
            ["armory.equipped.status"] = "{0} EQUIPPED",
            ["armory.insufficient"] = "NOT ENOUGH SUPPLIES",
            ["armory.stats"] = "DMG {0}   RATE {1}/s   MAG {2}   RANGE {3}   RESERVE {4}",
            ["armory.reserve.infinite"] = "INF",
            ["weapon.pistol.name"] = "PISTOL",
            ["weapon.smg.name"] = "SMG",
            ["weapon.shotgun.name"] = "SHOTGUN",
            ["weapon.rifle.name"] = "RIFLE",
            ["weapon.lmg.name"] = "LMG",
            ["gameover.title"] = "YOU DIED",
            ["gameover.summary"] = "ROUND {0}    KILLS {1}\nSUPPLIES EARNED {2}",
            ["gameover.restart"] = "RESTART",
            ["gameover.menu"] = "MAIN MENU",
            ["round.end.title"] = "ROUND {0} CLEARED",
            ["round.end.hint"] = "TAP A REWARD OR PRESS 1, 2, OR 3",
            ["round.end.summary"] = "KILLS {0}    SUPPLIES {1}    XP {2}",
            ["reward.supplies.card"] = "SUPPLIES\n+{0}",
            ["reward.xp.card"] = "XP\n+{0}",
            ["reward.ammo.card"] = "AMMO CRATE\n+{0}",
            ["reward.armor.card"] = "ARMOR PLATE\n+{0}"
        };

        public static string Get(string key)
        {
            return Entries.TryGetValue(key, out var value) ? value : key;
        }

        public static string Format(string key, params object[] arguments)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(key), arguments);
        }

        public static string WeaponNameKey(WeaponId weaponId)
        {
            return weaponId switch
            {
                WeaponId.Smg => "weapon.smg.name",
                WeaponId.Shotgun => "weapon.shotgun.name",
                WeaponId.Rifle => "weapon.rifle.name",
                WeaponId.Lmg => "weapon.lmg.name",
                _ => "weapon.pistol.name"
            };
        }

        public static string WeaponName(WeaponId weaponId)
        {
            return Get(WeaponNameKey(weaponId));
        }
    }
}
