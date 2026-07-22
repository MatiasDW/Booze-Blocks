using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;

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
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private float textRefreshTimer;
        private string stateText = string.Empty;
        private string populationText = string.Empty;
        private string objectiveText = string.Empty;

        public void Configure(PlayerVitals playerVitals, PlayerStateMachine stateMachine,
            PlayerInteraction playerInteraction, PrototypeRound prototypeRound, HordeDirector hordeDirector,
            PlayerInventory playerInventory, RunProgression runProgression, DrinkPreparationStation station)
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
        }

        private void Update()
        {
            if (vitals == null || round == null || horde == null) return;
            textRefreshTimer -= Time.unscaledDeltaTime;
            if (textRefreshTimer > 0f) return;
            textRefreshTimer = 0.25f;

            string phase = horde.IsWaveActive ? "activa" : "descanso";
            populationText = $"Jugadores: {round.SurvivingPlayerCount}/{round.RegisteredPlayerCount} | Horda: {horde.ActiveUnitCount}/{horde.DesiredUnitCount} | Entradas: {HordeEntranceRegistry.OpenCount}/4";
            stateText = $"Estado: {playerState.State} | Oleada {horde.CurrentWave} ({phase}, {Mathf.CeilToInt(horde.WaveRemainingTime)}s)";
            objectiveText = progression != null ? progression.StatusText : string.Empty;
        }

        private void OnGUI()
        {
            if (vitals == null || round == null) return;
            EnsureStyles();

            GUI.Box(new Rect(18f, 18f, 390f, 288f), GUIContent.none);
            GUI.Label(new Rect(34f, 28f, 300f, 30f), "BOOZE & BLOCKS", titleStyle);
            DrawBar(new Rect(34f, 68f, 285f, 18f), vitals.HealthRatio, new Color(0.85f, 0.22f, 0.18f), "HEALTH");
            DrawBar(new Rect(34f, 100f, 285f, 18f), vitals.BuzzRatio, new Color(0.96f, 0.67f, 0.12f), "BUZZ");
            DrawBar(new Rect(34f, 132f, 285f, 18f), vitals.BalanceRatio, new Color(0.18f, 0.72f, 0.86f), "BALANCE");
            GUI.Label(new Rect(34f, 162f, 285f, 24f), stateText, labelStyle);
            GUI.Label(new Rect(34f, 188f, 285f, 24f), populationText, labelStyle);
            if (inventory != null)
            {
                string itemName = inventory.Model.DefenseItem switch
                {
                    DefenseItemType.Broom => "Escoba",
                    DefenseItemType.FryingPan => "Sarten",
                    _ => "Vacio"
                };
                GUI.Label(new Rect(34f, 214f, 350f, 24f),
                    $"Botella: {inventory.Model.DrinkServings}/{inventory.Model.DrinkCapacity} | Objeto: {itemName} ({inventory.Model.DefenseUses})",
                    labelStyle);
            }
            if (appearance != null)
            {
                GUI.Label(new Rect(34f, 238f, 350f, 24f), $"Look: {appearance.CurrentDescription}", labelStyle);
            }
            GUI.Label(new Rect(34f, 262f, 350f, 24f), $"Supervivencia: {Mathf.CeilToInt(round.RemainingTime)}s", labelStyle);

            GUI.Label(new Rect(18f, Screen.height - 108f, 650f, 26f), objectiveText, labelStyle);
            if (drinkStation != null)
            {
                string stationText = drinkStation.State switch
                {
                    DrinkStationState.Preparing => $"Mezcla preparandose: {Mathf.CeilToInt(drinkStation.RemainingPreparation)}s",
                    DrinkStationState.Ready => $"Mezcla lista: {drinkStation.ServingsReady} porciones",
                    _ => "Estacion libre: inicia una mezcla con E"
                };
                GUI.Label(new Rect(18f, Screen.height - 82f, 650f, 26f), stationText, labelStyle);
            }
            GUI.Label(new Rect(18f, Screen.height - 52f, 700f, 26f),
                "Mover: WASD | Saltar: Espacio | Interactuar: E | Defender: F | Beber: Q", labelStyle);

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

            if (round.State != PrototypeRoundState.Playing)
            {
                string message = round.State == PrototypeRoundState.Won ? "ESCAPARON" : "LA HORDA GANO";
                GUI.Box(new Rect(Screen.width * 0.5f - 170f, Screen.height * 0.5f - 55f, 340f, 110f), GUIContent.none);
                GUI.Label(new Rect(Screen.width * 0.5f - 145f, Screen.height * 0.5f - 20f, 300f, 50f), message, titleStyle);
            }
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
        }
    }
}
