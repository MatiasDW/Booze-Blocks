using System;

namespace BoozeBlocks.Horde
{
    public enum HordeWavePhase
    {
        Preparation,
        Active,
        Break
    }

    public sealed class HordeWaveModel
    {
        private readonly float activeDuration;
        private readonly float breakDuration;
        private readonly int wavesPerDifficultyStep;

        public HordeWaveModel(float activeDuration, float breakDuration, int wavesPerDifficultyStep,
            float initialPreparationDuration = 0f)
        {
            this.activeDuration = Math.Max(0.1f, activeDuration);
            this.breakDuration = Math.Max(0.1f, breakDuration);
            this.wavesPerDifficultyStep = Math.Max(1, wavesPerDifficultyStep);
            WaveNumber = 1;
            Phase = initialPreparationDuration > 0f ? HordeWavePhase.Preparation : HordeWavePhase.Active;
            RemainingTime = Phase == HordeWavePhase.Preparation
                ? initialPreparationDuration
                : this.activeDuration;
        }

        public int WaveNumber { get; private set; }
        public int DifficultyStep => (WaveNumber - 1) / wavesPerDifficultyStep;
        public HordeWavePhase Phase { get; private set; }
        public bool IsWaveActive => Phase == HordeWavePhase.Active;
        public float ActiveElapsedTime => IsWaveActive ? Math.Max(0f, activeDuration - RemainingTime) : 0f;
        public float RemainingTime { get; private set; }

        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return false;

            bool phaseChanged = false;
            RemainingTime -= deltaTime;
            while (RemainingTime <= 0f)
            {
                float overflow = -RemainingTime;
                if (Phase == HordeWavePhase.Preparation)
                {
                    Phase = HordeWavePhase.Active;
                    RemainingTime = activeDuration;
                }
                else if (Phase == HordeWavePhase.Active)
                {
                    Phase = HordeWavePhase.Break;
                    RemainingTime = breakDuration;
                }
                else
                {
                    Phase = HordeWavePhase.Active;
                    WaveNumber++;
                    RemainingTime = activeDuration;
                }

                RemainingTime -= overflow;
                phaseChanged = true;
            }

            return phaseChanged;
        }
    }
}
