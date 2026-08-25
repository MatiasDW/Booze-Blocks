namespace BoozeBlocks.Prototype
{
    public static class RunLaunchOptions
    {
        private static int nextSeed;
        private static bool hasSeed;
        private static bool autoStart;

        public static void Prepare(int seed, bool startImmediately)
        {
            nextSeed = seed;
            hasSeed = seed != 0;
            autoStart = startImmediately;
        }

        public static int ConsumeSeed()
        {
            int seed = hasSeed ? nextSeed : 0;
            hasSeed = false;
            nextSeed = 0;
            return seed;
        }

        public static bool ConsumeAutoStart()
        {
            bool result = autoStart;
            autoStart = false;
            return result;
        }

        public static int ResolveSimulatedPlayerCount(int fallback)
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length - 1; i++)
            {
                if (!string.Equals(arguments[i], "-boozePlayers", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (int.TryParse(arguments[i + 1], out int count)) return UnityEngine.Mathf.Clamp(count, 1, 8);
            }
            return UnityEngine.Mathf.Clamp(fallback, 1, 8);
        }

        public static bool HasAutoStartArgument()
        {
            string[] arguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], "-boozeAutoStart", System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
