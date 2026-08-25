using BoozeBlocks.Horde;
using NUnit.Framework;
using UnityEngine;

namespace BoozeBlocks.Tests
{
    public sealed class KidUnitHealthTests
    {
        [Test]
        public void DamageAccumulatesUntilKidIsDefeated()
        {
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                KidUnit kid = instance.AddComponent<KidUnit>();
                kid.Initialize(1, new KidSpatialGrid(2f));
                kid.Activate(Vector3.zero, null, 3f);

                Assert.That(kid.ReceiveHit(1f, Vector3.back, 1f, 0.1f), Is.False);
                Assert.That(kid.CurrentHealth, Is.EqualTo(2f));
                Assert.That(kid.HealthRatio, Is.EqualTo(2f / 3f).Within(0.001f));
                Assert.That(kid.ReceiveHit(2f, Vector3.back, 1f, 0.1f), Is.True);
                Assert.That(kid.CurrentHealth, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void PoolActivationRestoresMaximumHealth()
        {
            GameObject instance = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                KidUnit kid = instance.AddComponent<KidUnit>();
                kid.Initialize(2, new KidSpatialGrid(2f));
                kid.Activate(Vector3.zero, null, 3f);
                kid.ReceiveHit(2f, Vector3.back, 0f, 0f);
                kid.Deactivate();
                kid.Activate(Vector3.zero, null, 4.5f);

                Assert.That(kid.CurrentHealth, Is.EqualTo(4.5f));
                Assert.That(kid.MaximumHealth, Is.EqualTo(4.5f));
                Assert.That(kid.HealthRatio, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
