using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerVitals))]
    public sealed class PlayerInteraction : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float radius = 3.25f;
        [SerializeField, Min(0.02f)] private float scanInterval = 0.1f;
        [SerializeField] private LayerMask interactionLayers = ~(1 << 2); // Exclude the horde's Ignore Raycast layer.

        private readonly Collider[] hits = new Collider[24];
        private PlayerInputReader input;
        private PlayerVitals vitals;
        private PlayerActionFeedback feedback;
        private IInteractable current;
        private float nextScanTime;

        public string CurrentPrompt => current != null && current.CanInteract(vitals) ? current.Prompt : string.Empty;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>();
            vitals = GetComponent<PlayerVitals>();
            feedback = GetComponent<PlayerActionFeedback>();
        }

        private void Update()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (vitals == null) vitals = GetComponent<PlayerVitals>();
            if (feedback == null) feedback = GetComponent<PlayerActionFeedback>();
            if (input == null || vitals == null) return;

            bool interactPressed = input.InteractPressed;
            if (interactPressed || Time.unscaledTime >= nextScanTime)
            {
                current = FindClosestInteractable();
                nextScanTime = Time.unscaledTime + scanInterval;
            }

            if (interactPressed) TryInteract();
        }

        public bool TryInteract()
        {
            current = FindClosestInteractable();
            if (current != null && current.CanInteract(vitals))
            {
                string action = current.Prompt.Replace("E - ", string.Empty);
                current.Interact(vitals);
                feedback?.Show($"{action}: listo");
                current = FindClosestInteractable();
                return true;
            }

            feedback?.Show("No hay nada interactuable cerca. Acercate hasta ver el aviso E.");
            return false;
        }

        private IInteractable FindClosestInteractable()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, radius, hits,
                interactionLayers, QueryTriggerInteraction.Collide);
            IInteractable closest = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = hits[i].GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract(vitals)) continue;

                Vector3 closestPoint = hits[i].ClosestPoint(transform.position);
                float distance = (closestPoint - transform.position).sqrMagnitude;
                if (distance >= closestDistance) continue;
                closest = candidate;
                closestDistance = distance;
            }

            return closest;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
