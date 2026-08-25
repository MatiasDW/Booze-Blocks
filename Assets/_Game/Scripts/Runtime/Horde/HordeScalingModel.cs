using System;

namespace BoozeBlocks.Horde
{
    public static class HordeScalingModel
    {
        public static int CalculateActiveUnits(int playerCount, int difficultyStep, int baseUnits,
            int unitsPerAdditionalPlayer, int unitsPerDifficultyStep, int maximumUnits)
        {
            if (playerCount <= 0 || maximumUnits <= 0) return 0;
            int safeBase = Math.Max(0, baseUnits);
            int safePlayerIncrement = Math.Max(0, unitsPerAdditionalPlayer);
            int safeWaveIncrement = Math.Max(0, unitsPerDifficultyStep);
            long requested = safeBase
                + (long)(playerCount - 1) * safePlayerIncrement
                + (long)Math.Max(0, difficultyStep) * safeWaveIncrement;
            return (int)Math.Min(Math.Max(0L, requested), maximumUnits);
        }

        public static float CalculatePressurePerSecond(int contactCount, float firstUnitPressure,
            float additionalUnitMultiplier)
        {
            if (contactCount <= 0 || firstUnitPressure <= 0f) return 0f;
            float additional = Math.Max(0f, additionalUnitMultiplier);
            return firstUnitPressure * (1f + (contactCount - 1) * additional);
        }

        public static int CalculateRampedUnits(int maximumForWave, float activeElapsedTime,
            float rampDuration)
        {
            if (maximumForWave <= 0 || activeElapsedTime <= 0f) return 0;
            if (rampDuration <= 0f) return maximumForWave;
            double ratio = Math.Min(1d, activeElapsedTime / rampDuration);
            return (int)Math.Min(maximumForWave, Math.Ceiling(maximumForWave * ratio));
        }
    }
}
