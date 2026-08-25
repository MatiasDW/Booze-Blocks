using System.IO;
using BoozeBlocks.Prototype;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
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

        [MenuItem("Booze & Blocks/Build macOS App")]
        public static void BuildMacRelease()
        {
            ConfigureProject();
            if (!File.Exists(ScenePath))
            {
                throw new FileNotFoundException("Create the prototype scene before building.", ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            string requestedPath = System.Environment.GetEnvironmentVariable("BOOZE_BUILD_PATH");
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string outputPath = string.IsNullOrWhiteSpace(requestedPath)
                ? Path.Combine(projectRoot, "Builds", "macOS", "BoozeAndBlocks.app")
                : requestedPath;
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"macOS build failed: {report.summary.result}");
            }

            Debug.Log($"macOS app ready at {outputPath} ({report.summary.totalSize / (1024f * 1024f):0.0} MB)");
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
