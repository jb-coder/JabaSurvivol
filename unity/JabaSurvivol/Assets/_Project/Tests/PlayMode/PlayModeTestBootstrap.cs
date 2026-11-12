using JabaSurvivol.Gameplay;
using NUnit.Framework;

[SetUpFixture]
public sealed class PlayModeTestBootstrap
{
    [OneTimeSetUp]
    public void SetUp()
    {
        GameBootstrap.OnlineDisabledForTests = true;
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        GameBootstrap.OnlineDisabledForTests = false;
    }
}
