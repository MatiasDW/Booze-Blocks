using System;
using BoozeBlocks.Camera;
using BoozeBlocks.Horde;
using BoozeBlocks.Interaction;
using BoozeBlocks.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class PrototypeStartMenu : MonoBehaviour
    {
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
        private PrototypeHud hud;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle hintStyle;
        private bool configured;

        public bool IsStarted { get; private set; }

        public void Configure(PlayerVitals player, ThirdPersonCamera thirdPersonCamera,
            HordeDirector hordeDirector, PrototypeRound prototypeRound,
            RunProgression runProgression, PrototypeHud prototypeHud)
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
            hud = prototypeHud;
            configured = true;
            OpenMenu(player.transform);
        }

        private void Update()
        {
            if (!configured || IsStarted || Keyboard.current == null) return;

            if (Keyboard.current.aKey.wasPressedThisFrame) appearance.CycleShirt(-1);
            if (Keyboard.current.dKey.wasPressedThisFrame) appearance.CycleShirt(1);
            if (Keyboard.current.wKey.wasPressedThisFrame) appearance.CycleSkin(1);
            if (Keyboard.current.sKey.wasPressedThisFrame) appearance.CycleSkin(-1);
            if (Keyboard.current.qKey.wasPressedThisFrame) appearance.CycleAccessory(-1);
            if (Keyboard.current.eKey.wasPressedThisFrame) appearance.CycleAccessory(1);
            if (Keyboard.current.mKey.wasPressedThisFrame) appearance.ToggleMoustache();
            if (Keyboard.current.enterKey.wasPressedThisFrame ||
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                StartGame();
            }
        }

        private void OnGUI()
        {
            if (!configured || IsStarted) return;
            EnsureStyles();

            Color previousColor = GUI.color;
            GUI.color = new Color(0.02f, 0.04f, 0.04f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            float panelWidth = Mathf.Min(570f, Screen.width - 40f);
            float panelHeight = 520f;
            Rect panel = new Rect(30f, Mathf.Max(20f, (Screen.height - panelHeight) * 0.5f),
                panelWidth, panelHeight);
            GUI.Box(panel, GUIContent.none);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, panel.width - 56f, 46f),
                "BOOZE & BLOCKS", titleStyle);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 67f, panel.width - 56f, 28f),
                "PREPARA A TU ADULTO", hintStyle);

            DrawSelector(panel, 120f, "ROPA", appearance.ShirtName,
                () => appearance.CycleShirt(-1), () => appearance.CycleShirt(1));
            DrawSelector(panel, 185f, "PIEL", appearance.SkinName,
                () => appearance.CycleSkin(-1), () => appearance.CycleSkin(1));
            DrawSelector(panel, 250f, "GORRO", appearance.AccessoryName,
                () => appearance.CycleAccessory(-1), () => appearance.CycleAccessory(1));

            string moustache = appearance.HasMoustache ? "BIGOTE: SI" : "BIGOTE: NO";
            if (GUI.Button(new Rect(panel.x + 115f, panel.y + 323f, panel.width - 230f, 42f), moustache))
            {
                appearance.ToggleMoustache();
            }

            if (GUI.Button(new Rect(panel.x + 82f, panel.y + 390f, panel.width - 164f, 58f),
                    "INICIAR PARRILLADA"))
            {
                StartGame();
            }
            GUI.Label(new Rect(panel.x + 30f, panel.y + 463f, panel.width - 60f, 42f),
                "Mouse o A/D: ropa | W/S: piel | Q/E: gorro | M: bigote | Enter: jugar", hintStyle);
        }

        public void StartGame()
        {
            if (!configured || IsStarted) return;
            IsStarted = true;
            Time.timeScale = 1f;
            SetEnabled(horde, true);
            SetEnabled(round, true);
            SetEnabled(progression, true);
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

        private void OpenMenu(Transform player)
        {
            IsStarted = false;
            Time.timeScale = 0f;
            motor?.SetControlEnabled(false);
            SetEnabled(interaction, false);
            SetEnabled(defense, false);
            SetEnabled(inventory, false);
            SetEnabled(horde, false);
            SetEnabled(round, false);
            SetEnabled(progression, false);
            SetEnabled(hud, false);
            if (cameraController == null) return;
            cameraController.enabled = false;
            UnityEngine.Camera previewCamera = cameraController.GetComponent<UnityEngine.Camera>();
            if (previewCamera == null) return;
            previewCamera.transform.position = player.position + new Vector3(0f, 1.6f, 4.8f);
            previewCamera.transform.LookAt(player.position + Vector3.up * 0.45f);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void DrawSelector(Rect panel, float offsetY, string label, string value,
            Action previous, Action next)
        {
            GUI.Label(new Rect(panel.x + 38f, panel.y + offsetY, 105f, 42f), label, labelStyle);
            if (GUI.Button(new Rect(panel.x + 145f, panel.y + offsetY, 52f, 42f), "<")) previous();
            GUI.Label(new Rect(panel.x + 205f, panel.y + offsetY, panel.width - 402f, 42f), value, labelStyle);
            if (GUI.Button(new Rect(panel.x + panel.width - 92f, panel.y + offsetY, 52f, 42f), ">")) next();
        }

        private void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 30,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.74f, 0.14f) }
            };
            labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            hintStyle = new GUIStyle(labelStyle)
            {
                fontSize = 14,
                normal = { textColor = new Color(0.78f, 0.92f, 0.88f) },
                wordWrap = true
            };
        }

        private static void SetEnabled(Behaviour behaviour, bool enabled)
        {
            if (behaviour != null) behaviour.enabled = enabled;
        }
    }
}
