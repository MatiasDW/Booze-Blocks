using System.Collections;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Interaction
{
    [DisallowMultipleComponent]
    public sealed class DefensePickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private DefenseItemType itemType = DefenseItemType.Broom;
        [SerializeField, Min(1)] private int uses = 8;
        [SerializeField, Min(0f)] private float respawnDelay = 18f;

        private Collider pickupCollider;
        private Renderer[] pickupRenderers;
        private bool available = true;

        public DefenseItemType ItemType => itemType;
        public int Uses => uses;
        public string Prompt => itemType == DefenseItemType.FryingPan
            ? "E - Recoger sarten"
            : "E - Recoger escoba";

        private void Awake()
        {
            pickupCollider = GetComponent<Collider>();
            pickupRenderers = GetComponentsInChildren<Renderer>(true);
        }

        public void Configure(DefenseItemType type, int itemUses, float itemRespawnDelay)
        {
            itemType = type;
            uses = Mathf.Max(1, itemUses);
            respawnDelay = Mathf.Max(0f, itemRespawnDelay);
        }

        public bool CanInteract(PlayerVitals player)
        {
            return available && player != null && player.TryGetComponent(out PlayerInventory _);
        }

        public void Interact(PlayerVitals player)
        {
            if (!CanInteract(player)) return;
            player.GetComponent<PlayerInventory>().EquipDefense(itemType, uses);
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            available = false;
            SetVisible(false);
            if (respawnDelay > 0f) yield return new WaitForSeconds(respawnDelay);
            SetVisible(true);
            available = true;
        }

        private void SetVisible(bool visible)
        {
            if (pickupCollider != null) pickupCollider.enabled = visible;
            if (pickupRenderers == null) return;
            for (int i = 0; i < pickupRenderers.Length; i++) pickupRenderers[i].enabled = visible;
        }
    }
}
