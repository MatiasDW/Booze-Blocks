using UnityEngine;

namespace BoozeBlocks.Horde
{
    [CreateAssetMenu(fileName = "HordeBalanceConfig", menuName = "Booze & Blocks/Horde Balance")]
    public sealed class HordeBalanceConfig : ScriptableObject
    {
        [field: SerializeField, Min(0)] public int BaseUnits { get; private set; } = 14;
        [field: SerializeField, Min(0)] public int UnitsPerAdditionalPlayer { get; private set; } = 6;
        [field: SerializeField, Min(0)] public int UnitsPerDifficultyStep { get; private set; } = 6;
        [field: SerializeField, Min(1)] public int MaximumUnits { get; private set; } = 96;
        [field: SerializeField, Min(1)] public int WavesPerDifficultyStep { get; private set; } = 2;
        [field: SerializeField, Min(1f)] public float WaveDuration { get; private set; } = 20f;
        [field: SerializeField, Min(0.1f)] public float BreakDuration { get; private set; } = 5f;
        [field: SerializeField, Min(0.1f)] public float RetargetInterval { get; private set; } = 0.65f;
        [field: SerializeField, Min(0f)] public float SpawnInterval { get; private set; } = 0.06f;
        [field: SerializeField, Min(0f)] public float FirstUnitPressure { get; private set; } = 9f;
        [field: SerializeField, Range(0f, 1f)] public float AdditionalUnitPressureMultiplier { get; private set; } = 0.7f;
        [field: SerializeField, Min(0f)] public float TargetCrowdPenalty { get; private set; } = 36f;
    }
}
