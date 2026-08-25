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
        [field: SerializeField, Min(0f)] public float InitialPreparationDuration { get; private set; } = 25f;
        [field: SerializeField, Min(1f)] public float WaveDuration { get; private set; } = 60f;
        [field: SerializeField, Min(0.1f)] public float BreakDuration { get; private set; } = 15f;
        [field: SerializeField, Min(0f)] public float SpawnRampDuration { get; private set; } = 20f;
        [field: SerializeField, Min(0.1f)] public float RetargetInterval { get; private set; } = 0.65f;
        [field: SerializeField, Min(0f)] public float SpawnInterval { get; private set; } = 0.10f;
        [field: SerializeField, Min(1f)] public float BaseKidHealth { get; private set; } = 3f;
        [field: SerializeField, Min(0f)] public float KidHealthPerDifficultyStep { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f)] public float BarricadeAttackDamage { get; private set; } = 4f;
        [field: SerializeField, Min(0f)] public float BarricadeAttackDamagePerDifficulty { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f)] public float BarricadePressureDamage { get; private set; } = 2f;
        [field: SerializeField, Min(0f)] public float BarricadePressureDamagePerDifficulty { get; private set; } = 0.25f;
        [field: SerializeField, Min(0f)] public float FirstUnitPressure { get; private set; } = 7f;
        [field: SerializeField, Range(0f, 1f)] public float AdditionalUnitPressureMultiplier { get; private set; } = 0.55f;
        [field: SerializeField, Min(0f)] public float FirstUnitHealthDamage { get; private set; } = 0.65f;
        [field: SerializeField, Range(0f, 1f)] public float AdditionalUnitHealthDamageMultiplier { get; private set; } = 0.50f;
        [field: SerializeField, Min(0f)] public float TargetCrowdPenalty { get; private set; } = 36f;
    }
}
