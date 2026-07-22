using System.Collections.Generic;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    public enum PrototypeRoundState
    {
        Playing,
        Won,
        Lost
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeRound : MonoBehaviour
    {
        private List<PlayerVitals> registeredPlayers = new List<PlayerVitals>(8);
        private List<PlayerVitals> survivingPlayers = new List<PlayerVitals>(8);
        public float RemainingTime { get; private set; }
        public PrototypeRoundState State { get; private set; } = PrototypeRoundState.Playing;
        public int RegisteredPlayerCount => registeredPlayers?.Count ?? 0;
        public int SurvivingPlayerCount => survivingPlayers?.Count ?? 0;

        public void Configure(float duration)
        {
            RemainingTime = Mathf.Max(1f, duration);
        }

        private void Update()
        {
            if (State != PrototypeRoundState.Playing) return;

            registeredPlayers ??= new List<PlayerVitals>(8);
            survivingPlayers ??= new List<PlayerVitals>(8);

            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            PlayerRegistry.Fill(registeredPlayers, true);
            PlayerRegistry.Fill(survivingPlayers, false);
            if (TeamRules.IsDefeated(registeredPlayers.Count, survivingPlayers.Count))
            {
                State = PrototypeRoundState.Lost;
            }
            else if (RemainingTime <= 0f)
            {
                State = PrototypeRoundState.Won;
            }
        }
    }
}
