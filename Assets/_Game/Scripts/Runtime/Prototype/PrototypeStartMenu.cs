using System;
using BoozeBlocks.Camera;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Prototype
{
    public enum StartMenuPage
    {
        Main,
        Multiplayer,
        Customization,
        Options
    }

    [DisallowMultipleComponent]
    public sealed class PrototypeStartMenu : MonoBehaviour
    {
        private static readonly string[] PrivacyNames = { "Publica", "Solo por codigo" };

        private PlayerAppearance appearance;
        private PlayerMotor motor;
        private PlayerInteraction interaction;
        private PlayerDefenseController defense;
        private PlayerInventory inventory;
        private PlayerActionFeedback feedback;
        private ThirdPersonCamera cameraController;
        private HordeDirector horde;
        private PrototypeRound round;
        private RunProgression progression;
        private RunSessionDirector session;
        private PrototypeHud hud;
        private GameSettingsController settings;
        private OnlineSessionController onlineSession;
        private NetworkGameplayCoordinator networkGameplay;
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle labelStyle;
        private GUIStyle hintStyle;
        private GUIStyle sectionStyle;
        private bool configured;
        private int lobbyCapacity = 4;
        private int privacyIndex;
        private string joinCode = string.Empty;

        public bool IsStarted { get; private set; }
        public StartMenuPage CurrentPage { get; private set; }

        public void Configure(PlayerVitals player, ThirdPersonCamera thirdPersonCamera,
            HordeDirector hordeDirector, PrototypeRound prototypeRound,
            RunProgression runProgression, RunSessionDirector runSession, PrototypeHud prototypeHud,
            GameSettingsController gameSettings, OnlineSessionController onlineController,
            bool startImmediately = false)
        {
            appearance = player.GetComponent<PlayerAppearance>();
            motor = player.GetComponent<PlayerMotor>();
            interaction = player.GetComponent<PlayerInteraction>();
            defense = player.GetComponent<PlayerDefenseController>();
            inventory = player.GetComponent<PlayerInventory>();
            feedback = player.GetComponent<PlayerActionFeedback>();
            cameraController = thirdPersonCamera;
            horde = hordeDirector;
            round = prototypeRound;
            progression = runProgression;
            session = runSession;
            hud = prototypeHud;
            settings = gameSettings;
            onlineSession = onlineController;
            configured = true;
            OpenMenu(player.transform);
            if (startImmediately) StartGame();
        }

        private void Update()
        {
            if (!configured || IsStarted || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame && CurrentPage != StartMenuPage.Main)
            {
                OpenPage(StartMenuPage.Main);
            }
            if (CurrentPage == StartMenuPage.Main && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartGame();
            }
        }

        private void OnGUI()
        {
            if (!configured || IsStarted) return;
            EnsureStyles();
            DrawBackdrop();

            float width = Mathf.Min(720f, Screen.width - 32f);
            float height = Mathf.Min(690f, Screen.height - 32f);
            Rect panel = new Rect(24f, Mathf.Max(16f, (Screen.height - height) * 0.5f), width, height);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 18f, panel.width - 48f, 48f),
                "BOOZE & BLOCKS", titleStyle);

            switch (CurrentPage)
            {
                case StartMenuPage.Multiplayer: DrawMultiplayer(panel); break;
                case StartMenuPage.Customization: DrawCustomization(panel); break;
                case StartMenuPage.Options: DrawOptions(panel); break;
                default: DrawMain(panel); break;
            }
        }

        public void OpenPage(StartMenuPage page)
        {
            CurrentPage = page;
        }

        public void SetNetworkGameplay(NetworkGameplayCoordinator coordinator)
        {
            networkGameplay = coordinator;
        }

        public void StartGame()
        {
            if (!configured || IsStarted) return;
            IsStarted = true;
            Time.timeScale = 1f;
            SetEnabled(horde, true);
            SetEnabled(round, true);
            SetEnabled(progression, true);
            SetEnabled(session, true);
            SetEnabled(hud, true);
            SetEnabled(interaction, true);
            SetEnabled(defense, true);
            SetEnabled(inventory, true);
            motor?.SetControlEnabled(true);
            if (cameraController != null)
            {
                cameraController.enabled = true;
                cameraController.SnapToTarget();
            }
            feedback?.Show("Parrillada iniciada: acercate a un objeto y pulsa E", 3.5f);
        }

        private void DrawMain(Rect panel)
        {
            GUI.Label(new Rect(panel.x + 42f, panel.y + 72f, panel.width - 84f, 32f),
                "PARRILLADA COOPERATIVA DE SUPERVIVENCIA", subtitleStyle);
            GUI.Label(new Rect(panel.x + 48f, panel.y + 105f, panel.width - 96f, 46f),
                $"Partida {session?.RunSeed} | Crisis: {session?.CrisisName}\n{session?.CrisisDescription}", hintStyle);

            float buttonWidth = Mathf.Min(420f, panel.width - 100f);
            float x = panel.x + (panel.width - buttonWidth) * 0.5f;
            if (GUI.Button(new Rect(x, panel.y + 170f, buttonWidth, 58f), "JUGAR SOLO")) StartGame();
            if (GUI.Button(new Rect(x, panel.y + 240f, buttonWidth, 58f), "MULTIJUGADOR"))
                OpenPage(StartMenuPage.Multiplayer);
            if (GUI.Button(new Rect(x, panel.y + 310f, buttonWidth, 58f), "PERSONALIZAR PERSONAJE"))
                OpenPage(StartMenuPage.Customization);
            if (GUI.Button(new Rect(x, panel.y + 380f, buttonWidth, 58f), "OPCIONES"))
                OpenPage(StartMenuPage.Options);
            if (GUI.Button(new Rect(x, panel.y + 450f, buttonWidth, 48f), "SALIR")) Application.Quit();

            GUI.Label(new Rect(panel.x + 42f, panel.y + 525f, panel.width - 84f, 80f),
                "Enter: jugar | Mouse: menus\nOnline hasta 8 jugadores es el objetivo. El prototipo actual ejecuta la simulacion local.", hintStyle);
        }

        private void DrawMultiplayer(Rect panel)
        {
            DrawPageHeader(panel, "MULTIJUGADOR", "Salas de 2 a 8 jugadores mediante Unity Relay.");
            DrawSelector(panel, 135f, "CAPACIDAD", $"{lobbyCapacity} jugadores",
                () => lobbyCapacity = Mathf.Max(2, lobbyCapacity - 1),
                () => lobbyCapacity = Mathf.Min(8, lobbyCapacity + 1));
            DrawSelector(panel, 200f, "PRIVACIDAD", PrivacyNames[privacyIndex],
                () => privacyIndex = Wrap(privacyIndex - 1, PrivacyNames.Length),
                () => privacyIndex = Wrap(privacyIndex + 1, PrivacyNames.Length));
            GUI.Label(new Rect(panel.x + 55f, panel.y + 265f, 145f, 40f), "REGION", labelStyle);
            GUI.Label(new Rect(panel.x + 215f, panel.y + 265f, panel.width - 270f, 40f),
                "Automatica (menor latencia)", labelStyle);

            GUI.Label(new Rect(panel.x + 55f, panel.y + 346f, 160f, 38f), "CODIGO DE SALA", labelStyle);
            joinCode = GUI.TextField(new Rect(panel.x + 230f, panel.y + 346f, panel.width - 285f, 38f),
                joinCode.ToUpperInvariant(), 8);

            bool canUseButtons = onlineSession != null && onlineSession.IsProjectLinked && !onlineSession.IsBusy;
            GUI.enabled = canUseButtons && onlineSession != null && !onlineSession.HasActiveSession;
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 415f, panel.width - 140f, 48f), "CREAR SALA ONLINE"))
                onlineSession.CreateSession(lobbyCapacity, privacyIndex > 0);
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 475f, panel.width - 140f, 48f), "UNIRSE CON CODIGO"))
                onlineSession.JoinSession(joinCode);
            GUI.enabled = true;
            GUI.Label(new Rect(panel.x + 65f, panel.y + 530f, panel.width - 130f, 52f),
                onlineSession?.StatusText ?? "La capa online no esta disponible.", hintStyle);

            bool inOnlineSession = onlineSession != null && onlineSession.HasActiveSession;
            if (inOnlineSession)
            {
                string startLabel = onlineSession.IsHost ? "INICIAR PARTIDA ONLINE" : "ESPERANDO AL ANFITRION";
                GUI.enabled = onlineSession.IsHost && networkGameplay != null && networkGameplay.CanHostStart;
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 590f, 260f, 46f), startLabel))
                    networkGameplay.StartOnlineMatch();
                GUI.enabled = !onlineSession.IsBusy;
                if (GUI.Button(new Rect(panel.x + panel.width - 330f, panel.y + 590f, 260f, 46f), "SALIR DE LA SALA"))
                    onlineSession.LeaveSession();
                GUI.enabled = true;
            }
            else
            {
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 590f, 260f, 46f), "PRACTICA OFFLINE")) StartGame();
                if (GUI.Button(new Rect(panel.x + panel.width - 330f, panel.y + 590f, 260f, 46f), "VOLVER"))
                    OpenPage(StartMenuPage.Main);
            }
        }

        private void DrawCustomization(Rect panel)
        {
            DrawPageHeader(panel, "PERSONALIZAR", appearance.CurrentDescription);
            float half = panel.width * 0.5f;
            DrawCompactSelector(panel.x + 35f, panel.y + 130f, half - 50f, "ROPA", appearance.ShirtName,
                () => appearance.CycleShirt(-1), () => appearance.CycleShirt(1));
            DrawCompactSelector(panel.x + half + 15f, panel.y + 130f, half - 50f, "PIEL", appearance.SkinName,
                () => appearance.CycleSkin(-1), () => appearance.CycleSkin(1));
            DrawCompactSelector(panel.x + 35f, panel.y + 220f, half - 50f, "PANTALON", appearance.PantsName,
                () => appearance.CyclePants(-1), () => appearance.CyclePants(1));
            DrawCompactSelector(panel.x + half + 15f, panel.y + 220f, half - 50f, "CABELLO", appearance.HairName,
                () => appearance.CycleHair(-1), () => appearance.CycleHair(1));
            DrawCompactSelector(panel.x + 35f, panel.y + 310f, half - 50f, "GORRO", appearance.AccessoryName,
                () => appearance.CycleAccessory(-1), () => appearance.CycleAccessory(1));
            DrawCompactSelector(panel.x + half + 15f, panel.y + 310f, half - 50f, "ROSTRO", appearance.FacialHairName,
                () => appearance.CycleFacialHair(-1), () => appearance.CycleFacialHair(1));

            string glasses = appearance.HasGlasses ? "LENTES: SI" : "LENTES: NO";
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 420f, 250f, 44f), glasses)) appearance.ToggleGlasses();
            if (GUI.Button(new Rect(panel.x + panel.width - 320f, panel.y + 420f, 250f, 44f), "LOOK ALEATORIO"))
                appearance.CyclePreset();

            GUI.Label(new Rect(panel.x + 45f, panel.y + 485f, panel.width - 90f, 54f),
                "Todos los looks usan colores y primitivas compartidas: no aumentan el peso con texturas.", hintStyle);
            if (GUI.Button(new Rect(panel.x + 70f, panel.y + 565f, 260f, 50f), "JUGAR CON ESTE LOOK")) StartGame();
            if (GUI.Button(new Rect(panel.x + panel.width - 330f, panel.y + 565f, 260f, 50f), "VOLVER"))
                OpenPage(StartMenuPage.Main);
        }

        private void DrawOptions(Rect panel)
        {
            DrawPageHeader(panel, "OPCIONES", "Los cambios se guardan automaticamente en este Mac.");
            GUI.Label(new Rect(panel.x + 40f, panel.y + 112f, panel.width - 80f, 30f), "VIDEO", sectionStyle);
            DrawSelector(panel, 145f, "CALIDAD", settings.QualityName,
                () => settings.CycleQuality(-1), () => settings.CycleQuality(1));
            DrawSelector(panel, 197f, "RESOLUCION", settings.ResolutionName,
                () => settings.CycleResolution(-1), () => settings.CycleResolution(1));
            if (GUI.Button(new Rect(panel.x + 55f, panel.y + 253f, 285f, 38f),
                    settings.FullScreen ? "PANTALLA COMPLETA: SI" : "PANTALLA COMPLETA: NO"))
                settings.ToggleFullScreen();
            if (GUI.Button(new Rect(panel.x + panel.width - 340f, panel.y + 253f, 285f, 38f),
                    settings.VSync ? "VSYNC: SI" : "VSYNC: NO")) settings.ToggleVSync();
            DrawSelector(panel, 300f, "LIMITE", settings.FrameRateName,
                () => settings.CycleFrameRate(-1), () => settings.CycleFrameRate(1));

            GUI.Label(new Rect(panel.x + 40f, panel.y + 357f, panel.width - 80f, 30f), "AUDIO Y CONTROL", sectionStyle);
            settings.SetMasterVolume(DrawSlider(panel, 392f, "VOLUMEN MAESTRO", settings.MasterVolume));
            settings.SetMusicVolume(DrawSlider(panel, 431f, "MUSICA", settings.MusicVolume));
            settings.SetEffectsVolume(DrawSlider(panel, 470f, "EFECTOS", settings.EffectsVolume));
            settings.SetMouseSensitivity(DrawSlider(panel, 509f, "SENSIBILIDAD", settings.MouseSensitivity,
                0.04f, 0.30f));

            if (GUI.Button(new Rect(panel.x + 55f, panel.y + 548f, 285f, 38f),
                    settings.CameraShake ? "MOVIMIENTO DE CAMARA: SI" : "MOVIMIENTO DE CAMARA: NO"))
                settings.ToggleCameraShake();
            if (GUI.Button(new Rect(panel.x + panel.width - 340f, panel.y + 548f, 285f, 38f),
                    settings.Subtitles ? "AVISOS VISUALES: SI" : "AVISOS VISUALES: NO"))
                settings.ToggleSubtitles();
            if (GUI.Button(new Rect(panel.x + 45f, panel.y + 598f, 190f, 42f), "APLICAR")) settings.Apply();
            if (GUI.Button(new Rect(panel.x + (panel.width - 190f) * 0.5f, panel.y + 598f, 190f, 42f),
                    "RESTAURAR")) settings.ResetDefaults();
            if (GUI.Button(new Rect(panel.x + panel.width - 235f, panel.y + 598f, 190f, 42f), "VOLVER"))
                OpenPage(StartMenuPage.Main);
        }

        private void OpenMenu(Transform player)
        {
            IsStarted = false;
            CurrentPage = StartMenuPage.Main;
            Time.timeScale = 0f;
            horde?.ResetForMenu();
            motor?.SetControlEnabled(false);
            SetEnabled(interaction, false);
            SetEnabled(defense, false);
            SetEnabled(inventory, false);
            SetEnabled(horde, false);
            SetEnabled(round, false);
            SetEnabled(progression, false);
            SetEnabled(session, false);
            SetEnabled(hud, false);
            if (cameraController == null) return;
            cameraController.enabled = false;
            UnityEngine.Camera previewCamera = cameraController.GetComponent<UnityEngine.Camera>();
            if (previewCamera == null) return;
            previewCamera.transform.position = player.position + new Vector3(0f, 1.6f, 4.8f);
            previewCamera.transform.LookAt(player.position + Vector3.left * 1.65f + Vector3.up * 0.45f);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void DrawPageHeader(Rect panel, string title, string subtitle)
        {
            GUI.Label(new Rect(panel.x + 36f, panel.y + 70f, panel.width - 72f, 34f), title, subtitleStyle);
            GUI.Label(new Rect(panel.x + 45f, panel.y + 100f, panel.width - 90f, 30f), subtitle, hintStyle);
        }

        private void DrawSelector(Rect panel, float offsetY, string label, string value,
            Action previous, Action next)
        {
            GUI.Label(new Rect(panel.x + 55f, panel.y + offsetY, 145f, 40f), label, labelStyle);
            if (GUI.Button(new Rect(panel.x + 215f, panel.y + offsetY, 48f, 40f), "<")) previous();
            GUI.Label(new Rect(panel.x + 270f, panel.y + offsetY, panel.width - 380f, 40f), value, labelStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 103f, panel.y + offsetY, 48f, 40f), ">")) next();
        }

        private void DrawCompactSelector(float x, float y, float width, string label, string value,
            Action previous, Action next)
        {
            GUI.Label(new Rect(x, y, width, 26f), label, sectionStyle);
            if (GUI.Button(new Rect(x, y + 32f, 42f, 38f), "<")) previous();
            GUI.Label(new Rect(x + 48f, y + 32f, width - 96f, 38f), value, labelStyle);
            if (GUI.Button(new Rect(x + width - 42f, y + 32f, 42f, 38f), ">")) next();
        }

        private float DrawSlider(Rect panel, float offsetY, string label, float value,
            float minimum = 0f, float maximum = 1f)
        {
            GUI.Label(new Rect(panel.x + 55f, panel.y + offsetY, 190f, 30f), label, labelStyle);
            float result = GUI.HorizontalSlider(new Rect(panel.x + 260f, panel.y + offsetY + 9f,
                panel.width - 365f, 22f), value, minimum, maximum);
            GUI.Label(new Rect(panel.x + panel.width - 95f, panel.y + offsetY, 55f, 30f),
                $"{Mathf.RoundToInt(Mathf.InverseLerp(minimum, maximum, result) * 100f)}%", labelStyle);
            return result;
        }

        private static void DrawBackdrop()
        {
            Color previous = GUI.color;
            GUI.color = new Color(0.015f, 0.035f, 0.03f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.74f, 0.14f) }
            };
            subtitleStyle = new GUIStyle(titleStyle)
            {
                fontSize = 20,
                normal = { textColor = new Color(0.20f, 0.90f, 0.72f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            hintStyle = new GUIStyle(labelStyle)
            {
                fontSize = 13,
                normal = { textColor = new Color(0.78f, 0.92f, 0.88f) },
                wordWrap = true
            };
            sectionStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14,
                normal = { textColor = new Color(1f, 0.72f, 0.16f) }
            };
        }

        private static int Wrap(int value, int count)
        {
            return (value % count + count) % count;
        }

        private static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null) behaviour.enabled = enabled;
        }
    }
}
