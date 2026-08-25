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

        [Test]
        public void InitialPreparation_DelaysFirstWaveWithoutAdvancingWaveNumber()
        {
            HordeWaveModel model = new HordeWaveModel(20f, 7f, 2, 12f);

            Assert.That(model.Phase, Is.EqualTo(HordeWavePhase.Preparation));
            Assert.That(model.IsWaveActive, Is.False);
            Assert.That(model.WaveNumber, Is.EqualTo(1));

            model.Tick(12f);

            Assert.That(model.Phase, Is.EqualTo(HordeWavePhase.Active));
            Assert.That(model.IsWaveActive, Is.True);
            Assert.That(model.WaveNumber, Is.EqualTo(1));
            Assert.That(model.RemainingTime, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void ActiveElapsedTime_OnlyAdvancesDuringWave()
        {
            HordeWaveModel model = new HordeWaveModel(20f, 7f, 2, 12f);

            model.Tick(14.5f);

            Assert.That(model.ActiveElapsedTime, Is.EqualTo(2.5f).Within(0.001f));
        }
    }
}
