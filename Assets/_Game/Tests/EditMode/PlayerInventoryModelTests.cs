using BoozeBlocks.Player;
using NUnit.Framework;

namespace BoozeBlocks.Tests
{
    public sealed class PlayerInventoryModelTests
    {
        [Test]
        public void DrinksRespectCapacityAndCanBeConsumed()
        {
            PlayerInventoryModel inventory = new PlayerInventoryModel(2, 1);

            Assert.That(inventory.StoreDrinks(5), Is.EqualTo(1));
            Assert.That(inventory.DrinkServings, Is.EqualTo(2));
            Assert.That(inventory.TryConsumeDrink(), Is.True);
            Assert.That(inventory.DrinkServings, Is.EqualTo(1));
        }

        [Test]
        public void DefenseItemExpiresAfterItsConfiguredUses()
        {
            PlayerInventoryModel inventory = new PlayerInventoryModel();
            inventory.EquipDefense(DefenseItemType.Broom, 2);

            Assert.That(inventory.TryConsumeDefenseUse(out DefenseItemType first), Is.True);
            Assert.That(first, Is.EqualTo(DefenseItemType.Broom));
            Assert.That(inventory.TryConsumeDefenseUse(out DefenseItemType second), Is.True);
            Assert.That(second, Is.EqualTo(DefenseItemType.Broom));
            Assert.That(inventory.DefenseItem, Is.EqualTo(DefenseItemType.None));
            Assert.That(inventory.TryConsumeDefenseUse(out _), Is.False);
        }

        [Test]
        public void RunUpgradesAreIdempotent()
        {
            PlayerInventoryModel inventory = new PlayerInventoryModel();

            inventory.SetDrinkCapacityAtLeast(4);
            inventory.SetDrinkCapacityAtLeast(3);
            inventory.SetDefensePowerLevelAtLeast(2);
            inventory.SetDefensePowerLevelAtLeast(1);

            Assert.That(inventory.DrinkCapacity, Is.EqualTo(4));
            Assert.That(inventory.DefensePowerLevel, Is.EqualTo(2));
        }
    }
}
