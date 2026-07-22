using System.Collections.Generic;
using UnityEngine;

namespace BoozeBlocks.Horde
{
    public static class HordeEntranceRegistry
    {
        private static List<HordeEntrance> entrances = new List<HordeEntrance>(4);
        private static List<HordeEntrance> Entrances => entrances ??= new List<HordeEntrance>(4);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            entrances = new List<HordeEntrance>(4);
        }

        public static void Register(HordeEntrance entrance)
        {
            if (entrance != null && !Entrances.Contains(entrance)) Entrances.Add(entrance);
        }

        public static void Unregister(HordeEntrance entrance)
        {
            Entrances.Remove(entrance);
        }

        public static bool TryGetSpawnPoint(int sequence, out Vector3 position)
        {
            RemoveMissing();
            int count = Entrances.Count;
            if (count == 0)
            {
                position = default;
                return false;
            }

            int start = Mathf.Abs(sequence) % count;
            for (int offset = 0; offset < count; offset++)
            {
                HordeEntrance entrance = Entrances[(start + offset) % count];
                if (!entrance.CanSpawn) continue;
                position = entrance.GetSpawnPosition(sequence);
                return true;
            }

            position = default;
            return false;
        }

        public static int OpenCount
        {
            get
            {
                RemoveMissing();
                int count = 0;
                for (int i = 0; i < Entrances.Count; i++)
                {
                    if (Entrances[i].CanSpawn) count++;
                }
                return count;
            }
        }

        public static bool HasEntrances
        {
            get
            {
                RemoveMissing();
                return Entrances.Count > 0;
            }
        }

        private static void RemoveMissing()
        {
            for (int i = Entrances.Count - 1; i >= 0; i--)
            {
                if (Entrances[i] == null) Entrances.RemoveAt(i);
            }
        }
    }
}
