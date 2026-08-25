using System;
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
        public float Duration { get; private set; }
        public float ElapsedTime => Mathf.Max(0f, Duration - RemainingTime);
        public PrototypeRoundState State { get; private set; } = PrototypeRoundState.Playing;
        public int RegisteredPlayerCount => registeredPlayers?.Count ?? 0;
        public int SurvivingPlayerCount => survivingPlayers?.Count ?? 0;
        public bool HasSimulationAuthority { get; private set; } = true;

        public event Action<PrototypeRoundState> Finished;

        public void Configure(float duration)
        {
            Duration = Mathf.Max(1f, duration);
            RemainingTime = Duration;
            State = PrototypeRoundState.Playing;
        }

        private void Update()
        {
            if (!HasSimulationAuthority || State != PrototypeRoundState.Playing) return;

            registeredPlayers ??= new List<PlayerVitals>(8);
            survivingPlayers ??= new List<PlayerVitals>(8);

            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            PlayerRegistry.Fill(registeredPlayers, true);
            PlayerRegistry.Fill(survivingPlayers, false);
            if (TeamRules.IsDefeated(registeredPlayers.Count, survivingPlayers.Count))
            {
                EndRound(false);
            }
            else if (RemainingTime <= 0f)
            {
                EndRound(true);
            }
        }

        public void EndRound(bool won)
        {
            if (State != PrototypeRoundState.Playing) return;
            State = won ? PrototypeRoundState.Won : PrototypeRoundState.Lost;
            Finished?.Invoke(State);
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            HasSimulationAuthority = isAuthoritative;
        }

        public void ApplyRemoteState(float remainingTime, PrototypeRoundState state)
        {
            if (HasSimulationAuthority) return;
            RemainingTime = Mathf.Clamp(remainingTime, 0f, Duration);
            if (State == state) return;
            State = state;
            if (State != PrototypeRoundState.Playing) Finished?.Invoke(State);
        }
    }
}
