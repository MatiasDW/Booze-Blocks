using BoozeBlocks.Horde;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class HordeScalingModelTests
    {
        [TestCase(0, 0, 0)]
        [TestCase(1, 0, 14)]
        [TestCase(4, 0, 32)]
        [TestCase(8, 0, 56)]
        [TestCase(8, 1, 62)]
        [TestCase(8, 20, 96)]
        public void ActiveUnits_ScaleWithPlayersWavesAndCap(int players, int difficultyStep, int expected)
        {
            int result = HordeScalingModel.CalculateActiveUnits(players, difficultyStep, 14, 6, 6, 96);
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Pressure_UsesDiminishingAdditionalContribution()
        {
            float result = HordeScalingModel.CalculatePressurePerSecond(4, 9f, 0.7f);
            Assert.That(result, Is.EqualTo(27.9f).Within(0.001f));
        }
    }
}
