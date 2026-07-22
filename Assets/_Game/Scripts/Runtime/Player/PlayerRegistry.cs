using System;
using System.Collections.Generic;

namespace BoozeBlocks.Player
{
    public static class PlayerRegistry
    {
        private static List<PlayerVitals> players = new List<PlayerVitals>(8);

        private static List<PlayerVitals> Players => players ??= new List<PlayerVitals>(8);

        public static event Action Changed;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            players = new List<PlayerVitals>(8);
            Changed = null;
        }

        public static int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < Players.Count; i++)
                {
                    PlayerVitals player = Players[i];
                    if (player != null && !player.Model.IsEliminated) count++;
                }
                return count;
            }
        }

        public static void Register(PlayerVitals player)
        {
            if (player == null || Players.Contains(player)) return;
            Players.Add(player);
            Changed?.Invoke();
        }

        public static void Unregister(PlayerVitals player)
        {
            if (!Players.Remove(player)) return;
            Changed?.Invoke();
        }

        public static void NotifyStateChanged()
        {
            Changed?.Invoke();
        }

        public static void Fill(List<PlayerVitals> destination, bool includeEliminated)
        {
            destination.Clear();
            for (int i = Players.Count - 1; i >= 0; i--)
            {
                PlayerVitals player = Players[i];
                if (player == null)
                {
                    Players.RemoveAt(i);
                    continue;
                }

                if (includeEliminated || !player.Model.IsEliminated) destination.Add(player);
            }
        }
    }
}
