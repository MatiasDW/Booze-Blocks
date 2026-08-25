using System;

namespace BoozeBlocks.Horde
{
    public readonly struct BarricadeDamageResult
    {
        public BarricadeDamageResult(bool sectionBroken, bool destroyed)
        {
            SectionBroken = sectionBroken;
            Destroyed = destroyed;
        }

        public bool SectionBroken { get; }
        public bool Destroyed { get; }
    }

    public sealed class BarricadeIntegrityModel
    {
        private readonly int sectionCount;

        public BarricadeIntegrityModel(float maximumIntegrity, int sections)
        {
            MaximumIntegrity = Math.Max(1f, maximumIntegrity);
            sectionCount = Math.Max(1, sections);
        }

        public float MaximumIntegrity { get; }
        public float CurrentIntegrity { get; private set; }
        public bool IsActive => CurrentIntegrity > 0f;
        public float IntegrityRatio => CurrentIntegrity / MaximumIntegrity;
        public int RemainingSections => IsActive
            ? Math.Clamp((int)Math.Ceiling(IntegrityRatio * sectionCount), 1, sectionCount)
            : 0;
        public int DestroyedSections => sectionCount - RemainingSections;
        public float FlowRatio => IsActive ? DestroyedSections / (float)sectionCount : 1f;

        public void Restore()
        {
            CurrentIntegrity = MaximumIntegrity;
        }

        public BarricadeDamageResult ApplyDamage(float damage)
        {
            if (!IsActive || damage <= 0f) return default;
            int previousSections = RemainingSections;
            CurrentIntegrity = Math.Max(0f, CurrentIntegrity - damage);
            int currentSections = RemainingSections;
            return new BarricadeDamageResult(currentSections < previousSections, !IsActive);
        }

        public void ApplySnapshot(float integrityRatio)
        {
            CurrentIntegrity = Math.Clamp(integrityRatio, 0f, 1f) * MaximumIntegrity;
        }

        public bool AllowsSpawn(int sequence, int salt)
        {
            if (!IsActive) return true;
            float flow = FlowRatio;
            if (flow <= 0f) return false;
            uint hash = unchecked((uint)sequence * 747796405u + (uint)salt * 2891336453u + 277803737u);
            hash = (hash ^ (hash >> 16)) * 2246822519u;
            hash ^= hash >> 13;
            float sample = (hash & 0x00FFFFFFu) / 16777215f;
            return sample < flow;
        }
    }
}
