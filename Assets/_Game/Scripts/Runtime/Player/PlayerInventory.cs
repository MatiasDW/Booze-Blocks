using System;
using UnityEngine;

namespace BoozeBlocks.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerVitals))]
    public sealed class PlayerInventory : MonoBehaviour
    {
        public event Action Drank;

        [SerializeField, Min(0f)] private float buzzPerServing = 38f;

        private PlayerInputReader input;
        private PlayerVitals vitals;
        private PlayerActionFeedback feedback;
        private PlayerInventoryModel model;
        private bool hasExecutionAuthority = true;

        public PlayerInventoryModel Model => model ??= new PlayerInventoryModel();

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            vitals = GetComponent<PlayerVitals>();
            feedback = GetComponent<PlayerActionFeedback>();
            _ = Model;
        }

        private void Update()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (feedback == null) feedback = GetComponent<PlayerActionFeedback>();
            if (!hasExecutionAuthority || input == null || vitals == null || !input.DrinkPressed) return;
            TryDrink();
        }

        public bool TryDrink()
        {
            if (vitals.BuzzRatio >= 0.999f)
            {
                feedback?.Show("BUZZ lleno: guarda la botella para despues.");
                return false;
            }
            if (!Model.TryConsumeDrink())
            {
                feedback?.Show("Botella vacia: prepara y rellena booze en el BOOZE LAB.");
                return false;
            }
            vitals.RefillBuzz(buzzPerServing);
            Drank?.Invoke();
            feedback?.Show($"Bebiste booze. Quedan {Model.DrinkServings} cargas.");
            return true;
        }

        public int StoreDrinks(int amount)
        {
            return Model.StoreDrinks(amount);
        }

        public void EquipDefense(DefenseItemType item, int uses)
        {
            Model.EquipDefense(item, uses);
        }

        public void SetExecutionAuthority(bool isAuthoritative)
        {
            hasExecutionAuthority = isAuthoritative;
        }
    }
}
