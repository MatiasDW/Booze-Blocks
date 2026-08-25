using System;
using System.Collections.Generic;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Distractions
{
    [DisallowMultipleComponent]
    public sealed class AttractionSource : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "E - Activar distraccion";
        [SerializeField, Min(0.1f)] private float radius = 9f;
        [SerializeField, Min(0.1f)] private float duration = 7f;
        [SerializeField, Min(1)] private int capacity = 12;
        [SerializeField, Min(0)] private int capacityPerAdditionalPlayer;
        [SerializeField] private int priority = 10;

        [SerializeField, Min(0f)] private float cooldown;

        private HashSet<int> claims = new HashSet<int>();
        private float activeUntil;
        private float cooldownUntil;
        private bool interactionEnabled = true;
        private float durationMultiplier = 1f;
        private Vector3 baseScale;
        private bool hasSimulationAuthority = true;

        public event Action Activated;

        public string Prompt => prompt;
        public bool IsActive => Time.time < activeUntil;
        public bool IsInteractionEnabled => interactionEnabled;
        public float CooldownRemaining => Mathf.Max(0f, cooldownUntil - Time.time);
        public float ActiveRemaining => Mathf.Max(0f, activeUntil - Time.time);
        public bool IsReady => interactionEnabled && !IsActive && CooldownRemaining <= 0f;
        public float Radius => radius;
        public int Priority => priority;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            claims ??= new HashSet<int>();
            AttractionRegistry.Register(this);
        }

        private void OnDisable()
        {
            AttractionRegistry.Unregister(this);
            claims?.Clear();
            if (baseScale.sqrMagnitude > 0f) transform.localScale = baseScale;
        }

        private void Update()
        {
            claims ??= new HashSet<int>();
            if (!IsActive && claims.Count > 0) claims.Clear();
            float pulse = IsActive ? 1f + Mathf.Sin(Time.unscaledTime * 8f) * 0.055f : 1f;
            transform.localScale = baseScale * pulse;
        }

        public void Configure(string interactionPrompt, int sourcePriority, float sourceRadius,
            float sourceDuration, int sourceCapacity, int sourceCapacityPerAdditionalPlayer = 0,
            float sourceCooldown = 0f)
        {
            prompt = interactionPrompt;
            priority = sourcePriority;
            radius = sourceRadius;
            duration = sourceDuration;
            capacity = sourceCapacity;
            capacityPerAdditionalPlayer = sourceCapacityPerAdditionalPlayer;
            cooldown = Mathf.Max(0f, sourceCooldown);
        }

        public void SetInteractionEnabled(bool enabled)
        {
            interactionEnabled = enabled;
        }

        public void SetDurationMultiplier(float multiplier)
        {
            durationMultiplier = Mathf.Clamp(multiplier, 0.25f, 2f);
        }

        public bool CanInteract(PlayerVitals player)
        {
            return IsReady;
        }

        public void Interact(PlayerVitals player)
        {
            if (!hasSimulationAuthority || !CanInteract(player)) return;
            activeUntil = Time.time + duration * durationMultiplier;
            cooldownUntil = activeUntil + cooldown;
            claims ??= new HashSet<int>();
            claims.Clear();
            Activated?.Invoke();
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            hasSimulationAuthority = isAuthoritative;
        }

        public void ApplyRemoteState(bool enabled, float activeRemaining, float cooldownRemaining)
        {
            if (hasSimulationAuthority) return;
            interactionEnabled = enabled;
            activeUntil = Time.time + Mathf.Max(0f, activeRemaining);
            cooldownUntil = Time.time + Mathf.Max(activeRemaining, cooldownRemaining);
        }

        public bool CanAffect(Vector3 position)
        {
            return IsActive && (transform.position - position).sqrMagnitude <= radius * radius;
        }

        public bool TryClaim(int agentId)
        {
            if (!IsActive) return false;
            if (claims.Contains(agentId)) return true;
            int effectiveCapacity = AttractionCapacityModel.Calculate(PlayerRegistry.ActiveCount,
                capacity, capacityPerAdditionalPlayer);
            if (claims.Count >= effectiveCapacity) return false;
            claims.Add(agentId);
            return true;
        }

        public void Release(int agentId)
        {
            claims.Remove(agentId);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
