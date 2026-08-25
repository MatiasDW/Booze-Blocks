using System;

namespace BoozeBlocks.Player
{
    public sealed class PlayerInventoryModel
    {
        public PlayerInventoryModel(int drinkCapacity = 2, int startingDrinks = 1)
        {
            DrinkCapacity = Math.Max(1, drinkCapacity);
            DrinkServings = Math.Clamp(startingDrinks, 0, DrinkCapacity);
        }

        public DefenseItemType DefenseItem { get; private set; }
        public int DefenseUses { get; private set; }
        public int DrinkServings { get; private set; }
        public int DrinkCapacity { get; private set; }
        public int DefensePowerLevel { get; private set; }

        public int StoreDrinks(int amount)
        {
            int accepted = Math.Clamp(amount, 0, DrinkCapacity - DrinkServings);
            DrinkServings += accepted;
            return accepted;
        }

        public bool TryConsumeDrink()
        {
            if (DrinkServings <= 0) return false;
            DrinkServings--;
            return true;
        }

        public void EquipDefense(DefenseItemType item, int uses)
        {
            DefenseItem = item;
            DefenseUses = Math.Max(0, uses);
            if (DefenseUses == 0) DefenseItem = DefenseItemType.None;
        }

        public bool TryConsumeDefenseUse(out DefenseItemType item)
        {
            item = DefenseItem;
            if (item == DefenseItemType.None || DefenseUses <= 0) return false;
            DefenseUses--;
            if (DefenseUses == 0) DefenseItem = DefenseItemType.None;
            return true;
        }

        public void UpgradeDrinkCapacity(int amount)
        {
            DrinkCapacity = Math.Max(1, DrinkCapacity + Math.Max(0, amount));
        }

        public void UpgradeDefensePower()
        {
            DefensePowerLevel++;
        }

        public void SetDrinkCapacityAtLeast(int capacity)
        {
            DrinkCapacity = Math.Max(DrinkCapacity, Math.Max(1, capacity));
        }

        public void SetDefensePowerLevelAtLeast(int level)
        {
            DefensePowerLevel = Math.Max(DefensePowerLevel, Math.Max(0, level));
        }

        public void ApplySnapshot(int drinkServings, int drinkCapacity, DefenseItemType defenseItem,
            int defenseUses, int defensePowerLevel)
        {
            DrinkCapacity = Math.Max(1, drinkCapacity);
            DrinkServings = Math.Clamp(drinkServings, 0, DrinkCapacity);
            DefenseUses = Math.Max(0, defenseUses);
            DefenseItem = DefenseUses > 0 ? defenseItem : DefenseItemType.None;
            DefensePowerLevel = Math.Max(0, defensePowerLevel);
        }
    }
}
