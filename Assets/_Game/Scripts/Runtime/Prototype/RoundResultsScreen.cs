using BoozeBlocks.Camera;
using BoozeBlocks.Horde;
using BoozeBlocks.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class RoundResultsScreen : MonoBehaviour
    {
        private PrototypeRound round;
        private RunSessionDirector session;
        private HordeDirector horde;
        private PlayerMotor motor;
        private ThirdPersonCamera cameraController;
        private NetworkGameplayCoordinator networkGameplay;
        private OnlineSessionController onlineSession;
        private GUIStyle titleStyle;
        private GUIStyle resultStyle;
        private GUIStyle statStyle;

        public bool IsVisible { get; private set; }

        public void Configure(PrototypeRound prototypeRound, RunSessionDirector runSession,
            HordeDirector hordeDirector, PlayerMotor playerMotor, ThirdPersonCamera thirdPersonCamera,
            NetworkGameplayCoordinator networkCoordinator = null, OnlineSessionController onlineController = null)
        {
            if (round != null) round.Finished -= Show;
            round = prototypeRound;
            session = runSession;
            horde = hordeDirector;
            motor = playerMotor;
            cameraController = thirdPersonCamera;
            networkGameplay = networkCoordinator;
            onlineSession = onlineController;
            if (round != null) round.Finished += Show;
        }

        private void OnDestroy()
        {
            if (round != null) round.Finished -= Show;
        }

        private void Show(PrototypeRoundState state)
        {
            IsVisible = true;
            Time.timeScale = 0f;
            if (horde != null) horde.enabled = false;
            motor?.SetControlEnabled(false);
            if (cameraController != null) cameraController.enabled = false;
        }

        private void OnGUI()
        {
            if (!IsVisible || round == null || session == null) return;
            EnsureStyles();

            Color previous = GUI.color;
            GUI.color = new Color(0.015f, 0.025f, 0.02f, 0.86f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float width = Mathf.Min(680f, Screen.width - 32f);
            float height = Mathf.Min(590f, Screen.height - 32f);
            Rect panel = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none);
            string result = round.State == PrototypeRoundState.Won ? "PARRILLADA SALVADA" : "LA HORDA ARRUINO EL ASADO";
            GUI.Label(new Rect(panel.x + 24f, panel.y + 22f, panel.width - 48f, 44f), result, titleStyle);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 74f, panel.width - 48f, 46f),
                $"PUNTAJE {session.Score.Score:N0}", resultStyle);
            GUI.Label(new Rect(panel.x + 55f, panel.y + 136f, panel.width - 110f, 150f),
                $"Oleada alcanzada       {horde?.CurrentWave ?? 1}\n" +
                $"Tiempo resistido        {Mathf.FloorToInt(round.ElapsedTime / 60f):00}:{Mathf.FloorToInt(round.ElapsedTime % 60f):00}\n" +
                $"Platos servidos         {session.Score.Servings}\n" +
                $"Entradas bloqueadas     {session.Score.Barricades}\n" +
                $"Distracciones usadas    {session.Score.Distractions}\n" +
                $"Rescates                 {session.Score.Rescues}\n" +
                $"Comida quemada          {session.Score.BurnedMeals}", statStyle);
            GUI.Label(new Rect(panel.x + 45f, panel.y + 300f, panel.width - 90f, 52f),
                $"Semilla {session.RunSeed} | Crisis: {session.CrisisName}", statStyle);

            if (networkGameplay != null && networkGameplay.IsMatchStarted)
            {
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 434f, panel.width - 140f, 54f),
                        "SALIR DE LA SALA Y VOLVER AL MENU"))
                    onlineSession?.LeaveSession(() => Reload(0, false));
            }
            else
            {
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 374f, panel.width - 140f, 48f),
                        "REINTENTAR MISMA SEMILLA")) Reload(session.RunSeed, true);
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 434f, panel.width - 140f, 48f),
                        "NUEVA PARTIDA")) Reload(0, true);
                if (GUI.Button(new Rect(panel.x + 70f, panel.y + 494f, panel.width - 140f, 48f),
                        "VOLVER AL MENU")) Reload(0, false);
            }
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
                fontSize = 27,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.72f, 0.12f) }
            };
            resultStyle = new GUIStyle(titleStyle)
            {
                fontSize = 22,
                normal = { textColor = new Color(0.18f, 0.92f, 0.72f) }
            };
            statStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
    }
}
