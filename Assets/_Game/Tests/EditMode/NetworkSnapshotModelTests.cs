using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using BoozeBlocks.Prototype;
using NUnit.Framework;

namespace BoozeBlocks.Tests.EditMode
{
    public sealed class NetworkSnapshotModelTests
    {
        [Test]
        public void Quantization_RoundTripsWithinNetworkBudget()
        {
            short position = NetworkSnapshotQuantization.EncodePosition(-21.347f);
            ushort yaw = NetworkSnapshotQuantization.EncodeYaw(359.4f);
            byte ratio = NetworkSnapshotQuantization.EncodeRatio(0.423f);
            ushort time = NetworkSnapshotQuantization.EncodeTime(149.96f);

            Assert.That(NetworkSnapshotQuantization.DecodePosition(position), Is.EqualTo(-21.35f).Within(0.011f));
            Assert.That(NetworkSnapshotQuantization.DecodeYaw(yaw), Is.EqualTo(359.4f).Within(0.01f));
            Assert.That(NetworkSnapshotQuantization.DecodeRatio(ratio), Is.EqualTo(0.423f).Within(0.005f));
            Assert.That(NetworkSnapshotQuantization.DecodeTime(time), Is.EqualTo(150f).Within(0.051f));
        }

        [Test]
        public void AuthoritativeSnapshots_ReplaceClientModels()
        {
            PlayerVitalsModel vitals = new PlayerVitalsModel(100f, 100f, 100f);
            vitals.ApplySnapshot(0.35f, 0.6f, 0.8f);
            Assert.That(vitals.Health, Is.EqualTo(35f).Within(0.001f));
            Assert.That(vitals.Buzz, Is.EqualTo(60f).Within(0.001f));
            Assert.That(vitals.Balance, Is.EqualTo(80f).Within(0.001f));

            PlayerInventoryModel inventory = new PlayerInventoryModel();
            inventory.ApplySnapshot(3, 4, DefenseItemType.FryingPan, 5, 2);
            Assert.That(inventory.DrinkServings, Is.EqualTo(3));
            Assert.That(inventory.DrinkCapacity, Is.EqualTo(4));
            Assert.That(inventory.DefenseItem, Is.EqualTo(DefenseItemType.FryingPan));
            Assert.That(inventory.DefenseUses, Is.EqualTo(5));
            Assert.That(inventory.DefensePowerLevel, Is.EqualTo(2));

            GrillTaskModel grill = new GrillTaskModel(5f, 8f, 6f);
            grill.ApplySnapshot(GrillTaskState.ReadyToServe, 2.4f);
            Assert.That(grill.State, Is.EqualTo(GrillTaskState.ReadyToServe));
            Assert.That(grill.Remaining, Is.EqualTo(2.4f).Within(0.001f));
        }
    }
}
