using System;
using System.Collections.Generic;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeAudioDirector : MonoBehaviour
    {
        private const int SampleRate = 22050;

        private readonly List<AudioClip> generatedClips = new List<AudioClip>(8);
        private AudioSource musicSource;
        private AudioSource effectsSource;
        private GameSettingsController settings;
        private HordeDirector horde;
        private GrillTaskStation grill;
        private PlayerVitals player;
        private PlayerMotor motor;
        private PlayerInventory inventory;
        private PlayerDefenseController defense;
        private PlayerReviveTarget[] reviveTargets = Array.Empty<PlayerReviveTarget>();
        private AttractionSource[] attractions = Array.Empty<AttractionSource>();
        private HordeEntrance[] entrances = Array.Empty<HordeEntrance>();
        private AudioClip waveClip;
        private AudioClip breakClip;
        private AudioClip successClip;
        private AudioClip failureClip;
        private AudioClip impactClip;
        private AudioClip jumpClip;
        private AudioClip drinkClip;
        private AudioClip defenseClip;
        private AudioClip reviveClip;

        public int GeneratedClipCount => generatedClips.Count;

        public void Configure(GameSettingsController gameSettings, HordeDirector hordeDirector,
            GrillTaskStation grillStation, PlayerVitals playerVitals, AttractionSource[] attractionSources,
            HordeEntrance[] hordeEntrances, PlayerMotor playerMotor, PlayerInventory playerInventory,
            PlayerDefenseController defenseController, PlayerReviveTarget[] playerReviveTargets)
        {
            Unsubscribe();
            settings = gameSettings;
            horde = hordeDirector;
            grill = grillStation;
            player = playerVitals;
            motor = playerMotor;
            inventory = playerInventory;
            defense = defenseController;
            reviveTargets = playerReviveTargets ?? Array.Empty<PlayerReviveTarget>();
            attractions = attractionSources ?? Array.Empty<AttractionSource>();
            entrances = hordeEntrances ?? Array.Empty<HordeEntrance>();
            BuildAudio();
            Subscribe();
        }

        private void Update()
        {
            if (settings == null) return;
            if (musicSource != null) musicSource.volume = settings.MusicVolume * 0.24f;
            if (effectsSource != null) effectsSource.volume = settings.EffectsVolume * 0.75f;
        }

        private void OnDestroy()
        {
            Unsubscribe();
            for (int i = 0; i < generatedClips.Count; i++)
            {
                if (generatedClips[i] != null) Destroy(generatedClips[i]);
            }
        }

        private void BuildAudio()
        {
            if (musicSource != null) return;
            musicSource = gameObject.AddComponent<AudioSource>();
            effectsSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            effectsSource.playOnAwake = false;
            effectsSource.spatialBlend = 0f;

            AudioClip music = CreatePartyLoop();
            waveClip = CreateTone("Wave Start", 0.30f, 185f, 370f, 0.20f);
            breakClip = CreateTone("Wave Clear", 0.42f, 440f, 660f, 0.15f);
            successClip = CreateTone("Task Complete", 0.26f, 520f, 780f, 0.12f);
            failureClip = CreateTone("Task Failed", 0.34f, 150f, 92f, 0.20f);
            impactClip = CreateNoise("Impact", 0.18f, 0.34f);
            jumpClip = CreateTone("Jump", 0.13f, 240f, 410f, 0.10f);
            drinkClip = CreateTone("Drink", 0.22f, 680f, 340f, 0.10f);
            defenseClip = CreateNoise("Defense Swing", 0.12f, 0.20f);
            reviveClip = CreateTone("Revive", 0.34f, 360f, 720f, 0.14f);
            musicSource.clip = music;
            musicSource.Play();
        }

        private AudioClip CreatePartyLoop()
        {
            const float duration = 4f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            float[] notes = { 220f, 277.18f, 329.63f, 277.18f, 246.94f, 329.63f, 369.99f, 329.63f };
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)SampleRate;
                int beat = Mathf.FloorToInt(time * 2f) % notes.Length;
                float beatTime = Mathf.Repeat(time, 0.5f);
                float envelope = Mathf.Exp(-beatTime * 4.5f);
                float melody = Mathf.Sin(time * notes[beat] * Mathf.PI * 2f) * envelope * 0.12f;
                float bass = Mathf.Sin(time * 110f * Mathf.PI * 2f) * 0.045f;
                float kickPhase = Mathf.Repeat(time, 0.5f);
                float kick = Mathf.Sin(kickPhase * Mathf.Lerp(110f, 48f, kickPhase * 2f) * Mathf.PI * 2f) *
                             Mathf.Exp(-kickPhase * 18f) * 0.20f;
                samples[i] = Mathf.Clamp(melody + bass + kick, -0.8f, 0.8f);
            }
            return CreateClip("Backyard Party Loop", samples);
        }

        private AudioClip CreateTone(string name, float duration, float startFrequency, float endFrequency,
            float amplitude)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            float phase = 0f;
            for (int i = 0; i < sampleCount; i++)
            {
                float progress = i / (float)sampleCount;
                float frequency = Mathf.Lerp(startFrequency, endFrequency, progress);
                phase += frequency / SampleRate;
                float envelope = Mathf.Sin(progress * Mathf.PI);
                samples[i] = Mathf.Sin(phase * Mathf.PI * 2f) * envelope * amplitude;
            }
            return CreateClip(name, samples);
        }

        private AudioClip CreateNoise(string name, float duration, float amplitude)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] samples = new float[sampleCount];
            uint state = 0x9E3779B9u;
            for (int i = 0; i < sampleCount; i++)
            {
                state = state * 1664525u + 1013904223u;
                float noise = (state / (float)uint.MaxValue) * 2f - 1f;
                samples[i] = noise * Mathf.Exp(-i / (SampleRate * 0.045f)) * amplitude;
            }
            return CreateClip(name, samples);
        }

        private AudioClip CreateClip(string name, float[] samples)
        {
            AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);
            generatedClips.Add(clip);
            return clip;
        }

        private void Play(AudioClip clip)
        {
            if (effectsSource != null && clip != null) effectsSource.PlayOneShot(clip);
        }

        private void HandlePhaseChanged(int wave, bool active) => Play(active ? waveClip : breakClip);
        private void HandleSuccess() => Play(successClip);
        private void HandleFailure() => Play(failureClip);
        private void HandleImpact() => Play(impactClip);
        private void HandleJump() => Play(jumpClip);
        private void HandleDrink() => Play(drinkClip);
        private void HandleDefense(int affected) => Play(defenseClip);
        private void HandleRevive() => Play(reviveClip);
        private void HandleBarrierDamage(HordeEntrance entrance) => Play(impactClip);

        private void Subscribe()
        {
            if (horde != null) horde.PhaseChanged += HandlePhaseChanged;
            if (grill != null)
            {
                grill.Served += HandleSuccess;
                grill.Burned += HandleFailure;
            }
            if (player != null) player.KnockedDown += HandleImpact;
            if (motor != null) motor.Jumped += HandleJump;
            if (inventory != null) inventory.Drank += HandleDrink;
            if (defense != null) defense.Used += HandleDefense;
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null) attractions[i].Activated += HandleSuccess;
            for (int i = 0; i < entrances.Length; i++)
                if (entrances[i] != null)
                {
                    entrances[i].Blocked += HandleImpact;
                    entrances[i].SectionBroken += HandleBarrierDamage;
                    entrances[i].Destroyed += HandleBarrierDamage;
                }
            for (int i = 0; i < reviveTargets.Length; i++)
                if (reviveTargets[i] != null) reviveTargets[i].Revived += HandleRevive;
        }

        private void Unsubscribe()
        {
            if (horde != null) horde.PhaseChanged -= HandlePhaseChanged;
            if (grill != null)
            {
                grill.Served -= HandleSuccess;
                grill.Burned -= HandleFailure;
            }
            if (player != null) player.KnockedDown -= HandleImpact;
            if (motor != null) motor.Jumped -= HandleJump;
            if (inventory != null) inventory.Drank -= HandleDrink;
            if (defense != null) defense.Used -= HandleDefense;
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null) attractions[i].Activated -= HandleSuccess;
            for (int i = 0; i < entrances.Length; i++)
                if (entrances[i] != null)
                {
                    entrances[i].Blocked -= HandleImpact;
                    entrances[i].SectionBroken -= HandleBarrierDamage;
                    entrances[i].Destroyed -= HandleBarrierDamage;
                }
            for (int i = 0; i < reviveTargets.Length; i++)
                if (reviveTargets[i] != null) reviveTargets[i].Revived -= HandleRevive;
        }
    }
}
