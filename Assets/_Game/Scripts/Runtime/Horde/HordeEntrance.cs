using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Horde
{
    [DisallowMultipleComponent]
    public sealed class HordeEntrance : MonoBehaviour, IInteractable
    {
        [SerializeField] private string barricadeName = "tablones";
        [SerializeField, Min(1f)] private float blockDuration = 14f;
        [SerializeField, Min(0f)] private float rebuildCooldown = 9f;

        private GameObject barricadeVisual;
        private float blockedUntil;
        private float rebuildAt;
        private bool lastVisualState;

        public bool IsBlocked => Time.time < blockedUntil;
        public bool CanSpawn => !IsBlocked;
        public float BlockRemaining => Mathf.Max(0f, blockedUntil - Time.time);
        public string Prompt => $"E - Bloquear con {barricadeName}";

        private void OnEnable()
        {
            HordeEntranceRegistry.Register(this);
        }

        private void OnDisable()
        {
            HordeEntranceRegistry.Unregister(this);
        }

        private void Update()
        {
            bool visible = IsBlocked;
            if (visible == lastVisualState) return;
            lastVisualState = visible;
            if (barricadeVisual != null) barricadeVisual.SetActive(visible);
        }

        public void Configure(string methodName, float duration, float cooldown, GameObject visual)
        {
            barricadeName = methodName;
            blockDuration = Mathf.Max(1f, duration);
            rebuildCooldown = Mathf.Max(0f, cooldown);
            barricadeVisual = visual;
            if (barricadeVisual != null) barricadeVisual.SetActive(false);
        }

        public bool CanInteract(PlayerVitals player)
        {
            return player != null && !IsBlocked && Time.time >= rebuildAt;
        }

        public void Interact(PlayerVitals player)
        {
            if (!CanInteract(player)) return;
            blockedUntil = Time.time + blockDuration;
            rebuildAt = blockedUntil + rebuildCooldown;
            lastVisualState = false;
        }

        public Vector3 GetSpawnPosition(int sequence)
        {
            float hash = Mathf.Repeat(sequence * 0.6180339f, 1f) * 2f - 1f;
            return transform.position + transform.right * (hash * 1.4f) + transform.forward * 0.8f;
        }
    }
}
