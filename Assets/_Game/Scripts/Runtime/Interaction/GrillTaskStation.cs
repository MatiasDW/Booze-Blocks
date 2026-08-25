using System;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Interaction
{
    [DisallowMultipleComponent]
    public sealed class GrillTaskStation : MonoBehaviour, IInteractable
    {
        public const float ServingHealthRecovery = 25f;

        private GrillTaskModel model;
        private float taskSpeedMultiplier = 1f;
        private Renderer heatIndicator;
        private Renderer[] foodRenderers = Array.Empty<Renderer>();
        private MaterialPropertyBlock properties;
        private bool hasSimulationAuthority = true;

        public event Action Served;
        public event Action Burned;

        public GrillTaskState State => Model.State;
        public float Remaining => Model.Remaining;
        public string Prompt => State switch
        {
            GrillTaskState.NeedsFuel => "E - Encender carbon",
            GrillTaskState.Heating => $"Calentando parrilla... {Mathf.CeilToInt(Remaining)}s",
            GrillTaskState.ReadyToCook => "E - Poner carne",
            GrillTaskState.Cooking => $"Cocinando... {Mathf.CeilToInt(Remaining)}s",
            GrillTaskState.ReadyToServe => $"E - Servir asado ({Mathf.CeilToInt(Remaining)}s antes de quemarse)",
            _ => "E - Limpiar carne quemada"
        };

        private GrillTaskModel Model => model ??= new GrillTaskModel(5f, 8f, 6f);

        private void Update()
        {
            GrillTaskState previous = State;
            if (hasSimulationAuthority && Model.Tick(Time.deltaTime) && State == GrillTaskState.Burned && previous != State)
            {
                Burned?.Invoke();
            }
            RefreshVisuals();
        }

        public void Configure(float heatingDuration, float cookingDuration, float burnDuration,
            Renderer indicator, Renderer[] food)
        {
            model = new GrillTaskModel(heatingDuration, cookingDuration, burnDuration);
            heatIndicator = indicator;
            foodRenderers = food ?? Array.Empty<Renderer>();
            properties = new MaterialPropertyBlock();
            RefreshVisuals();
        }

        public void SetTaskSpeedMultiplier(float multiplier)
        {
            taskSpeedMultiplier = Mathf.Clamp(multiplier, 0.25f, 2f);
        }

        public bool CanInteract(PlayerVitals player)
        {
            return player != null && State is GrillTaskState.NeedsFuel or GrillTaskState.ReadyToCook
                or GrillTaskState.ReadyToServe or GrillTaskState.Burned;
        }

        public void Interact(PlayerVitals player)
        {
            if (!hasSimulationAuthority || !CanInteract(player)) return;
            GrillTaskState previous = State;
            if (!Model.TryInteract(taskSpeedMultiplier)) return;
            if (previous == GrillTaskState.ReadyToServe)
            {
                player.Heal(ServingHealthRecovery);
                Served?.Invoke();
            }
            RefreshVisuals();
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            hasSimulationAuthority = isAuthoritative;
        }

        public void ApplyRemoteState(GrillTaskState state, float remaining)
        {
            if (hasSimulationAuthority) return;
            Model.ApplySnapshot(state, remaining);
            RefreshVisuals();
        }

        private void RefreshVisuals()
        {
            bool showFood = State is GrillTaskState.Cooking or GrillTaskState.ReadyToServe or GrillTaskState.Burned;
            for (int i = 0; i < foodRenderers.Length; i++)
            {
                if (foodRenderers[i] != null) foodRenderers[i].enabled = showFood;
            }
            if (heatIndicator == null) return;

            Color color = State switch
            {
                GrillTaskState.NeedsFuel => new Color(0.18f, 0.18f, 0.18f),
                GrillTaskState.Heating => Color.Lerp(new Color(0.35f, 0.08f, 0.02f),
                    new Color(1f, 0.35f, 0.03f), Mathf.PingPong(Time.time * 2f, 1f)),
                GrillTaskState.ReadyToCook => new Color(1f, 0.55f, 0.04f),
                GrillTaskState.Cooking => new Color(1f, 0.22f, 0.03f),
                GrillTaskState.ReadyToServe => new Color(0.18f, 1f, 0.25f),
                _ => new Color(0.08f, 0.04f, 0.02f)
            };
            properties ??= new MaterialPropertyBlock();
            heatIndicator.GetPropertyBlock(properties);
            properties.SetColor("_Color", color);
            heatIndicator.SetPropertyBlock(properties);
        }
    }
}
