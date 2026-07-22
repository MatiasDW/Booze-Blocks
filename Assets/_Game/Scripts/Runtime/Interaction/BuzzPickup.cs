using System.Collections;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Interaction
{
    [DisallowMultipleComponent]
    public sealed class BuzzPickup : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(0f)] private float amount = 45f;
        [SerializeField, Min(0f)] private float respawnDelay = 9f;

        private Collider pickupCollider;
        private Renderer pickupRenderer;
        private WaitForSeconds respawnWait;
        private bool available = true;

        public string Prompt => "E - Tomar bebida";

        private void Awake()
        {
            pickupCollider = GetComponent<Collider>();
            pickupRenderer = GetComponentInChildren<Renderer>();
            respawnWait = new WaitForSeconds(respawnDelay);
        }

        public bool CanInteract(PlayerVitals player)
        {
            return available && player != null && player.BuzzRatio < 0.999f;
        }

        public void Interact(PlayerVitals player)
        {
            if (!CanInteract(player)) return;
            player.RefillBuzz(amount);
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            available = false;
            if (pickupCollider != null) pickupCollider.enabled = false;
            if (pickupRenderer != null) pickupRenderer.enabled = false;
            yield return respawnWait;
            if (pickupCollider != null) pickupCollider.enabled = true;
            if (pickupRenderer != null) pickupRenderer.enabled = true;
            available = true;
        }
    }
}
