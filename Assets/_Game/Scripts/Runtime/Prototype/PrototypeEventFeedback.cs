using System;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeEventFeedback : MonoBehaviour
    {
        private HordeDirector horde;
        private GrillTaskStation grill;
        private PlayerVitals player;
        private AttractionSource[] attractions = Array.Empty<AttractionSource>();
        private HordeEntrance[] entrances = Array.Empty<HordeEntrance>();
        private PlayerReviveTarget[] reviveTargets = Array.Empty<PlayerReviveTarget>();
        private GameSettingsController settings;
        private float visibleUntil;
        private Color accent;
        private GUIStyle eventStyle;

        public string CurrentEvent { get; private set; } = string.Empty;
        public bool IsVisible => Time.unscaledTime < visibleUntil;

        public void Configure(HordeDirector hordeDirector, GrillTaskStation grillStation,
            PlayerVitals playerVitals, AttractionSource[] attractionSources, HordeEntrance[] hordeEntrances,
            PlayerReviveTarget[] playerReviveTargets, GameSettingsController gameSettings)
        {
            Unsubscribe();
            horde = hordeDirector;
            grill = grillStation;
            player = playerVitals;
            attractions = attractionSources ?? Array.Empty<AttractionSource>();
            entrances = hordeEntrances ?? Array.Empty<HordeEntrance>();
            reviveTargets = playerReviveTargets ?? Array.Empty<PlayerReviveTarget>();
            settings = gameSettings;
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Show(string message, Color color, float duration = 1.8f)
        {
            CurrentEvent = message;
            accent = color;
            visibleUntil = Time.unscaledTime + duration;
        }

        private void OnGUI()
        {
            if (Time.timeScale <= 0f || !IsVisible || (settings != null && !settings.Subtitles)) return;
            eventStyle ??= new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            float remaining = visibleUntil - Time.unscaledTime;
            float pulse = 0.72f + Mathf.Sin(Time.unscaledTime * 12f) * 0.08f;
            Color previous = GUI.color;
            GUI.color = new Color(accent.r, accent.g, accent.b, Mathf.Min(pulse, remaining * 1.5f));
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 280f, 68f, 560f, 48f), Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(Screen.width * 0.5f - 270f, 72f, 540f, 40f), CurrentEvent, eventStyle);
        }

        private void HandlePhase(int wave, bool active) => Show(active ? $"OLEADA {wave}" : "RESPIRO: PREPAREN EL PATIO",
            active ? new Color(0.86f, 0.20f, 0.12f) : new Color(0.08f, 0.62f, 0.43f), 2.2f);
        private void HandleServed() => Show("ASADO SERVIDO  +250  +25 VIDA", new Color(0.12f, 0.68f, 0.35f));
        private void HandleBurned() => Show("SE QUEMO LA CARNE  -40", new Color(0.84f, 0.16f, 0.08f));
        private void HandleBlocked() => Show("ENTRADA BLOQUEADA  +35", new Color(0.12f, 0.55f, 0.76f));
        private void HandleBarrierSection(HordeEntrance entrance) =>
            Show($"BARRERA CEDIENDO  {entrance.RemainingSections} SECCIONES",
                new Color(0.90f, 0.52f, 0.08f));
        private void HandleBarrierDestroyed(HordeEntrance entrance) =>
            Show("BARRERA DESTRUIDA: FLUJO TOTAL", new Color(0.84f, 0.16f, 0.08f));
        private void HandleDistraction() => Show("HORDA DISTRAIDA  +60", new Color(0.94f, 0.56f, 0.06f));
        private void HandleKnockdown() => Show("TE TUMBARON", new Color(0.82f, 0.12f, 0.10f));
        private void HandleRevive() => Show("COMPANERO RESCATADO  +120", new Color(0.10f, 0.70f, 0.48f));

        private void Subscribe()
        {
            if (horde != null) horde.PhaseChanged += HandlePhase;
            if (grill != null)
            {
                grill.Served += HandleServed;
                grill.Burned += HandleBurned;
            }
            if (player != null) player.KnockedDown += HandleKnockdown;
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null) attractions[i].Activated += HandleDistraction;
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
            if (horde != null) horde.PhaseChanged -= HandlePhase;
            if (grill != null)
            {
                grill.Served -= HandleServed;
                grill.Burned -= HandleBurned;
            }
            if (player != null) player.KnockedDown -= HandleKnockdown;
            for (int i = 0; i < attractions.Length; i++)
                if (attractions[i] != null) attractions[i].Activated -= HandleDistraction;
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
