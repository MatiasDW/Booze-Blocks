using System;
using UnityEngine;

namespace BoozeBlocks.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerVitals : MonoBehaviour
    {
        [SerializeField] private PlayerVitalsConfig config;

        public event Action KnockedDown;
        public event Action Eliminated;

        private PlayerVitalsModel model;

        public PlayerVitalsModel Model
        {
            get
            {
                EnsureModel();
                return model;
            }
        }
        public float HealthRatio
        {
            get { return Model.Health / Model.MaxHealth; }
        }
        public float BuzzRatio
        {
            get { return Model.Buzz / Model.MaxBuzz; }
        }
        public float BalanceRatio
        {
            get { return Model.Balance / Model.MaxBalance; }
        }
        public float BalanceAfterRecovery => config != null ? config.BalanceAfterRecovery : 0.25f;

        private bool eliminationRaised;

        private void Awake()
        {
            EnsureModel();
        }

        private void EnsureModel()
        {
            if (model != null) return;
            float maxHealth = config != null ? config.MaxHealth : 100f;
            float maxBuzz = config != null ? config.MaxBuzz : 100f;
            float maxBalance = config != null ? config.MaxBalance : 100f;
            model = new PlayerVitalsModel(maxHealth, maxBuzz, maxBalance);
        }

        private void OnEnable()
        {
            PlayerRegistry.Register(this);
        }

        private void OnDisable()
        {
            PlayerRegistry.Unregister(this);
        }

        private void Update()
        {
            EnsureModel();
            float buzzDrain = config != null ? config.BuzzDrainRate : 3f;
            float dryHealthDrain = config != null ? config.DryHealthDrainRate : 8f;
            float balanceRecovery = config != null ? config.BalanceRecoveryRate : 14f;
            Model.Tick(Time.deltaTime, buzzDrain, dryHealthDrain, balanceRecovery);

            if (!eliminationRaised && Model.IsEliminated)
            {
                eliminationRaised = true;
                Eliminated?.Invoke();
                PlayerRegistry.NotifyStateChanged();
            }
        }

        public void ApplyPressure(float amount)
        {
            EnsureModel();
            if (Model.ApplyPressure(amount)) KnockedDown?.Invoke();
        }

        public void RefillBuzz(float amount)
        {
            EnsureModel();
            Model.RefillBuzz(amount);
        }

        public void RecoverBalance()
        {
            EnsureModel();
            Model.RecoverFromKnockdown(BalanceAfterRecovery);
        }
    }
}
