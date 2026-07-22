using System.IO;
using BoozeBlocks.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoozeBlocks.Editor
{
    public static class PrototypeSceneBuilder
    {
        private const string SceneDirectory = "Assets/_Game/Scenes";
        private const string ScenePath = SceneDirectory + "/Prototype.unity";

        [MenuItem("Booze & Blocks/Create Prototype Scene")]
        public static void CreatePrototypeScene()
        {
            ConfigureProject();
            if (!Directory.Exists(SceneDirectory)) Directory.CreateDirectory(SceneDirectory);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject bootstrap = new GameObject("PrototypeBootstrap");
            bootstrap.AddComponent<PrototypeBootstrap>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            Selection.activeGameObject = bootstrap;
            Debug.Log($"Booze & Blocks prototype created at {ScenePath}");
        }

        private static void ConfigureProject()
        {
            PlayerSettings.companyName = "BoozeBlocks";
            PlayerSettings.productName = "Booze & Blocks";
            PlayerSettings.colorSpace = ColorSpace.Linear;

            Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (settingsAssets.Length == 0) return;

            SerializedObject settings = new SerializedObject(settingsAssets[0]);
            SerializedProperty activeInputHandler = settings.FindProperty("activeInputHandler");
            if (activeInputHandler == null || activeInputHandler.intValue == 1) return;
            activeInputHandler.intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
