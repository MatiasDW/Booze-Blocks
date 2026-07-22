using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoozeBlocks.Horde
{
    public sealed class KidSpatialGrid
    {
        private readonly Dictionary<long, List<KidUnit>> buckets = new Dictionary<long, List<KidUnit>>(128);
        private readonly Stack<List<KidUnit>> listPool = new Stack<List<KidUnit>>(128);
        private readonly float cellSize;

        public KidSpatialGrid(float cellSize)
        {
            this.cellSize = Math.Max(0.25f, cellSize);
        }

        public void Rebuild(IReadOnlyList<KidUnit> units)
        {
            foreach (KeyValuePair<long, List<KidUnit>> pair in buckets)
            {
                pair.Value.Clear();
                listPool.Push(pair.Value);
            }
            buckets.Clear();

            for (int i = 0; i < units.Count; i++)
            {
                KidUnit unit = units[i];
                if (unit == null || !unit.isActiveAndEnabled) continue;
                long key = GetKey(unit.Position);
                if (!buckets.TryGetValue(key, out List<KidUnit> bucket))
                {
                    bucket = listPool.Count > 0 ? listPool.Pop() : new List<KidUnit>(8);
                    buckets.Add(key, bucket);
                }
                bucket.Add(unit);
            }
        }

        public Vector3 CalculateSeparation(KidUnit self, float radius)
        {
            if (self == null || radius <= 0f) return Vector3.zero;

            Vector3 position = self.Position;
            float radiusSquared = radius * radius;
            int minX = Mathf.FloorToInt((position.x - radius) / cellSize);
            int maxX = Mathf.FloorToInt((position.x + radius) / cellSize);
            int minZ = Mathf.FloorToInt((position.z - radius) / cellSize);
            int maxZ = Mathf.FloorToInt((position.z + radius) / cellSize);
            Vector3 separation = Vector3.zero;

            for (int x = minX; x <= maxX; x++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (!buckets.TryGetValue(Pack(x, z), out List<KidUnit> bucket)) continue;
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        KidUnit other = bucket[i];
                        if (other == null || other == self) continue;
                        Vector3 offset = position - other.Position;
                        offset.y = 0f;
                        float distanceSquared = offset.sqrMagnitude;
                        if (distanceSquared <= 0.0001f || distanceSquared > radiusSquared) continue;
                        separation += offset / distanceSquared;
                    }
                }
            }

            return separation.normalized;
        }

        private long GetKey(Vector3 position)
        {
            return Pack(Mathf.FloorToInt(position.x / cellSize), Mathf.FloorToInt(position.z / cellSize));
        }

        private static long Pack(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
