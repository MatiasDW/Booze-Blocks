using System;

namespace BoozeBlocks.Horde
{
    public sealed class HordeWaveModel
    {
        private readonly float activeDuration;
        private readonly float breakDuration;
        private readonly int wavesPerDifficultyStep;

        public HordeWaveModel(float activeDuration, float breakDuration, int wavesPerDifficultyStep)
        {
            this.activeDuration = Math.Max(0.1f, activeDuration);
            this.breakDuration = Math.Max(0.1f, breakDuration);
            this.wavesPerDifficultyStep = Math.Max(1, wavesPerDifficultyStep);
            WaveNumber = 1;
            IsWaveActive = true;
            RemainingTime = this.activeDuration;
        }

        public int WaveNumber { get; private set; }
        public int DifficultyStep => (WaveNumber - 1) / wavesPerDifficultyStep;
        public bool IsWaveActive { get; private set; }
        public float RemainingTime { get; private set; }

        public bool Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return false;

            bool phaseChanged = false;
            RemainingTime -= deltaTime;
            while (RemainingTime <= 0f)
            {
                float overflow = -RemainingTime;
                if (IsWaveActive)
                {
                    IsWaveActive = false;
                    RemainingTime = breakDuration;
                }
                else
                {
                    IsWaveActive = true;
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
