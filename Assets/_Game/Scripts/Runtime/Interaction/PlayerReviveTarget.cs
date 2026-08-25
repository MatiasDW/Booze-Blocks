using System;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerVitals), typeof(PlayerStateMachine))]
    public sealed class PlayerReviveTarget : MonoBehaviour, IInteractable
    {
        private PlayerVitals target;
        private PlayerStateMachine stateMachine;

        public event Action Revived;

        public string Prompt => "E - Levantar companero";
        public bool IsWaitingForRescue => stateMachine != null && stateMachine.CanBeRevived;

        private void Awake()
        {
            target = GetComponent<PlayerVitals>();
            stateMachine = GetComponent<PlayerStateMachine>();
        }

        public bool CanInteract(PlayerVitals player)
        {
            return player != null && player != target && stateMachine != null && stateMachine.CanBeRevived;
        }

        public void Interact(PlayerVitals player)
        {
            if (!CanInteract(player) || !stateMachine.TryRevive()) return;
            Revived?.Invoke();
        }
    }
}
