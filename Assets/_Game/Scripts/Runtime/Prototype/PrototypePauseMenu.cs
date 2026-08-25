using BoozeBlocks.Camera;
using BoozeBlocks.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypePauseMenu : MonoBehaviour
    {
        private PrototypeStartMenu startMenu;
        private RoundResultsScreen results;
        private RunSessionDirector session;
        private GameSettingsController settings;
        private PlayerMotor motor;
        private PlayerStateMachine playerState;
        private PlayerVitals vitals;
        private ThirdPersonCamera cameraController;
        private NetworkGameplayCoordinator networkGameplay;
        private OnlineSessionController onlineSession;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;

        public bool IsPaused { get; private set; }

        public void Configure(PrototypeStartMenu prototypeStartMenu, RoundResultsScreen resultsScreen,
            RunSessionDirector runSession, GameSettingsController gameSettings, PlayerMotor playerMotor,
            PlayerStateMachine stateMachine, ThirdPersonCamera thirdPersonCamera,
            NetworkGameplayCoordinator networkCoordinator, OnlineSessionController onlineController)
        {
            startMenu = prototypeStartMenu;
            results = resultsScreen;
            session = runSession;
            settings = gameSettings;
            motor = playerMotor;
            playerState = stateMachine;
            vitals = stateMachine != null ? stateMachine.GetComponent<PlayerVitals>() : null;
            cameraController = thirdPersonCamera;
            networkGameplay = networkCoordinator;
            onlineSession = onlineController;
        }

        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
            if (startMenu == null || !startMenu.IsStarted || results == null || results.IsVisible) return;
            if (session != null && (session.IsUpgradeChoiceOpen || session.IsWaitingForHostUpgrade)) return;
            TogglePause();
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else Pause();
        }

        public void Pause()
        {
            if (IsPaused) return;
            IsPaused = true;
            Time.timeScale = IsOnlineMatch ? 1f : 0f;
            motor?.SetControlEnabled(false);
            if (cameraController != null) cameraController.enabled = false;
        }

        public void Resume()
        {
            if (!IsPaused) return;
            IsPaused = false;
            Time.timeScale = 1f;
            if (playerState != null && playerState.State == PlayerState.Normal) motor?.SetControlEnabled(true);
            if (cameraController != null) cameraController.enabled = true;
        }

        private void OnDestroy()
        {
            if (IsPaused) Time.timeScale = 1f;
        }

        private void OnGUI()
        {
            if (!IsPaused || settings == null) return;
            EnsureStyles();
            Color previous = GUI.color;
            GUI.color = new Color(0.01f, 0.02f, 0.02f, 0.86f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float width = Mathf.Min(620f, Screen.width - 32f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.5f - 285f, width, 570f);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 30f, panel.y + 20f, panel.width - 60f, 45f),
                IsOnlineMatch ? "MENU LOCAL - LA PARTIDA CONTINUA" : "PARRILLADA EN PAUSA", titleStyle);
            DrawStatusBar(new Rect(panel.x + 42f, panel.y + 70f, 160f, 13f),
                vitals?.HealthRatio ?? 0f, new Color(0.85f, 0.22f, 0.18f), "VIDA");
            DrawStatusBar(new Rect(panel.x + 230f, panel.y + 70f, 160f, 13f),
                vitals?.BuzzRatio ?? 0f, new Color(0.96f, 0.67f, 0.12f), "BUZZ");
            DrawStatusBar(new Rect(panel.x + 418f, panel.y + 70f, 160f, 13f),
                1f - (vitals?.BalanceRatio ?? 0f), new Color(0.18f, 0.72f, 0.86f), "STAMINA");
            if (GUI.Button(new Rect(panel.x + 75f, panel.y + 99f, panel.width - 150f, 42f), "CONTINUAR")) Resume();

            settings.SetMasterVolume(DrawSlider(panel, 160f, "VOLUMEN MAESTRO", settings.MasterVolume));
            settings.SetMusicVolume(DrawSlider(panel, 205f, "MUSICA", settings.MusicVolume));
            settings.SetEffectsVolume(DrawSlider(panel, 250f, "EFECTOS", settings.EffectsVolume));
            settings.SetMouseSensitivity(DrawSlider(panel, 295f, "SENSIBILIDAD", settings.MouseSensitivity, 0.04f, 0.30f));

            if (GUI.Button(new Rect(panel.x + 45f, panel.y + 355f, 250f, 42f),
                    settings.CameraShake ? "CAMERA SHAKE: SI" : "CAMERA SHAKE: NO")) settings.ToggleCameraShake();
            if (GUI.Button(new Rect(panel.x + panel.width - 295f, panel.y + 355f, 250f, 42f),
                    settings.Subtitles ? "AVISOS: SI" : "AVISOS: NO")) settings.ToggleSubtitles();

            if (IsOnlineMatch)
            {
                if (GUI.Button(new Rect(panel.x + 75f, panel.y + 453f, panel.width - 150f, 48f),
                        "ABANDONAR SALA Y VOLVER AL MENU"))
                    onlineSession?.LeaveSession(() => Reload(0, false));
            }
            else
            {
                if (GUI.Button(new Rect(panel.x + 75f, panel.y + 425f, panel.width - 150f, 44f),
                        "REINICIAR MISMA SEMILLA")) Reload(session?.RunSeed ?? 0, true);
                if (GUI.Button(new Rect(panel.x + 75f, panel.y + 483f, panel.width - 150f, 44f),
                        "VOLVER AL MENU PRINCIPAL")) Reload(0, false);
            }
            GUI.Label(new Rect(panel.x + 30f, panel.y + 535f, panel.width - 60f, 24f),
                "Esc tambien continua la partida", labelStyle);
        }

        private bool IsOnlineMatch => networkGameplay != null && networkGameplay.IsMatchStarted;

        private float DrawSlider(Rect panel, float offsetY, string label, float value,
            float minimum = 0f, float maximum = 1f)
        {
            GUI.Label(new Rect(panel.x + 50f, panel.y + offsetY, 180f, 30f), label, labelStyle);
            float result = GUI.HorizontalSlider(new Rect(panel.x + 235f, panel.y + offsetY + 9f,
                panel.width - 340f, 20f), value, minimum, maximum);
            GUI.Label(new Rect(panel.x + panel.width - 95f, panel.y + offsetY, 55f, 30f),
                $"{Mathf.RoundToInt(Mathf.InverseLerp(minimum, maximum, result) * 100f)}%", labelStyle);
            return result;
        }

        private void DrawStatusBar(Rect rect, float value, Color color, string label)
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height),
                Texture2D.whiteTexture);
            GUI.color = previous;
            GUI.Label(new Rect(rect.x, rect.y - 2f, rect.width, rect.height + 4f), label, labelStyle);
        }

        private static void Reload(int seed, bool autoStart)
        {
            RunLaunchOptions.Prepare(seed, autoStart);
            Time.timeScale = 1f;
            Scene scene = SceneManager.GetActiveScene();
            if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
            else if (!string.IsNullOrEmpty(scene.name)) SceneManager.LoadScene(scene.name);
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.72f, 0.12f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
