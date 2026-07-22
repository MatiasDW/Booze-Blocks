using BoozeBlocks.Prototype;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class TeamRulesTests
    {
        [TestCase(0, 0, false)]
        [TestCase(1, 1, false)]
        [TestCase(8, 1, false)]
        [TestCase(8, 0, true)]
        public void Defeat_RequiresARegisteredTeamWithNoSurvivors(int registered, int survivors, bool expected)
        {
            Assert.That(TeamRules.IsDefeated(registered, survivors), Is.EqualTo(expected));
        }
    }
}
