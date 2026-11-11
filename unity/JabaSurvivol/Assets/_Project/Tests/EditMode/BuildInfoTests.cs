using JabaSurvivol.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace JabaSurvivol.Tests.EditMode
{
    public sealed class BuildInfoTests
    {
        [Test]
        public void Version_matches_the_player_version()
        {
            Assert.That(BuildInfo.Version, Is.EqualTo(Application.version));
        }

        [Test]
        public void Commit_is_available()
        {
            Assert.That(string.IsNullOrWhiteSpace(BuildInfo.Commit), Is.False);
        }
    }
}
