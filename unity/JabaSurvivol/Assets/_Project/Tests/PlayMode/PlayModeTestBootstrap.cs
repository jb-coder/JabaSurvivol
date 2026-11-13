using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;

[SetUpFixture]
public sealed class PlayModeTestBootstrap
{
    [OneTimeSetUp]
    public void SetUp()
    {
        GameBootstrap.OnlineDisabledForTests = true;
        PlayerPrefs.DeleteKey(Localization.LocalePreferenceKey);
        PlayerPrefs.Save();
        Localization.SetLocale("en");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        GameBootstrap.OnlineDisabledForTests = false;
        PlayerPrefs.DeleteKey(Localization.LocalePreferenceKey);
        PlayerPrefs.Save();
        Localization.ReloadForTests();
    }
}
