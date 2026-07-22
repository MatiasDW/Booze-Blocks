namespace BoozeBlocks.Distractions
{
    public static class AttractionScoring
    {
        public static bool IsBetter(int candidatePriority, float candidateDistanceSquared,
            int currentPriority, float currentDistanceSquared)
        {
            return candidatePriority > currentPriority ||
                   candidatePriority == currentPriority && candidateDistanceSquared < currentDistanceSquared;
        }
    }
}
