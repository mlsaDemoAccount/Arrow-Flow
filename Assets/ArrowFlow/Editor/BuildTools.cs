using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using ArrowFlow.Core;

namespace ArrowFlow.Editor
{
    public static class BuildTools
    {
        [MenuItem("Arrow Flow/Prepare project")]
        public static void Prepare()
        {
            Directory.CreateDirectory("Assets/Scenes");
            if (!File.Exists("Assets/Scenes/Main.unity"))
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                SceneCanvasSetup.Create();
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            }

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
            };
            PlayerSettings.companyName = "ArrowFlowStudio";
            PlayerSettings.productName = "Arrow Flow";
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.arrowflowstudio.arrowflow");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.arrowflowstudio.arrowflow");
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.bundleVersionCode = 1;
#if UNITY_2023_1_OR_NEWER
            PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
#endif
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.requiresFullScreen = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 0; settings.ApplyModifiedPropertiesWithoutUndo(); }
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            AssetDatabase.SaveAssets();
            Debug.Log("ARROWFLOW_PREPARED");
        }

        [MenuItem("Arrow Flow/Run logic checks")]
        public static void Validate()
        {
            string result = Checks.Run();
            Directory.CreateDirectory("Builds");
            File.WriteAllText("Builds/logic-checks.txt", result);
            Debug.Log(result);
        }

        public static void PrepareAndValidate()
        {
            Prepare();
            Validate();
        }

        [MenuItem("Arrow Flow/Build Windows preview")]
        public static void Windows()
        {
            Prepare();
            Validate();
            Build("Builds/Windows/ArrowFlow.exe", BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Arrow Flow/Build Android APK")]
        public static void Android()
        {
            Prepare();
            Validate();
            EditorUserBuildSettings.buildAppBundle = false;
            Build("Builds/Android/ArrowFlow.apk", BuildTarget.Android);
        }

        [MenuItem("Arrow Flow/Build Android App Bundle")]
        public static void AndroidBundle()
        {
            Prepare();
            Validate();
            EditorUserBuildSettings.buildAppBundle = true;
            Build("Builds/Android/ArrowFlow.aab", BuildTarget.Android);
        }

        [MenuItem("Arrow Flow/Export iOS Xcode project")]
        public static void IOS()
        {
            Prepare();
            Validate();
            Build("Builds/iOS", BuildTarget.iOS);
        }

        static void Build(string path, BuildTarget target)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = path, target = target, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("ARROWFLOW_BUILD_OK " + path);
        }
    }
}



