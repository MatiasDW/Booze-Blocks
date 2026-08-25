using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeHud : MonoBehaviour
    {
        private PlayerVitals vitals;
        private PlayerStateMachine playerState;
        private PlayerInteraction interaction;
        private PrototypeRound round;
        private HordeDirector horde;
        private PlayerInventory inventory;
        private PlayerAppearance appearance;
        private PlayerActionFeedback feedback;
        private RunProgression progression;
        private DrinkPreparationStation drinkStation;
        private GrillTaskStation grillStation;
        private RunSessionDirector session;
        private ContextObjectiveTracker objectives;
        private PrototypePauseMenu pauseMenu;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle compactStyle;
        private float textRefreshTimer;
        private string stateText = string.Empty;
        private string populationText = string.Empty;
        private string objectiveText = string.Empty;
        private string waveText = string.Empty;
        private string inventoryText = string.Empty;
        private bool detailsRequested;

        public bool IsDetailVisible => detailsRequested;
        public bool IsSuppressed => Time.timeScale <= 0f || (pauseMenu != null && pauseMenu.IsPaused);

        public void Configure(PlayerVitals playerVitals, PlayerStateMachine stateMachine,
            PlayerInteraction playerInteraction, PrototypeRound prototypeRound, HordeDirector hordeDirector,
            PlayerInventory playerInventory, RunProgression runProgression, DrinkPreparationStation station,
            GrillTaskStation grill, RunSessionDirector runSession, ContextObjectiveTracker objectiveTracker)
        {
            vitals = playerVitals;
            playerState = stateMachine;
            interaction = playerInteraction;
            round = prototypeRound;
            horde = hordeDirector;
            inventory = playerInventory;
            appearance = playerVitals.GetComponent<PlayerAppearance>();
            feedback = playerVitals.GetComponent<PlayerActionFeedback>();
            progression = runProgression;
            drinkStation = station;
            grillStation = grill;
            session = runSession;
            objectives = objectiveTracker;
        }

        public void SetPauseMenu(PrototypePauseMenu prototypePauseMenu)
        {
            pauseMenu = prototypePauseMenu;
        }

        private void Update()
        {
            if (vitals == null || round == null || horde == null) return;
            detailsRequested = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
            textRefreshTimer -= Time.unscaledDeltaTime;
            if (textRefreshTimer > 0f) return;
            textRefreshTimer = 0.25f;

            string phase = horde.IsPreparing ? "preparacion" : horde.IsWaveActive ? "activa" : "descanso";
            populationText =
                $"Jugadores: {round.SurvivingPlayerCount}/{round.RegisteredPlayerCount} | Horda: {horde.ActiveUnitCount}/{horde.WaveUnitLimit} | Flujo entradas: {HordeEntranceRegistry.TotalFlow:0.0}/4";
            stateText = $"Estado: {playerState.State} | Oleada {horde.CurrentWave} ({phase})";
            waveText = $"OLEADA {horde.CurrentWave}  {phase.ToUpperInvariant()} {Mathf.CeilToInt(horde.WaveRemainingTime)}s   |   RONDA {FormatTime(round.RemainingTime)}";
            inventoryText = BuildInventoryText();
            objectiveText = objectives != null ? objectives.CurrentObjective : progression?.StatusText ?? string.Empty;
        }

        private void OnGUI()
        {
            if (vitals == null || round == null || IsSuppressed) return;
            EnsureStyles();

            float panelWidth = Mathf.Min(detailsRequested ? 430f : 350f, Screen.width - 36f);
            float panelHeight = detailsRequested ? 370f : 154f;
            Rect panel = new Rect(18f, 18f, panelWidth, panelHeight);
            GUI.Box(panel, GUIContent.none);
            DrawBar(new Rect(panel.x + 14f, panel.y + 14f, panel.width - 28f, 15f),
                vitals.HealthRatio, new Color(0.85f, 0.22f, 0.18f), "HEALTH");
            DrawBar(new Rect(panel.x + 14f, panel.y + 42f, panel.width - 28f, 15f),
                vitals.BuzzRatio, new Color(0.96f, 0.67f, 0.12f), "BUZZ");
            DrawBar(new Rect(panel.x + 14f, panel.y + 70f, panel.width - 28f, 15f),
                1f - vitals.BalanceRatio, new Color(0.18f, 0.72f, 0.86f), "STAMINA");
            GUI.Label(new Rect(panel.x + 14f, panel.y + 96f, panel.width - 28f, 22f),
                waveText, compactStyle);
            GUI.Label(new Rect(panel.x + 14f, panel.y + 120f, panel.width - 92f, 22f),
                inventoryText, compactStyle);
            GUI.Label(new Rect(panel.x + panel.width - 82f, panel.y + 120f, 68f, 22f),
                "TAB INFO", compactStyle);

            if (detailsRequested)
            {
                float x = panel.x + 14f;
                float width = panel.width - 28f;
                GUI.Label(new Rect(x, panel.y + 151f, width, 22f), stateText, labelStyle);
                GUI.Label(new Rect(x, panel.y + 174f, width, 22f), populationText, labelStyle);
                GUI.Label(new Rect(x, panel.y + 197f, width, 22f),
                    $"Look: {appearance?.CurrentDescription ?? "-"}", labelStyle);
                GUI.Label(new Rect(x, panel.y + 220f, width, 22f),
                    $"Puntaje: {session?.Score.Score ?? 0} | Asados: {session?.Score.Servings ?? 0} | Rescates: {session?.Score.Rescues ?? 0}",
                    labelStyle);
                GUI.Label(new Rect(x, panel.y + 243f, width, 22f),
                    $"Crisis: {session?.CrisisName ?? "-"} - {session?.CrisisDescription ?? string.Empty}",
                    labelStyle);
                GUI.Label(new Rect(x, panel.y + 266f, width, 40f), objectiveText, labelStyle);
                GUI.Label(new Rect(x, panel.y + 307f, width, 22f),
                    $"Parrilla: {grillStation?.Prompt ?? "-"}", labelStyle);
                GUI.Label(new Rect(x, panel.y + 330f, width, 22f),
                    BuildDrinkStationText(), labelStyle);
            }

            if (objectives != null && !string.IsNullOrEmpty(objectives.TutorialText))
            {
                float tutorialWidth = Mathf.Clamp(Screen.width - panel.xMax - 32f, 220f, 500f);
                Rect tutorial = new Rect(Screen.width - tutorialWidth - 18f, 18f, tutorialWidth, 34f);
                GUI.Box(tutorial, GUIContent.none);
                GUI.Label(new Rect(tutorial.x + 10f, tutorial.y + 2f, tutorial.width - 20f, 28f),
                    objectives.TutorialText, titleStyle);
            }

            if (feedback != null && feedback.IsVisible)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 255f, Screen.height - 175f, 510f, 42f), GUIContent.none);
                GUI.Label(new Rect(Screen.width * 0.5f - 245f, Screen.height - 170f, 490f, 32f),
                    feedback.CurrentMessage, titleStyle);
            }

            if (!string.IsNullOrEmpty(interaction.CurrentPrompt))
            {
                GUI.Label(new Rect(Screen.width * 0.5f - 120f, Screen.height - 120f, 360f, 30f),
                    interaction.CurrentPrompt, titleStyle);
            }

        }

        private string BuildInventoryText()
        {
            if (inventory == null) return "BOTELLA - | SIN OBJETO";
            string itemName = inventory.Model.DefenseItem switch
            {
                DefenseItemType.Broom => "ESCOBA",
                DefenseItemType.FryingPan => "SARTEN",
                _ => "SIN OBJETO"
            };
            string uses = inventory.Model.DefenseItem == DefenseItemType.None
                ? string.Empty
                : $" x{inventory.Model.DefenseUses}";
            return $"BOTELLA {inventory.Model.DrinkServings}/{inventory.Model.DrinkCapacity}  |  {itemName}{uses}";
        }

        private string BuildDrinkStationText()
        {
            if (drinkStation == null) return "BOOZE LAB: -";
            return drinkStation.State switch
            {
                DrinkStationState.Preparing => $"BOOZE LAB: preparando {Mathf.CeilToInt(drinkStation.RemainingPreparation)}s",
                DrinkStationState.Ready => $"BOOZE LAB: {drinkStation.ServingsReady} porciones listas",
                _ => "BOOZE LAB: libre"
            };
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{total / 60:00}:{total % 60:00}";
        }

        private void DrawBar(Rect rect, float value, Color color, string label)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(rect.x + 6f, rect.y - 2f, rect.width, rect.height + 4f), label, labelStyle);
        }

        private void EnsureStyles()
        {
            if (labelStyle != null) return;
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            titleStyle = new GUIStyle(labelStyle)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter
            };
            compactStyle = new GUIStyle(labelStyle)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft
            };
        }
    }
}
