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
        [MenuItem("Arrow Attack/Prepare project")]
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
            Branding.Apply();
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
            // iPad runs the portrait game full screen. Older Xcode versions reject a portrait-only iPad app without
            // this. Apple deprecated it in iPadOS 26; if iPadOS resizes the window anyway, FitDesign adapts the layout.
            PlayerSettings.iOS.requiresFullScreen = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            // 64-bit phones and tablets, plus older and budget devices that run 32-bit Android.
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.iOS, ScriptingImplementation.IL2CPP);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SetSetting(settings, "activeInputHandler", 0);
            // Every phone and tablet: iOS builds for iPhone and iPad; Android draws behind camera cutouts and lets
            // the game resize (split screen, foldables, desktop windows). FitDesign adapts the layout to each shape.
            SetSetting(settings, "targetDevice", 2);
            SetSetting(settings, "androidRenderOutsideSafeArea", 1);
            SetSetting(settings, "androidResizeableActivity", 1);
            settings.ApplyModifiedPropertiesWithoutUndo();
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 4;
            AssetDatabase.SaveAssets();
            Debug.Log("ARROWFLOW_PREPARED");
        }

        static void SetSetting(SerializedObject settings, string name, int value)
        {
            var property = settings.FindProperty(name);
            if (property == null) return;
            if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = value != 0;
            else property.intValue = value;
        }

        [MenuItem("Arrow Attack/Run logic checks")]
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

        [MenuItem("Arrow Attack/Build Windows preview")]
        public static void Windows()
        {
            Prepare();
            Validate();
            Build("Builds/Windows/ArrowAttack.exe", BuildTarget.StandaloneWindows64);
        }

        [MenuItem("Arrow Attack/Build Android APK")]
        public static void Android()
        {
            Prepare();
            Validate();
            EditorUserBuildSettings.buildAppBundle = false;
            Build("Builds/Android/ArrowAttack.apk", BuildTarget.Android);
        }

        [MenuItem("Arrow Attack/Build Android Test Ads APK")]
        public static void AndroidTestAds()
        {
            Prepare();
            Validate();
            EditorUserBuildSettings.buildAppBundle = false;
            Build("Builds/Android/ArrowAttack-TestAds.apk", BuildTarget.Android, BuildOptions.Development);
        }

        [MenuItem("Arrow Attack/Build Android App Bundle")]
        public static void AndroidBundle()
        {
            Prepare();
            Validate();
            EditorUserBuildSettings.buildAppBundle = true;
            Build("Builds/Android/ArrowAttack.aab", BuildTarget.Android);
        }

        [MenuItem("Arrow Attack/Export iOS Xcode project")]
        public static void IOS()
        {
            Prepare();
            Validate();
            Build("Builds/iOS", BuildTarget.iOS);
        }

        static void Build(string path, BuildTarget target, BuildOptions options = BuildOptions.None)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/Main.unity" }, locationPathName = path, target = target, options = options });
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("ARROWFLOW_BUILD_OK " + path);
        }
    }
}



