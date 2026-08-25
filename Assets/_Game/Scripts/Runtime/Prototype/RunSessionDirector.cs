using System;
using System.Collections.Generic;
using BoozeBlocks.Camera;
using BoozeBlocks.Distractions;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RunSessionDirector : MonoBehaviour
    {
        private readonly List<PlayerVitals> players = new List<PlayerVitals>(8);
        private HordeDirector horde;
        private ThirdPersonCamera cameraController;
        private DrinkPreparationStation drinkStation;
        private GrillTaskStation grill;
        private AttractionSource[] attractions = Array.Empty<AttractionSource>();
        private HordeEntrance[] entrances = Array.Empty<HordeEntrance>();
        private PlayerReviveTarget[] reviveTargets = Array.Empty<PlayerReviveTarget>();
        private RunUpgradeType[] choices = Array.Empty<RunUpgradeType>();
        private int runSeed;
        private int upgradeTier;
        private int bottleLevel;
        private int defenseLevel;
        private int mixLevel;
        private int grillLevel;
        private int distractionLevel;
        private float crisisMixMultiplier = 1f;
        private float crisisGrillMultiplier = 1f;
        private float crisisDistractionMultiplier = 1f;
        private GUIStyle titleStyle;
        private GUIStyle choiceStyle;
        private bool hasSimulationAuthority = true;
        private bool waitingForHostUpgrade;

        public PartyScoreModel Score { get; } = new PartyScoreModel();
        public int RunSeed => runSeed;
        public RunCrisisType Crisis { get; private set; }
        public bool IsUpgradeChoiceOpen { get; private set; }
        public bool IsWaitingForHostUpgrade => waitingForHostUpgrade;
        public string CrisisName => Crisis switch
        {
            RunCrisisType.WetCharcoal => "Carbon mojado",
            RunCrisisType.WeakMix => "Mezcla aguada",
            RunCrisisType.DoubleBirthday => "Cumpleanos doble",
            _ => "Cercos sueltos"
        };
        public string CrisisDescription => Crisis switch
        {
            RunCrisisType.WetCharcoal => "La parrilla tarda 35% mas.",
            RunCrisisType.WeakMix => "El BOOZE LAB tarda 35% mas.",
            RunCrisisType.DoubleBirthday => "Las distracciones duran 30% menos.",
            _ => "Las barricadas duran 30% menos."
        };

        public void Configure(HordeDirector director, DrinkPreparationStation boozeStation,
            GrillTaskStation grillStation, AttractionSource[] attractionSources, int seed = 0)
        {
            Unsubscribe();
            horde = director;
            cameraController = FindAnyObjectByType<ThirdPersonCamera>();
            drinkStation = boozeStation;
            grill = grillStation;
            attractions = attractionSources ?? Array.Empty<AttractionSource>();
            entrances = FindObjectsByType<HordeEntrance>();
            reviveTargets = FindObjectsByType<PlayerReviveTarget>();
            runSeed = seed != 0 ? seed : unchecked(Environment.TickCount ^ DateTime.UtcNow.Millisecond * 7919);
            Crisis = RunVariantModels.SelectCrisis(runSeed);
            ApplyCrisis();
            ApplyUpgradeModifiers();
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
            PlayerRegistry.Changed += ApplyPlayerUpgrades;
            ApplyPlayerUpgrades();
        }

        private void OnDisable()
        {
            Unsubscribe();
            PlayerRegistry.Changed -= ApplyPlayerUpgrades;
            if (IsUpgradeChoiceOpen)
            {
                IsUpgradeChoiceOpen = false;
                Time.timeScale = 1f;
                if (cameraController != null) cameraController.enabled = true;
            }
        }

        private void Update()
        {
            if (!IsUpgradeChoiceOpen || Keyboard.current == null) return;
            if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectUpgrade(0);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectUpgrade(1);
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectUpgrade(2);
        }

        public bool SelectUpgrade(int index)
        {
            if (!IsUpgradeChoiceOpen || index < 0 || index >= choices.Length) return false;
            ApplyUpgrade(choices[index]);
            IsUpgradeChoiceOpen = false;
            Time.timeScale = 1f;
            if (cameraController != null) cameraController.enabled = true;
            return true;
        }

        private void HandlePhaseChanged(int wave, bool isActive)
        {
            if (!hasSimulationAuthority || isActive) return;
            Score.AddWaveBonus(wave);
            if (wave % 2 != 0) return;
            upgradeTier++;
            choices = RunVariantModels.CreateUpgradeChoices(runSeed, upgradeTier);
            IsUpgradeChoiceOpen = true;
            Time.timeScale = 0f;
            if (cameraController != null) cameraController.enabled = false;
        }

        private void ApplyUpgrade(RunUpgradeType upgrade)
        {
            switch (upgrade)
            {
                case RunUpgradeType.BiggerBottle: bottleLevel++; break;
                case RunUpgradeType.StrongerDefense: defenseLevel++; break;
                case RunUpgradeType.FastMix: mixLevel++; break;
                case RunUpgradeType.GrillMaster: grillLevel++; break;
                case RunUpgradeType.BetterDistractions: distractionLevel++; break;
            }
            ApplyPlayerUpgrades();
            ApplyUpgradeModifiers();
        }

        public void SetSimulationAuthority(bool isAuthoritative)
        {
            hasSimulationAuthority = isAuthoritative;
            if (isAuthoritative) ApplyRemoteUpgradePause(false);
        }

        public void ApplyAuthoritativeSeed(int seed)
        {
            if (seed == 0 || seed == runSeed) return;
            runSeed = seed;
            Crisis = RunVariantModels.SelectCrisis(runSeed);
            ApplyCrisis();
            ApplyUpgradeModifiers();
        }

        public void ApplyRemoteUpgradePause(bool paused)
        {
            if (hasSimulationAuthority && paused) return;
            if (waitingForHostUpgrade == paused) return;
            waitingForHostUpgrade = paused;
            if (paused)
            {
                Time.timeScale = 0f;
                if (cameraController != null) cameraController.enabled = false;
            }
            else if (!IsUpgradeChoiceOpen)
            {
                Time.timeScale = 1f;
                if (cameraController != null) cameraController.enabled = true;
            }
        }

        private void ApplyPlayerUpgrades()
        {
            PlayerRegistry.Fill(players, true);
            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].TryGetComponent(out PlayerInventory inventory)) continue;
                inventory.Model.SetDrinkCapacityAtLeast(2 + bottleLevel);
                inventory.Model.SetDefensePowerLevelAtLeast(defenseLevel);
            }
        }

        private void ApplyCrisis()
        {
            crisisMixMultiplier = Crisis == RunCrisisType.WeakMix ? 1.35f : 1f;
            crisisGrillMultiplier = Crisis == RunCrisisType.WetCharcoal ? 1.35f : 1f;
            crisisDistractionMultiplier = Crisis == RunCrisisType.DoubleBirthday ? 0.70f : 1f;
            float barricadeMultiplier = Crisis == RunCrisisType.LooseFences ? 0.70f : 1f;
            for (int i = 0; i < entrances.Length; i++) entrances[i]?.SetDurationMultiplier(barricadeMultiplier);
        }

        private void ApplyUpgradeModifiers()
        {
            float mixMultiplier = crisisMixMultiplier * Mathf.Pow(0.82f, mixLevel);
            float grillMultiplier = crisisGrillMultiplier * Mathf.Pow(0.82f, grillLevel);
            float distractionMultiplier = crisisDistractionMultiplier * Mathf.Pow(1.20f, distractionLevel);
            drinkStation?.SetPreparationMultiplier(mixMultiplier);
            grill?.SetTaskSpeedMultiplier(grillMultiplier);
            for (int i = 0; i < attractions.Length; i++)
            {
                attractions[i]?.SetDurationMultiplier(distractionMultiplier);
            }
        }

        private void Subscribe()
        {
            if (horde != null) horde.PhaseChanged -= HandlePhaseChanged;
            if (horde != null) horde.PhaseChanged += HandlePhaseChanged;
            if (grill != null)
            {
                grill.Served -= Score.AddServing;
                grill.Burned -= Score.AddBurnedMeal;
                grill.Served += Score.AddServing;
                grill.Burned += Score.AddBurnedMeal;
            }
            for (int i = 0; i < attractions.Length; i++)
            {
                if (attractions[i] == null) continue;
                attractions[i].Activated -= Score.AddDistraction;
                attractions[i].Activated += Score.AddDistraction;
            }
            for (int i = 0; i < entrances.Length; i++)
            {
                if (entrances[i] == null) continue;
                entrances[i].Blocked -= Score.AddBarricade;
                entrances[i].Blocked += Score.AddBarricade;
            }
            for (int i = 0; i < reviveTargets.Length; i++)
            {
                if (reviveTargets[i] == null) continue;
                reviveTargets[i].Revived -= Score.AddRescue;
                reviveTargets[i].Revived += Score.AddRescue;
            }
        }

        private void Unsubscribe()
        {
            if (horde != null) horde.PhaseChanged -= HandlePhaseChanged;
            if (grill != null)
            {
                grill.Served -= Score.AddServing;
                grill.Burned -= Score.AddBurnedMeal;
            }
            for (int i = 0; i < attractions.Length; i++)
            {
                if (attractions[i] != null) attractions[i].Activated -= Score.AddDistraction;
            }
            for (int i = 0; i < entrances.Length; i++)
            {
                if (entrances[i] != null) entrances[i].Blocked -= Score.AddBarricade;
            }
            for (int i = 0; i < reviveTargets.Length; i++)
            {
                if (reviveTargets[i] != null) reviveTargets[i].Revived -= Score.AddRescue;
            }
        }

        private void OnGUI()
        {
            if (waitingForHostUpgrade)
            {
                EnsureStyles();
                GUI.Box(new Rect(Screen.width * 0.5f - 260f, Screen.height * 0.5f - 55f, 520f, 110f), GUIContent.none);
                GUI.Label(new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 35f, 480f, 70f),
                    "EL ANFITRION ESTA ELIGIENDO UNA MEJORA DE EQUIPO", titleStyle);
                return;
            }
            if (!IsUpgradeChoiceOpen || choices.Length != 3) return;
            EnsureStyles();
            Color previous = GUI.color;
            GUI.color = new Color(0.02f, 0.03f, 0.03f, 0.80f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float width = Mathf.Min(840f, Screen.width - 40f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 170f, width, 340f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 18f, panel.width - 40f, 44f),
                $"OLEADA SUPERADA - MEJORA DE EQUIPO {upgradeTier}", titleStyle);
            float cardWidth = (panel.width - 80f) / 3f;
            for (int i = 0; i < choices.Length; i++)
            {
                Rect card = new Rect(panel.x + 20f + i * (cardWidth + 20f), panel.y + 85f, cardWidth, 205f);
                if (GUI.Button(card, $"{i + 1}\n\n{UpgradeName(choices[i])}\n\n{UpgradeDescription(choices[i])}", choiceStyle))
                {
                    SelectUpgrade(i);
                }
            }
            GUI.Label(new Rect(panel.x + 20f, panel.y + 302f, panel.width - 40f, 25f),
                "Pulsa 1, 2 o 3. La eleccion mejora a todo el equipo.", choiceStyle);
        }

        private static string UpgradeName(RunUpgradeType upgrade) => upgrade switch
        {
            RunUpgradeType.BiggerBottle => "BOTELLA XL",
            RunUpgradeType.StrongerDefense => "BRAZOS DE PARRILLERO",
            RunUpgradeType.FastMix => "BARTENDER VELOZ",
            RunUpgradeType.GrillMaster => "MAESTRO DEL ASADO",
            _ => "FIESTA IRRESISTIBLE"
        };

        private static string UpgradeDescription(RunUpgradeType upgrade) => upgrade switch
        {
            RunUpgradeType.BiggerBottle => "+1 carga de booze por jugador",
            RunUpgradeType.StrongerDefense => "+25% fuerza con escoba y sarten",
            RunUpgradeType.FastMix => "Mezclas 18% mas rapidas",
            RunUpgradeType.GrillMaster => "Parrilla 18% mas rapida",
            _ => "Distracciones duran 20% mas"
        };

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.72f, 0.12f) }
            };
            choiceStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }
    }
}
