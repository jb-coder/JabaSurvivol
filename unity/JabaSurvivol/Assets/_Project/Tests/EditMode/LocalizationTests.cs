using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class LocalizationTests
    {
        // Spanish needs these accented letters and inverted punctuation marks.
        // Checking the exact runtime UI font guards against a font swap that
        // silently renders question marks or empty boxes for Spanish text.
        private static readonly char[] SpanishGlyphs =
        {
            'á', 'é', 'í', 'ó', 'ú', 'ñ', 'ü', '¿', '¡'
        };

        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(Localization.LocalePreferenceKey);
            PlayerPrefs.Save();
            Localization.ReloadForTests();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(Localization.LocalePreferenceKey);
            PlayerPrefs.Save();
            Localization.ReloadForTests();
        }

        [Test]
        public void Supported_locales_are_english_and_spanish()
        {
            Assert.That(Localization.SupportedLocales, Is.EquivalentTo(new[] { "en", "es" }));
        }

        [Test]
        public void English_and_spanish_have_the_same_keys()
        {
            var english = Localization.GetKeys("en").OrderBy(key => key).ToArray();
            var spanish = Localization.GetKeys("es").OrderBy(key => key).ToArray();

            Assert.That(english, Is.Not.Empty);
            Assert.That(spanish, Is.EquivalentTo(english));
        }

        [Test]
        public void Runtime_literal_localization_keys_exist_in_both_locales()
        {
            var runtimeRoot = Path.Combine(Application.dataPath, "_Project", "Scripts", "Runtime");
            var directKey = new Regex(
                "Localization\\.(?:Get|Format)\\(\\s*\"([^\"]+)\"",
                RegexOptions.Singleline);
            var boundKey = new Regex(
                "BindLocalized(?:Button)?\\([^;\\n]*?\"([a-z][A-Za-z0-9]*(?:\\.[a-z][A-Za-z0-9]*)+)\"");
            var keys = new HashSet<string>(StringComparer.Ordinal);

            foreach (var file in Directory.GetFiles(runtimeRoot, "*.cs", SearchOption.AllDirectories))
            {
                var source = File.ReadAllText(file);
                foreach (Match match in directKey.Matches(source))
                {
                    keys.Add(match.Groups[1].Value);
                }

                foreach (Match match in boundKey.Matches(source))
                {
                    keys.Add(match.Groups[1].Value);
                }
            }

            var english = new HashSet<string>(Localization.GetKeys("en"), StringComparer.Ordinal);
            var spanish = new HashSet<string>(Localization.GetKeys("es"), StringComparer.Ordinal);

            Assert.That(keys, Is.Not.Empty);
            foreach (var key in keys)
            {
                Assert.That(english.Contains(key), Is.True, $"Missing English key: {key}");
                Assert.That(spanish.Contains(key), Is.True, $"Missing Spanish key: {key}");
            }
        }

        [Test]
        public void Every_spanish_value_is_nonempty_and_not_a_key()
        {
            Localization.SetLocale("es");

            foreach (var key in Localization.GetKeys("es"))
            {
                var value = Localization.Get(key);
                Assert.That(value, Is.Not.Empty, key);
                Assert.That(value, Is.Not.EqualTo("..."), key);
                Assert.That(value, Is.Not.EqualTo(key), key);
            }
        }

        [Test]
        public void Every_english_value_is_nonempty_and_not_a_key()
        {
            Localization.SetLocale("en");

            foreach (var key in Localization.GetKeys("en"))
            {
                var value = Localization.Get(key);
                Assert.That(value, Is.Not.Empty, key);
                Assert.That(value, Is.Not.EqualTo("..."), key);
                Assert.That(value, Is.Not.EqualTo(key), key);
            }
        }

        [Test]
        public void Missing_key_falls_back_to_english_before_the_placeholder()
        {
            var active = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["present.key"] = "ACTIVE"
            };
            var fallback = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["present.key"] = "ENGLISH",
                ["fallback.key"] = "ENGLISH FALLBACK"
            };

            var value = Localization.ResolveValueForTests(
                "fallback.key",
                active,
                fallback,
                out var usedFallback);

            Assert.That(value, Is.EqualTo("ENGLISH FALLBACK"));
            Assert.That(usedFallback, Is.True);
            Assert.That(value, Is.Not.EqualTo("fallback.key"));

            var missing = Localization.ResolveValueForTests(
                "missing.key",
                active,
                fallback,
                out var missingUsedFallback);

            Assert.That(missing, Is.EqualTo("..."));
            Assert.That(missing, Is.Not.EqualTo("missing.key"));
            Assert.That(missingUsedFallback, Is.False);
        }

        [Test]
        public void Pseudo_locale_expands_every_english_value_by_at_least_thirty_percent()
        {
            Localization.SetLocale("en");
            var english = Localization.GetKeys("en")
                .ToDictionary(key => key, key => Localization.Get(key), StringComparer.Ordinal);

            Localization.UsePseudoLocaleForTests();

            Assert.That(Localization.CurrentLocale, Is.EqualTo(Localization.PseudoLocale));
            Assert.That(
                PlayerPrefs.GetString(Localization.LocalePreferenceKey),
                Is.EqualTo("en"));

            foreach (var pair in english)
            {
                var pseudo = Localization.Get(pair.Key);
                var minimumLength = (int)Math.Ceiling(pair.Value.Length * 1.3d);

                Assert.That(pseudo.Length, Is.GreaterThanOrEqualTo(minimumLength), pair.Key);
                Assert.That(pseudo, Is.Not.EqualTo(pair.Value), pair.Key);
                Assert.That(pseudo, Is.Not.EqualTo(pair.Key), pair.Key);
                Assert.That(pseudo, Does.Contain("["), pair.Key);

                foreach (Match placeholder in Regex.Matches(pair.Value, "\\{\\d+\\}"))
                {
                    Assert.That(pseudo, Does.Contain(placeholder.Value), pair.Key);
                }
            }
        }

        [Test]
        public void Device_language_mapping_prefers_supported_system_languages()
        {
            Assert.That(
                Localization.MapSystemLanguage(SystemLanguage.Spanish, "en-US"),
                Is.EqualTo("es"));
            Assert.That(
                Localization.MapSystemLanguage(SystemLanguage.English, "es-ES"),
                Is.EqualTo("en"));
            Assert.That(
                Localization.MapSystemLanguage(SystemLanguage.French, "es-MX"),
                Is.EqualTo("es"));
            Assert.That(
                Localization.MapSystemLanguage(SystemLanguage.French, "fr-FR"),
                Is.EqualTo("en"));
        }

        [Test]
        public void Saved_override_wins_over_the_device_language()
        {
            Assert.That(Localization.ResolveLocale("es", SystemLanguage.English, "en-US"), Is.EqualTo("es"));
            Assert.That(Localization.ResolveLocale("en", SystemLanguage.Spanish, "es-ES"), Is.EqualTo("en"));
            Assert.That(Localization.ResolveLocale("system", SystemLanguage.Spanish, "es-ES"), Is.EqualTo("es"));
            Assert.That(Localization.ResolveLocale("invalid", SystemLanguage.French, "fr-FR"), Is.EqualTo("en"));
        }

        [Test]
        public void Set_locale_persists_across_reload()
        {
            Localization.SetLocale("es");

            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
            Assert.That(
                PlayerPrefs.GetString(Localization.LocalePreferenceKey),
                Is.EqualTo("es"));
            Assert.That(Localization.DisplayCulture.Name, Is.EqualTo("es-ES"));

            Localization.ReloadForTests();

            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
        }

        [Test]
        public void Use_system_locale_clears_the_override()
        {
            Localization.SetLocale("es");
            Localization.UseSystemLocale();
            var expected = Localization.MapSystemLanguage(
                Application.systemLanguage,
                CultureInfo.CurrentUICulture.TwoLetterISOLanguageName);

            Assert.That(
                PlayerPrefs.GetString(Localization.LocalePreferenceKey),
                Is.EqualTo(Localization.SystemLocaleValue));
            Assert.That(Localization.CurrentLocale, Is.EqualTo(expected));
        }

        [Test]
        public void Spanish_values_differ_from_english_values()
        {
            Localization.SetLocale("es");
            var spanish = Localization.Get("account.password");

            Localization.SetLocale("en");
            var english = Localization.Get("account.password");

            Assert.That(spanish, Is.Not.EqualTo(english));
            Assert.That(spanish, Is.Not.EqualTo("..."));
        }

        [Test]
        public void Missing_key_returns_a_placeholder_and_never_the_raw_key()
        {
            LogAssert.Expect(
                LogType.Error,
                "Missing localization key in all locales: missing.key");

            var value = Localization.Get("missing.key");

            Assert.That(value, Is.EqualTo("..."));
            Assert.That(value, Is.Not.EqualTo("missing.key"));
        }

        [Test]
        public void Display_culture_uses_locale_number_conventions()
        {
            Localization.SetLocale("es");
            Assert.That(1.5f.ToString(Localization.DisplayCulture), Is.EqualTo("1,5"));

            Localization.SetLocale("en");
            Assert.That(1.5f.ToString(Localization.DisplayCulture), Is.EqualTo("1.5"));
        }

        [Test]
        public void Invalid_locale_is_rejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Localization.SetLocale("fr"));
        }

        [Test]
        public void Locale_change_event_fires()
        {
            var fired = false;
            Action handler = () => fired = true;
            Localization.LocaleChanged += handler;

            try
            {
                Localization.SetLocale("es");
            }
            finally
            {
                Localization.LocaleChanged -= handler;
            }

            Assert.That(fired, Is.True);
        }

        [Test]
        public void Android_locale_config_declares_exactly_the_supported_locales()
        {
            // Unity 6.3 rejects resources under Assets/Plugins/Android/res, so
            // the locale table ships in the supported Android Library plug-in.
            var path = Path.Combine(
                Application.dataPath,
                "Plugins",
                "Android",
                "jaba_locales.androidlib",
                "src",
                "main",
                "res",
                "xml",
                "locales_config.xml");
            Assert.That(File.Exists(path), Is.True, path);

            var document = XDocument.Load(path);
            XNamespace android = AndroidNamespace;
            var declared = document.Root?
                .Elements("locale")
                .Select(element => element.Attribute(android + "name")?.Value)
                .Where(value => !string.IsNullOrEmpty(value))
                .ToArray();

            Assert.That(declared, Is.Not.Null);
            Assert.That(declared, Is.EquivalentTo(Localization.SupportedLocales));
            Assert.That(declared.Length, Is.EqualTo(Localization.SupportedLocales.Count));
        }

        [Test]
        public void Android_manifest_links_the_locale_config()
        {
            var path = Path.Combine(
                Application.dataPath,
                "Plugins",
                "Android",
                "AndroidManifest.xml");
            Assert.That(File.Exists(path), Is.True, path);

            var document = XDocument.Load(path);
            XNamespace android = AndroidNamespace;
            var application = document.Root?.Element("application");

            Assert.That(application, Is.Not.Null);
            Assert.That(
                application.Attribute(android + "localeConfig")?.Value,
                Is.EqualTo("@xml/locales_config"));
        }

        [Test]
        public void Runtime_ui_font_covers_spanish_glyphs()
        {
            var font = UiFactory.GetFontForTests();
            Assert.That(font, Is.Not.Null);

            foreach (var glyph in SpanishGlyphs)
            {
                Assert.That(
                    font.HasCharacter(glyph),
                    Is.True,
                    $"Runtime UI font '{font.name}' is missing '{glyph}'.");
            }
        }

        [Test]
        public void System_refresh_reapplies_a_changed_system_locale_and_raises_the_event_once()
        {
            Localization.UseSystemLocale();
            Localization.RefreshSystemLocaleForTests(SystemLanguage.English, "en-US");
            Assert.That(Localization.CurrentLocale, Is.EqualTo("en"));

            var fired = 0;
            Action handler = () => fired++;
            Localization.LocaleChanged += handler;

            try
            {
                Localization.RefreshSystemLocaleForTests(SystemLanguage.Spanish, "es-ES");

                Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
                Assert.That(fired, Is.EqualTo(1));

                Localization.RefreshSystemLocaleForTests(SystemLanguage.Spanish, "es-ES");

                Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
                Assert.That(fired, Is.EqualTo(1));
            }
            finally
            {
                Localization.LocaleChanged -= handler;
            }
        }

        [Test]
        public void Explicit_override_ignores_a_changed_system_locale()
        {
            Localization.SetLocale("en");
            var fired = false;
            Action handler = () => fired = true;
            Localization.LocaleChanged += handler;

            try
            {
                Localization.RefreshSystemLocaleForTests(SystemLanguage.Spanish, "es-ES");
            }
            finally
            {
                Localization.LocaleChanged -= handler;
            }

            Assert.That(Localization.CurrentLocale, Is.EqualTo("en"));
            Assert.That(PlayerPrefs.GetString(Localization.LocalePreferenceKey), Is.EqualTo("en"));
            Assert.That(fired, Is.False);
        }

        [Test]
        public void Invalid_stored_preference_falls_back_to_the_system_locale()
        {
            PlayerPrefs.SetString(Localization.LocalePreferenceKey, "fr");
            PlayerPrefs.Save();
            Localization.ReloadForTests();

            Localization.RefreshSystemLocaleForTests(SystemLanguage.Spanish, "es-ES");

            Assert.That(Localization.CurrentLocale, Is.EqualTo("es"));
        }
    }
}
