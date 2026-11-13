using System;
using System.Collections.Generic;
using System.Globalization;
using JabaSurvivol.Core;
using UnityEngine;

namespace JabaSurvivol.Gameplay
{
    /// <summary>
    /// Locale-aware string table for user-facing text. English is the source
    /// locale and falls back when a key or locale is unavailable.
    /// </summary>
    public static class Localization
    {
        [Serializable]
        public sealed class LocalizationEntry
        {
            public string key;
            public string value;
        }

        [Serializable]
        public sealed class LocalizationFile
        {
            public string locale;
            public string displayName;
            public LocalizationEntry[] entries;
        }

        public const string DefaultLocale = "en";
        public const string SystemLocaleValue = "system";
        public const string LocalePreferenceKey = "localization.locale";

        private const string MissingText = "...";
        private static readonly string[] SupportedLocaleCodes = { "en", "es" };
        private static readonly HashSet<string> LoggedMissingKeys = new(StringComparer.Ordinal);
        private static Dictionary<string, string> _entries;
        private static Dictionary<string, string> _fallbackEntries;
        private static string _currentLocale;
        private static bool _initialized;

        public static event Action LocaleChanged;

        public static string CurrentLocale
        {
            get
            {
                EnsureInitialized();
                return _currentLocale;
            }
        }

        public static CultureInfo DisplayCulture
        {
            get
            {
                EnsureInitialized();
                return CultureInfo.GetCultureInfo(_currentLocale == "es" ? "es-ES" : "en-US");
            }
        }

        public static IReadOnlyList<string> SupportedLocales => SupportedLocaleCodes;

        public static string GetDisplayName(string locale)
        {
            var file = LoadLocaleFile(locale);
            return file?.displayName ?? locale;
        }

        public static IReadOnlyCollection<string> GetKeys(string locale)
        {
            var entries = LoadLocaleEntries(locale);
            return entries == null ? Array.Empty<string>() : entries.Keys;
        }

        public static string ResolveLocale(string preference, SystemLanguage language, string cultureName)
        {
            if (preference == "en" || preference == "es")
            {
                return preference;
            }

            return MapSystemLanguage(language, cultureName);
        }

        public static string MapSystemLanguage(SystemLanguage language, string cultureName)
        {
            if (language == SystemLanguage.Spanish)
            {
                return "es";
            }

            if (language == SystemLanguage.English)
            {
                return "en";
            }

            return NormalizeCulture(cultureName) == "es" ? "es" : "en";
        }

        public static void SetLocale(string locale)
        {
            EnsureInitialized();
            if (locale == SystemLocaleValue)
            {
                UseSystemLocale();
                return;
            }

            if (!IsSupported(locale))
            {
                throw new ArgumentOutOfRangeException(nameof(locale));
            }

            PlayerPrefs.SetString(LocalePreferenceKey, locale);
            PlayerPrefs.Save();
            ApplyLocale(locale, raiseEvent: true);
        }

        public static void UseSystemLocale()
        {
            EnsureInitialized();
            PlayerPrefs.SetString(LocalePreferenceKey, SystemLocaleValue);
            PlayerPrefs.Save();
            ApplyLocale(
                MapSystemLanguage(
                    Application.systemLanguage,
                    CultureInfo.CurrentUICulture.TwoLetterISOLanguageName),
                raiseEvent: true);
        }

        /// <summary>
        /// Test hook that clears cached state and reloads the saved preference.
        /// </summary>
        public static void ReloadForTests()
        {
            _initialized = false;
            _entries = null;
            _fallbackEntries = null;
            _currentLocale = null;
            LoggedMissingKeys.Clear();
            EnsureInitialized();
        }

        public static string Get(string key)
        {
            EnsureInitialized();

            if (string.IsNullOrWhiteSpace(key))
            {
                LogMissing(key, bothTables: true);
                return MissingText;
            }

            if (_entries != null && _entries.TryGetValue(key, out var value))
            {
                return value;
            }

            if (_fallbackEntries != null && _fallbackEntries.TryGetValue(key, out var fallback))
            {
                LogMissing(key, bothTables: false);
                return fallback;
            }

            LogMissing(key, bothTables: true);
            return MissingText;
        }

        public static string Format(string key, params object[] arguments)
        {
            return string.Format(DisplayCulture, Get(key), arguments);
        }

        public static string Format(string key, int arg0)
        {
            return string.Format(DisplayCulture, Get(key), arg0);
        }

        public static string Format(string key, int arg0, int arg1)
        {
            return string.Format(DisplayCulture, Get(key), arg0, arg1);
        }

        public static string Format(string key, int arg0, int arg1, int arg2)
        {
            return string.Format(DisplayCulture, Get(key), arg0, arg1, arg2);
        }

        public static string Format(string key, int arg0, int arg1, int arg2, int arg3)
        {
            return string.Format(DisplayCulture, Get(key), arg0, arg1, arg2, arg3);
        }

        public static string Format(string key, string arg0)
        {
            return string.Format(DisplayCulture, Get(key), arg0);
        }

        public static string Format(string key, string arg0, string arg1)
        {
            return string.Format(DisplayCulture, Get(key), arg0, arg1);
        }

        public static string Format(string key, string arg0, int arg1)
        {
            return string.Format(DisplayCulture, Get(key), arg0, arg1);
        }

        public static string Format(string key, int arg0, int arg1, string arg2)
        {
            return string.Format(DisplayCulture, Get(key), arg0, arg1, arg2);
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _fallbackEntries = LoadLocaleEntries(DefaultLocale) ??
                               new Dictionary<string, string>(StringComparer.Ordinal);
            var preference = PlayerPrefs.GetString(LocalePreferenceKey, SystemLocaleValue);
            var locale = ResolveLocale(
                preference,
                Application.systemLanguage,
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);
            ApplyLocale(locale, raiseEvent: false);
        }

        private static void ApplyLocale(string locale, bool raiseEvent)
        {
            var entries = locale == DefaultLocale
                ? _fallbackEntries
                : LoadLocaleEntries(locale);

            if (entries == null || entries.Count == 0)
            {
                locale = DefaultLocale;
                entries = _fallbackEntries;
            }

            _currentLocale = locale;
            _entries = entries ?? new Dictionary<string, string>(StringComparer.Ordinal);
            LoggedMissingKeys.Clear();

            if (raiseEvent)
            {
                LocaleChanged?.Invoke();
            }
        }

        private static Dictionary<string, string> LoadLocaleEntries(string locale)
        {
            var file = LoadLocaleFile(locale);
            if (file?.entries == null)
            {
                return null;
            }

            var entries = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var entry in file.entries)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.key) ||
                    string.IsNullOrEmpty(entry.value))
                {
                    Debug.LogError($"Locale {locale} contains an empty key or value.");
                    continue;
                }

                if (!entries.TryAdd(entry.key, entry.value))
                {
                    Debug.LogError($"Locale {locale} contains duplicate key {entry.key}.");
                }
            }

            return entries;
        }

        private static LocalizationFile LoadLocaleFile(string locale)
        {
            var asset = Resources.Load<TextAsset>("Localization/" + locale);
            if (asset == null)
            {
                Debug.LogError($"Missing localization resource for locale {locale}.");
                return null;
            }

            try
            {
                var file = JsonUtility.FromJson<LocalizationFile>(asset.text);
                if (file?.entries == null || file.entries.Length == 0)
                {
                    Debug.LogError($"Localization resource for locale {locale} is empty.");
                    return null;
                }

                return file;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Localization resource for locale {locale} is invalid: {exception.Message}");
                return null;
            }
        }

        private static bool IsSupported(string locale)
        {
            for (var i = 0; i < SupportedLocaleCodes.Length; i++)
            {
                if (SupportedLocaleCodes[i] == locale)
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeCulture(string cultureName)
        {
            if (string.IsNullOrWhiteSpace(cultureName))
            {
                return string.Empty;
            }

            var separator = cultureName.IndexOfAny(new[] { '-', '_' });
            var language = separator >= 0
                ? cultureName.Substring(0, separator)
                : cultureName;
            return language.ToLowerInvariant();
        }

        private static void LogMissing(string key, bool bothTables)
        {
            var displayKey = string.IsNullOrWhiteSpace(key) ? "<empty>" : key;
            if (!LoggedMissingKeys.Add(displayKey))
            {
                return;
            }

            if (bothTables)
            {
                Debug.LogError($"Missing localization key in all locales: {displayKey}");
            }
            else
            {
                Debug.LogWarning(
                    $"Missing localization key in {_currentLocale}; using {DefaultLocale}: {displayKey}");
            }
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

        public static string WeaponUpgradeStatKey(WeaponUpgradeStat stat)
        {
            return stat switch
            {
                WeaponUpgradeStat.FireRate => "weapon.upgrade.firerate",
                WeaponUpgradeStat.MagazineSize => "weapon.upgrade.magazine",
                WeaponUpgradeStat.ReloadSpeed => "weapon.upgrade.reload",
                _ => "weapon.upgrade.damage"
            };
        }

        public static string WeaponUpgradeStatName(WeaponUpgradeStat stat)
        {
            return Get(WeaponUpgradeStatKey(stat));
        }

        public static string ArmorNameKey(ArmorTier tier)
        {
            return tier switch
            {
                ArmorTier.Light => "armory.armor.light",
                ArmorTier.Medium => "armory.armor.medium",
                ArmorTier.Heavy => "armory.armor.heavy",
                _ => "armory.armor.none"
            };
        }

        public static string ArmorName(ArmorTier tier)
        {
            return Get(ArmorNameKey(tier));
        }

        public static string BranchNameKey(SkillBranch branch)
        {
            return branch switch
            {
                SkillBranch.Agility => "character.branch.agility",
                SkillBranch.Survival => "character.branch.survival",
                _ => "character.branch.strength"
            };
        }

        public static string BranchName(SkillBranch branch)
        {
            return Get(BranchNameKey(branch));
        }

        public static string SkillNameKey(SkillNode node)
        {
            return node switch
            {
                SkillNode.Vitality => "skill.vitality.name",
                SkillNode.Shockwave => "skill.shockwave.name",
                SkillNode.Speed => "skill.speed.name",
                SkillNode.Precision => "skill.precision.name",
                SkillNode.Dash => "skill.dash.name",
                SkillNode.Endurance => "skill.endurance.name",
                SkillNode.Engineering => "skill.engineering.name",
                SkillNode.Turret => "skill.turret.name",
                _ => "skill.strength.name"
            };
        }

        public static string SkillName(SkillNode node)
        {
            return Get(SkillNameKey(node));
        }
    }
}
