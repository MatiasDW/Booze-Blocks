using System;

namespace BoozeBlocks.Distractions
{
    public static class AttractionCapacityModel
    {
        public static int Calculate(int playerCount, int baseCapacity, int capacityPerAdditionalPlayer)
        {
            int safeBase = Math.Max(1, baseCapacity);
            if (playerCount <= 1) return safeBase;
            long result = safeBase + (long)(playerCount - 1) * Math.Max(0, capacityPerAdditionalPlayer);
            return (int)Math.Min(result, int.MaxValue);
        }
    }
}
