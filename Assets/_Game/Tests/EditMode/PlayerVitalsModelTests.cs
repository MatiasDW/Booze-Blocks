using BoozeBlocks.Player;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class PlayerVitalsModelTests
    {
        [Test]
        public void Tick_DrainsBuzzBeforeHealth()
        {
            PlayerVitalsModel model = new PlayerVitalsModel(100f, 10f, 100f);

            model.Tick(1f, 5f, 20f, 0f);

            Assert.That(model.Buzz, Is.EqualTo(5f));
            Assert.That(model.Health, Is.EqualTo(100f));
        }

        [Test]
        public void Tick_DrainsHealthWhenBuzzIsEmpty()
        {
            PlayerVitalsModel model = new PlayerVitalsModel(100f, 10f, 100f);

            model.Tick(1f, 10f, 20f, 0f);
            model.Tick(1f, 10f, 20f, 0f);

            Assert.That(model.Buzz, Is.Zero);
            Assert.That(model.Health, Is.EqualTo(60f));
        }

        [Test]
        public void ApplyPressure_ReportsOnlyFirstThresholdCrossing()
        {
            PlayerVitalsModel model = new PlayerVitalsModel(100f, 100f, 10f);

            Assert.That(model.ApplyPressure(6f), Is.False);
            Assert.That(model.ApplyPressure(4f), Is.True);
            Assert.That(model.ApplyPressure(1f), Is.False);
        }

        [Test]
        public void RefillAndRecovery_ClampToConfiguredRanges()
        {
            PlayerVitalsModel model = new PlayerVitalsModel(100f, 10f, 20f);
            model.Tick(1f, 8f, 0f, 0f);
            model.ApplyPressure(20f);

            model.RefillBuzz(100f);
            model.RecoverFromKnockdown(0.25f);

            Assert.That(model.Buzz, Is.EqualTo(10f));
            Assert.That(model.Balance, Is.EqualTo(5f));
        }
    }
}
