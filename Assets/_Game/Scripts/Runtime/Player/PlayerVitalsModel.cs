using System;

namespace BoozeBlocks.Player
{
    public sealed class PlayerVitalsModel
    {
        private bool knockdownLatched;

        public PlayerVitalsModel(float maxHealth, float maxBuzz, float maxBalance)
        {
            if (maxHealth <= 0f) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (maxBuzz <= 0f) throw new ArgumentOutOfRangeException(nameof(maxBuzz));
            if (maxBalance <= 0f) throw new ArgumentOutOfRangeException(nameof(maxBalance));

            MaxHealth = maxHealth;
            MaxBuzz = maxBuzz;
            MaxBalance = maxBalance;
            Health = maxHealth;
            Buzz = maxBuzz;
        }

        public float MaxHealth { get; }
        public float MaxBuzz { get; }
        public float MaxBalance { get; }
        public float Health { get; private set; }
        public float Buzz { get; private set; }
        public float Balance { get; private set; }
        public bool IsEliminated => Health <= 0f;

        public void Tick(float deltaTime, float buzzDrainRate, float dryHealthDrainRate, float balanceRecoveryRate)
        {
            if (deltaTime <= 0f || IsEliminated) return;

            Buzz = Clamp(Buzz - Math.Max(0f, buzzDrainRate) * deltaTime, 0f, MaxBuzz);
            if (Buzz <= 0f)
            {
                Health = Clamp(Health - Math.Max(0f, dryHealthDrainRate) * deltaTime, 0f, MaxHealth);
            }

            Balance = Clamp(Balance - Math.Max(0f, balanceRecoveryRate) * deltaTime, 0f, MaxBalance);
        }

        public bool ApplyPressure(float amount)
        {
            if (amount <= 0f || IsEliminated || knockdownLatched) return false;

            Balance = Clamp(Balance + amount, 0f, MaxBalance);
            if (Balance < MaxBalance) return false;

            knockdownLatched = true;
            return true;
        }

        public void RefillBuzz(float amount)
        {
            if (amount <= 0f || IsEliminated) return;
            Buzz = Clamp(Buzz + amount, 0f, MaxBuzz);
        }

        public bool ApplyDamage(float amount)
        {
            if (amount <= 0f || IsEliminated) return false;
            Health = Clamp(Health - amount, 0f, MaxHealth);
            return IsEliminated;
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsEliminated) return;
            Health = Clamp(Health + amount, 0f, MaxHealth);
        }

        public void RecoverFromKnockdown(float retainedBalanceRatio)
        {
            Balance = MaxBalance * Clamp(retainedBalanceRatio, 0f, 1f);
            knockdownLatched = false;
        }

        public void ApplySnapshot(float healthRatio, float buzzRatio, float balanceRatio)
        {
            Health = MaxHealth * Clamp(healthRatio, 0f, 1f);
            Buzz = MaxBuzz * Clamp(buzzRatio, 0f, 1f);
            Balance = MaxBalance * Clamp(balanceRatio, 0f, 1f);
            knockdownLatched = Balance >= MaxBalance;
        }

        private static float Clamp(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }
    }
}
