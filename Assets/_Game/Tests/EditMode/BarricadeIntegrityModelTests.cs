using BoozeBlocks.Horde;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class BarricadeIntegrityModelTests
    {
        [Test]
        public void DamageBreaksSectionsAndProgressivelyOpensFlow()
        {
            BarricadeIntegrityModel barrier = new BarricadeIntegrityModel(120f, 3);
            barrier.Restore();

            Assert.That(barrier.IntegrityRatio, Is.EqualTo(1f));
            Assert.That(barrier.RemainingSections, Is.EqualTo(3));
            Assert.That(barrier.FlowRatio, Is.Zero);

            BarricadeDamageResult first = barrier.ApplyDamage(41f);
            Assert.That(first.SectionBroken, Is.True);
            Assert.That(first.Destroyed, Is.False);
            Assert.That(barrier.RemainingSections, Is.EqualTo(2));
            Assert.That(barrier.FlowRatio, Is.EqualTo(1f / 3f).Within(0.001f));

            BarricadeDamageResult second = barrier.ApplyDamage(40f);
            Assert.That(second.SectionBroken, Is.True);
            Assert.That(barrier.RemainingSections, Is.EqualTo(1));
            Assert.That(barrier.FlowRatio, Is.EqualTo(2f / 3f).Within(0.001f));

            BarricadeDamageResult final = barrier.ApplyDamage(100f);
            Assert.That(final.Destroyed, Is.True);
            Assert.That(barrier.RemainingSections, Is.Zero);
            Assert.That(barrier.FlowRatio, Is.EqualTo(1f));
        }

        [Test]
        public void SnapshotAndRestoreClampIntegrity()
        {
            BarricadeIntegrityModel barrier = new BarricadeIntegrityModel(80f, 2);

            barrier.ApplySnapshot(0.5f);
            Assert.That(barrier.CurrentIntegrity, Is.EqualTo(40f));
            Assert.That(barrier.FlowRatio, Is.EqualTo(0.5f));
            barrier.ApplySnapshot(-2f);
            Assert.That(barrier.IsActive, Is.False);
            barrier.Restore();
            Assert.That(barrier.CurrentIntegrity, Is.EqualTo(80f));
        }

        [Test]
        public void IntactBarrierRejectsSpawnsAndDestroyedBarrierAllowsAll()
        {
            BarricadeIntegrityModel barrier = new BarricadeIntegrityModel(60f, 3);
            barrier.Restore();

            for (int i = 0; i < 20; i++) Assert.That(barrier.AllowsSpawn(i, 7), Is.False);
            barrier.ApplyDamage(60f);
            for (int i = 0; i < 20; i++) Assert.That(barrier.AllowsSpawn(i, 7), Is.True);
        }
    }
}
