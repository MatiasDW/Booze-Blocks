using System;
using BoozeBlocks.Camera;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeVfxDirector : MonoBehaviour
    {
        private ParticleSystem particles;
        private Material particleMaterial;
        private ThirdPersonCamera cameraController;
        private HordeDirector horde;
        private GrillTaskStation grill;
        private PlayerVitals player;
        private PlayerMotor motor;
        private PlayerInventory inventory;
        private PlayerDefenseController defense;
        private AttractionSource[] attractions = Array.Empty<AttractionSource>();
        private HordeEntrance[] entrances = Array.Empty<HordeEntrance>();
        private PlayerReviveTarget[] reviveTargets = Array.Empty<PlayerReviveTarget>();
        private float nextAmbientEffectTime;
        private float impulseFovKick;

        public int MaximumParticles => particles != null ? particles.main.maxParticles : 0;

        public void Configure(ThirdPersonCamera thirdPersonCamera, HordeDirector hordeDirector,
            GrillTaskStation grillStation, PlayerVitals playerVitals, PlayerMotor playerMotor,
            PlayerInventory playerInventory, PlayerDefenseController defenseController,
            AttractionSource[] attractionSources, HordeEntrance[] hordeEntrances,
            PlayerReviveTarget[] playerReviveTargets)
        {
            Unsubscribe();
            cameraController = thirdPersonCamera;
            horde = hordeDirector;
            grill = grillStation;
            player = playerVitals;
            motor = playerMotor;
            inventory = playerInventory;
            defense = defenseController;
            attractions = attractionSources ?? Array.Empty<AttractionSource>();
            entrances = hordeEntrances ?? Array.Empty<HordeEntrance>();
            reviveTargets = playerReviveTargets ?? Array.Empty<PlayerReviveTarget>();
            BuildParticles();
            Subscribe();
        }

        private void Update()
        {
            UpdateFovKick();
            if (particles == null || Time.time < nextAmbientEffectTime) return;
            nextAmbientEffectTime = Time.time + 0.28f;
            if (grill != null && grill.State is GrillTaskState.Cooking or GrillTaskState.ReadyToServe or GrillTaskState.Burned)
            {
                Color smoke = grill.State == GrillTaskState.Burned
                    ? new Color(0.12f, 0.10f, 0.09f, 0.75f)
                    : new Color(0.78f, 0.72f, 0.62f, 0.55f);
                Emit(grill.transform.position + Vector3.up * 2.3f, smoke, 2, 0.7f, 0.30f);
            }
            for (int i = 0; i < entrances.Length; i++)
            {
                if (entrances[i] != null && entrances[i].IsBuilding)
                    Emit(entrances[i].transform.position + Vector3.up * 0.35f,
                        new Color(0.72f, 0.52f, 0.28f, 0.75f), 2, 0.75f, 0.16f);
            }
        }

        private void UpdateFovKick()
        {
            if (cameraController == null) return;
            impulseFovKick = Mathf.MoveTowards(impulseFovKick, 0f, Time.deltaTime * 8f);
            float buzzKick = 0f;
            if (player != null)
            {
                float excess = Mathf.Clamp01((player.BuzzRatio - 0.7f) / 0.3f);
                buzzKick = excess * 4f;
            }
            cameraController.SetFovKick(buzzKick + impulseFovKick);
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (particleMaterial != null) Destroy(particleMaterial);
        }

        private void BuildParticles()
        {
            if (particles != null) return;
            particles = gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.65f;
            main.startSize = 0.22f;
            main.startSpeed = 0f;
            main.gravityModifier = 0.08f;
            main.maxParticles = 180;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Standard");
            particleMaterial = new Material(shader);
            renderer.sharedMaterial = particleMaterial;
        }

        private void Emit(Vector3 position, Color color, int count, float speed, float size)
        {
            if (particles == null) return;
            for (int i = 0; i < count; i++)
            {
                float angle = i * 2.399963f;
                float vertical = 0.35f + (i % 3) * 0.22f;
                Vector3 direction = new Vector3(Mathf.Cos(angle), vertical, Mathf.Sin(angle)).normalized;
                ParticleSystem.EmitParams parameters = new ParticleSystem.EmitParams
                {
                    position = position,
                    velocity = direction * speed,
                    startColor = color,
                    startSize = size * (0.85f + (i % 4) * 0.10f)
                };
                particles.Emit(parameters, 1);
            }
        }

        private void HandlePhase(int wave, bool active)
        {
            if (player == null) return;
            Emit(player.transform.position + Vector3.up * 2.4f,
                active ? new Color(1f, 0.18f, 0.08f) : new Color(0.10f, 0.85f, 0.48f), 22, 2.4f, 0.24f);
            if (active) impulseFovKick = Mathf.Max(impulseFovKick, 4.5f);
        }

        private void HandleServed() => Emit(grill.transform.position + Vector3.up * 2f,
            new Color(0.18f, 1f, 0.35f), 20, 2.2f, 0.24f);
        private void HandleBurned() => Emit(grill.transform.position + Vector3.up * 2f,
            new Color(0.16f, 0.10f, 0.08f), 24, 1.6f, 0.34f);
        private void HandleKnockdown()
        {
            Emit(player.transform.position + Vector3.up * 0.7f, new Color(1f, 0.70f, 0.08f), 18, 2.7f, 0.28f);
            cameraController?.AddImpulse(0.70f);
            impulseFovKick = Mathf.Max(impulseFovKick, 6f);
        }
        private void HandleJump() => Emit(player.transform.position + Vector3.up * 0.05f,
            new Color(0.82f, 0.74f, 0.58f, 0.7f), 5, 0.9f, 0.14f);
        private void HandleDrink() => Emit(player.transform.position + Vector3.up * 1.2f,
            new Color(1f, 0.65f, 0.05f), 12, 1.6f, 0.18f);
        private void HandleDefense(int affected)
        {
            Emit(player.transform.position + player.transform.forward * 1.4f + Vector3.up,
                new Color(0.18f, 0.82f, 1f), Mathf.Clamp(8 + affected, 8, 20), 2.5f, 0.19f);
            if (affected > 0) cameraController?.AddImpulse(0.22f);
        }
        private void HandleKidHit(Vector3 position) =>
            Emit(position + Vector3.up * 0.8f, new Color(1f, 0.78f, 0.12f), 4, 1.4f, 0.13f);
        private void HandleKidDefeated(Vector3 position) =>
            Emit(position + Vector3.up * 0.8f, new Color(0.20f, 0.88f, 1f), 9, 2.0f, 0.18f);
        private void HandleAttraction()
        {
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null && attractions[i].IsActive)
                    Emit(attractions[i].transform.position + Vector3.up, new Color(1f, 0.32f, 0.45f), 18, 2f, 0.21f);
        }
        private void HandleBlocked()
        {
            for (int i = 0; i < entrances.Length; i++)
                if (entrances[i] != null && entrances[i].IsBlocked)
                    Emit(entrances[i].transform.position + Vector3.up, new Color(0.68f, 0.48f, 0.24f), 12, 1.7f, 0.20f);
        }
        private void HandleBarrierSection(HordeEntrance entrance)
        {
            if (entrance == null) return;
            Emit(entrance.transform.position + Vector3.up * 0.9f,
                new Color(0.86f, 0.60f, 0.25f), 12, 2.1f, 0.20f);
            cameraController?.AddImpulse(0.10f);
        }
        private void HandleBarrierDestroyed(HordeEntrance entrance)
        {
            if (entrance == null) return;
            Emit(entrance.transform.position + Vector3.up * 0.8f,
                new Color(0.92f, 0.22f, 0.12f), 20, 2.8f, 0.24f);
            cameraController?.AddImpulse(0.18f);
            impulseFovKick = Mathf.Max(impulseFovKick, 3.5f);
        }
        private void HandleRevive()
        {
            Emit(player.transform.position + Vector3.up, new Color(0.12f, 1f, 0.58f), 22, 2.4f, 0.22f);
            cameraController?.AddImpulse(0.12f);
        }

        private void Subscribe()
        {
            if (horde != null)
            {
                horde.PhaseChanged += HandlePhase;
                horde.UnitHit += HandleKidHit;
                horde.UnitDefeated += HandleKidDefeated;
            }
            if (grill != null)
            {
                grill.Served += HandleServed;
                grill.Burned += HandleBurned;
            }
            if (player != null) player.KnockedDown += HandleKnockdown;
            if (motor != null) motor.Jumped += HandleJump;
            if (inventory != null) inventory.Drank += HandleDrink;
            if (defense != null) defense.Used += HandleDefense;
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null) attractions[i].Activated += HandleAttraction;
            for (int i = 0; i < entrances.Length; i++)
                if (entrances[i] != null)
                {
                    entrances[i].Blocked += HandleBlocked;
                    entrances[i].SectionBroken += HandleBarrierSection;
                    entrances[i].Destroyed += HandleBarrierDestroyed;
                }
            for (int i = 0; i < reviveTargets.Length; i++)
                if (reviveTargets[i] != null) reviveTargets[i].Revived += HandleRevive;
        }

        private void Unsubscribe()
        {
            if (horde != null)
            {
                horde.PhaseChanged -= HandlePhase;
                horde.UnitHit -= HandleKidHit;
                horde.UnitDefeated -= HandleKidDefeated;
            }
            if (grill != null)
            {
                grill.Served -= HandleServed;
                grill.Burned -= HandleBurned;
            }
            if (player != null) player.KnockedDown -= HandleKnockdown;
            if (motor != null) motor.Jumped -= HandleJump;
            if (inventory != null) inventory.Drank -= HandleDrink;
            if (defense != null) defense.Used -= HandleDefense;
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null) attractions[i].Activated -= HandleAttraction;
            for (int i = 0; i < entrances.Length; i++)
                if (entrances[i] != null)
                {
                    entrances[i].Blocked -= HandleBlocked;
                    entrances[i].SectionBroken -= HandleBarrierSection;
                    entrances[i].Destroyed -= HandleBarrierDestroyed;
                }
            for (int i = 0; i < reviveTargets.Length; i++)
                if (reviveTargets[i] != null) reviveTargets[i].Revived -= HandleRevive;
        }
    }
}
