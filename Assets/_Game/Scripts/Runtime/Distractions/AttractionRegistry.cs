using System.Collections.Generic;
using UnityEngine;

namespace BoozeBlocks.Distractions
{
    public static class AttractionRegistry
    {
        private static List<AttractionSource> sources = new List<AttractionSource>();

        private static List<AttractionSource> Sources => sources ??= new List<AttractionSource>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sources = new List<AttractionSource>();
        }

        public static void Register(AttractionSource source)
        {
            if (source != null && !Sources.Contains(source)) Sources.Add(source);
        }

        public static void Unregister(AttractionSource source)
        {
            Sources.Remove(source);
        }

        public static AttractionSource ClaimBest(Vector3 position, int agentId)
        {
            AttractionSource best = null;
            int bestPriority = int.MinValue;
            float bestDistance = float.MaxValue;

            for (int i = Sources.Count - 1; i >= 0; i--)
            {
                AttractionSource source = Sources[i];
                if (source == null)
                {
                    Sources.RemoveAt(i);
                    continue;
                }

                if (!source.CanAffect(position)) continue;
                float distance = (source.transform.position - position).sqrMagnitude;
                if (!AttractionScoring.IsBetter(source.Priority, distance, bestPriority, bestDistance)) continue;
                if (!source.TryClaim(agentId)) continue;

                if (best != null) best.Release(agentId);
                best = source;
                bestPriority = source.Priority;
                bestDistance = distance;
            }

            return best;
        }
    }
}
