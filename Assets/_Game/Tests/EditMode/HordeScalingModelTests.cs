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

        [Test]
        public void HealthDamage_UsesLowDiminishingContactRate()
        {
            float result = HordeScalingModel.CalculatePressurePerSecond(4, 0.65f, 0.5f);
            Assert.That(result, Is.EqualTo(1.625f).Within(0.001f));
        }

        [TestCase(14, 0f, 10f, 0)]
        [TestCase(14, 2.5f, 10f, 4)]
        [TestCase(14, 5f, 10f, 7)]
        [TestCase(14, 10f, 10f, 14)]
        [TestCase(14, 30f, 10f, 14)]
        [TestCase(56, 1f, 10f, 6)]
        public void RampedUnits_EnterProgressivelyAndNeverExceedWaveLimit(int maximum,
            float elapsed, float rampDuration, int expected)
        {
            int result = HordeScalingModel.CalculateRampedUnits(maximum, elapsed, rampDuration);
            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
