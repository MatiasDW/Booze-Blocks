using UnityEngine;

namespace BoozeBlocks.Player
{
    [CreateAssetMenu(fileName = "PlayerVitalsConfig", menuName = "Booze & Blocks/Player Vitals")]
    public sealed class PlayerVitalsConfig : ScriptableObject
    {
        [field: SerializeField, Min(1f)] public float MaxHealth { get; private set; } = 150f;
        [field: SerializeField, Min(1f)] public float MaxBuzz { get; private set; } = 100f;
        [field: SerializeField, Min(1f)] public float MaxBalance { get; private set; } = 100f;
        [field: SerializeField, Min(0f)] public float BuzzDrainRate { get; private set; } = 3f;
        [field: SerializeField, Min(0f)] public float DryHealthDrainRate { get; private set; } = 4f;
        [field: SerializeField, Min(0f)] public float BalanceRecoveryRate { get; private set; } = 14f;
        [field: SerializeField, Range(0f, 1f)] public float BalanceAfterRecovery { get; private set; } = 0.25f;
    }
}
