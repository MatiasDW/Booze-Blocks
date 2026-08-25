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
            return TryGetSpawnPoint(sequence, 0f, out position);
        }

        public static bool TryGetSpawnPoint(int sequence, float hordeDamage, out Vector3 position)
        {
            RemoveMissing();
            int count = Entrances.Count;
            if (count == 0)
            {
                position = default;
                return false;
            }

            HordeEntrance entrance = Entrances[PositiveModulo(sequence, count)];
            if (!entrance.TryResolveSpawn(sequence, hordeDamage))
            {
                position = default;
                return false;
            }
            position = entrance.GetSpawnPosition(sequence);
            return true;
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

        public static float TotalFlow
        {
            get
            {
                RemoveMissing();
                float flow = 0f;
                for (int i = 0; i < Entrances.Count; i++) flow += Entrances[i].FlowRatio;
                return flow;
            }
        }

        public static void ApplyHordePressure(float damage)
        {
            if (damage <= 0f) return;
            RemoveMissing();
            for (int i = 0; i < Entrances.Count; i++)
            {
                if (Entrances[i].IsBlocked) Entrances[i].ApplyHordeAttack(damage);
            }
        }

        private static void RemoveMissing()
        {
            for (int i = Entrances.Count - 1; i >= 0; i--)
            {
                if (Entrances[i] == null) Entrances.RemoveAt(i);
            }
        }

        private static int PositiveModulo(int value, int divisor)
        {
            int remainder = value % divisor;
            return remainder < 0 ? remainder + divisor : remainder;
        }
    }
}
