using System;
using BoozeBlocks.Horde;
using UnityEngine;

namespace BoozeBlocks.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerInventory))]
    public sealed class PlayerDefenseController : MonoBehaviour
    {
        public event Action<int> Used;

        [SerializeField, Min(0.05f)] private float useCooldown = 0.55f;

        private PlayerInputReader input;
        private PlayerInventory inventory;
        private PlayerVitals vitals;
        private HordeDirector horde;
        private PlayerActionFeedback feedback;
        private float nextUseTime;
        private bool hasExecutionAuthority = true;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            inventory = GetComponent<PlayerInventory>();
            vitals = GetComponent<PlayerVitals>();
            feedback = GetComponent<PlayerActionFeedback>();
        }

        private void Update()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (inventory == null) inventory = GetComponent<PlayerInventory>();
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (feedback == null) feedback = GetComponent<PlayerActionFeedback>();
            if (!hasExecutionAuthority || input == null || inventory == null || !input.UseItemPressed) return;
            TryUseEquippedItem();
        }

        public bool TryUseEquippedItem()
        {
            if (Time.time < nextUseTime) return false;
            if (inventory.Model.DefenseItem == DefenseItemType.None || inventory.Model.DefenseUses <= 0)
            {
                feedback?.Show("No tienes defensa. Recoge una escoba o sarten con E.");
                return false;
            }

            if (horde == null) horde = FindAnyObjectByType<HordeDirector>();
            if (horde == null)
            {
                feedback?.Show("La horda aun no esta activa.");
                return false;
            }

            if (!inventory.Model.TryConsumeDefenseUse(out DefenseItemType item)) return false;

            float powerMultiplier = (1f + inventory.Model.DefensePowerLevel * 0.25f) *
                                    (vitals != null ? vitals.BuzzStrengthMultiplier : 1f);
            int affected = 0;
            int defeated = 0;
            switch (item)
            {
                case DefenseItemType.Broom:
                    affected = horde.ApplyDefenseSweep(transform.position, transform.forward, 3.8f, -0.15f,
                        2.8f * powerMultiplier, 0.45f, 1f * powerMultiplier, out defeated);
                    break;
                case DefenseItemType.FryingPan:
                    affected = horde.ApplyDefenseSweep(transform.position, transform.forward, 2.7f, 0.15f,
                        4.2f * powerMultiplier, 0.8f, 2f * powerMultiplier, out defeated);
                    break;
            }

            string itemName = item == DefenseItemType.Broom ? "Escoba" : "Sarten";
            feedback?.Show(affected > 0
                ? defeated > 0
                    ? $"{itemName}: {affected} golpeados, {defeated} fuera de la horda"
                    : $"{itemName}: {affected} golpeados"
                : $"{itemName}: no habia ninos al alcance");
            nextUseTime = Time.time + useCooldown;
            Used?.Invoke(affected);
            return true;
        }

        public void SetExecutionAuthority(bool isAuthoritative)
        {
            hasExecutionAuthority = isAuthoritative;
        }
    }
}
