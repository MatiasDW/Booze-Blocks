using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Interaction
{
    public enum DrinkStationState
    {
        Idle,
        Preparing,
        Ready
    }

    [DisallowMultipleComponent]
    public sealed class DrinkPreparationStation : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(0.1f)] private float preparationDuration = 9f;
        [SerializeField, Min(1)] private int servingsPerBatch = 4;

        private float readyAt;
        private float preparationMultiplier = 1f;
        private int servingsReady;
        private Renderer stateIndicator;
        private MaterialPropertyBlock indicatorProperties;

        public DrinkStationState State { get; private set; }
        public float RemainingPreparation => State == DrinkStationState.Preparing
            ? Mathf.Max(0f, readyAt - Time.time)
            : 0f;
        public int ServingsReady => servingsReady;
        public string Prompt
        {
            get
            {
                if (State == DrinkStationState.Ready) return $"E - Llenar botella ({servingsReady})";
                if (State == DrinkStationState.Preparing) return $"Preparando... {Mathf.CeilToInt(RemainingPreparation)}s";
                return "E - Preparar mezcla";
            }
        }

        private void Update()
        {
            if (State == DrinkStationState.Preparing && Time.time >= readyAt)
            {
                State = DrinkStationState.Ready;
                servingsReady = servingsPerBatch;
            }
            RefreshIndicator();
        }

        public void Configure(float duration, int batchServings)
        {
            preparationDuration = Mathf.Max(0.1f, duration);
            servingsPerBatch = Mathf.Max(1, batchServings);
        }

        public void ConfigureVisual(Renderer indicator)
        {
            stateIndicator = indicator;
            indicatorProperties = new MaterialPropertyBlock();
            RefreshIndicator();
        }

        public void SetPreparationMultiplier(float multiplier)
        {
            preparationMultiplier = Mathf.Clamp(multiplier, 0.25f, 2f);
        }

        public bool CanInteract(PlayerVitals player)
        {
            if (player == null || !player.TryGetComponent(out PlayerInventory inventory)) return false;
            if (State == DrinkStationState.Idle) return true;
            return State == DrinkStationState.Ready && inventory.Model.DrinkServings < inventory.Model.DrinkCapacity;
        }

        public void Interact(PlayerVitals player)
        {
            if (!CanInteract(player)) return;
            if (State == DrinkStationState.Idle)
            {
                State = DrinkStationState.Preparing;
                readyAt = Time.time + preparationDuration * preparationMultiplier;
                return;
            }

            PlayerInventory inventory = player.GetComponent<PlayerInventory>();
            int stored = inventory.StoreDrinks(servingsReady);
            servingsReady -= stored;
            if (servingsReady <= 0) State = DrinkStationState.Idle;
        }

        private void RefreshIndicator()
        {
            if (stateIndicator == null) return;
            indicatorProperties ??= new MaterialPropertyBlock();
            Color color = State switch
            {
                DrinkStationState.Preparing => Color.Lerp(new Color(1f, 0.28f, 0.03f),
                    new Color(1f, 0.85f, 0.08f), Mathf.PingPong(Time.time * 2.5f, 1f)),
                DrinkStationState.Ready => new Color(0.15f, 1f, 0.32f),
                _ => new Color(0.15f, 0.65f, 1f)
            };
            ApplyIndicatorColor(color);
        }

        private void ApplyIndicatorColor(Color color)
        {
            stateIndicator.GetPropertyBlock(indicatorProperties);
            indicatorProperties.SetColor("_Color", color);
            stateIndicator.SetPropertyBlock(indicatorProperties);
        }
    }
}
