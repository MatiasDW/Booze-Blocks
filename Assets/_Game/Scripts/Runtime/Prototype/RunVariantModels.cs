using System;
using System.Collections.Generic;

namespace BoozeBlocks.Prototype
{
    public enum RunCrisisType
    {
        WetCharcoal,
        WeakMix,
        DoubleBirthday,
        LooseFences
    }

    public enum RunUpgradeType
    {
        BiggerBottle,
        StrongerDefense,
        FastMix,
        GrillMaster,
        BetterDistractions
    }

    public static class RunVariantModels
    {
        public static RunCrisisType SelectCrisis(int seed)
        {
            int count = Enum.GetValues(typeof(RunCrisisType)).Length;
            return (RunCrisisType)(PositiveHash(seed) % count);
        }

        public static RunUpgradeType[] CreateUpgradeChoices(int seed, int tier)
        {
            List<RunUpgradeType> pool = new List<RunUpgradeType>((RunUpgradeType[])Enum.GetValues(typeof(RunUpgradeType)));
            RunUpgradeType[] result = new RunUpgradeType[3];
            int state = PositiveHash(seed ^ tier * 486187739);
            for (int i = 0; i < result.Length; i++)
            {
                state = PositiveHash(unchecked(state * 1103515245 + 12345));
                int index = state % pool.Count;
                result[i] = pool[index];
                pool.RemoveAt(index);
            }
            return result;
        }

        private static int PositiveHash(int value)
        {
            uint hash = unchecked((uint)value);
            hash ^= hash >> 16;
            hash *= 0x7feb352d;
            hash ^= hash >> 15;
            hash *= 0x846ca68b;
            hash ^= hash >> 16;
            return (int)(hash & 0x7fffffff);
        }
    }
}
