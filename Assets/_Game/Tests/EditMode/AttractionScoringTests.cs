using BoozeBlocks.Distractions;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class AttractionScoringTests
    {
        [Test]
        public void HigherPriorityWinsEvenWhenFartherAway()
        {
            bool better = AttractionScoring.IsBetter(100, 100f, 10, 1f);
            Assert.That(better, Is.True);
        }

        [Test]
        public void DistanceBreaksEqualPriorityTie()
        {
            bool better = AttractionScoring.IsBetter(10, 4f, 10, 9f);
            Assert.That(better, Is.True);
        }
    }
}
