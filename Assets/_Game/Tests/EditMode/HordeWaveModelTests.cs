using BoozeBlocks.Horde;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class HordeWaveModelTests
    {
        [Test]
        public void Difficulty_IncreasesEveryConfiguredNumberOfWaves()
        {
            HordeWaveModel model = new HordeWaveModel(20f, 5f, 2);

            model.Tick(25f);
            Assert.That(model.WaveNumber, Is.EqualTo(2));
            Assert.That(model.DifficultyStep, Is.EqualTo(0));

            model.Tick(25f);
            Assert.That(model.WaveNumber, Is.EqualTo(3));
            Assert.That(model.DifficultyStep, Is.EqualTo(1));
        }

        [Test]
        public void Tick_AlternatesBetweenActiveWaveAndBreak()
        {
            HordeWaveModel model = new HordeWaveModel(20f, 5f, 2);

            model.Tick(20f);
            Assert.That(model.IsWaveActive, Is.False);

            model.Tick(5f);
            Assert.That(model.IsWaveActive, Is.True);
            Assert.That(model.WaveNumber, Is.EqualTo(2));
        }
    }
}
