using BoozeBlocks.Distractions;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class AttractionCapacityModelTests
    {
        [TestCase(1, 6)]
        [TestCase(4, 12)]
        [TestCase(8, 20)]
        public void Capacity_ScalesWithAdditionalPlayers(int players, int expected)
        {
            int result = AttractionCapacityModel.Calculate(players, 6, 2);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
