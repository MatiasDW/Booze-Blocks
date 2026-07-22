namespace BoozeBlocks.Prototype
{
    public static class TeamRules
    {
        public static bool IsDefeated(int registeredPlayers, int survivingPlayers)
        {
            return registeredPlayers > 0 && survivingPlayers <= 0;
        }
    }
}
