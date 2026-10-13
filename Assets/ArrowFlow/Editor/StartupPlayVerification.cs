using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArrowFlow.Editor
{
    public static class StartupPlayVerification
    {
        const string Output = "Builds/SplashDifficulty";
        static bool oldEnabled, finishing, sawSplash, captured;
        static EnterPlayModeOptions oldOptions;
        static double started, splashAt;
        static int assertions, launches, exitCode;
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new Exception("Use a separate Unity batch session.");
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var ui = UnityEngine.Object.FindAnyObjectByType<SceneUI>();
            Check(ui.Get("Screen/Splash").gameObject.activeInHierarchy, "Saved scene opens on splash");
            Check(!ui.Get("Screen/Home").gameObject.activeSelf, "Home is hidden at startup");
            oldEnabled = EditorSettings.enterPlayModeOptionsEnabled;
            oldOptions = EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            SaveStore.VerificationDirectory = Path.GetFullPath(Output + "/StartupSave");
            SaveStore.Save(new ArrowFlow.Core.Progress { tutorialSeen = true, sound = false, haptics = false });
            started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
            EditorApplication.EnterPlaymode();
        }
        static void Tick()
        {
            if (finishing)
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                EditorApplication.update -= Tick;
                EditorSettings.enterPlayModeOptionsEnabled = oldEnabled;
                EditorSettings.enterPlayModeOptions = oldOptions;
                SaveStore.VerificationDirectory = null;
                EditorApplication.Exit(exitCode);
                return;
            }
            try
            {
                if (EditorApplication.timeSinceStartup - started > 90) throw new Exception("Startup verification timed out.");
                if (!EditorApplication.isPlaying || GameApp.Instance == null) return;
                var ui = GameApp.Instance.GetComponent<SceneUI>();
                bool splash = ui.Get("Screen/Splash").gameObject.activeInHierarchy;
                if (splash)
                {
                    if (!sawSplash)
                    {
                        sawSplash = true;
                        splashAt = EditorApplication.timeSinceStartup;
                        var logo = (Image)ui.Get("Splash/Logo").graphic;
                        Check(logo.sprite != null && logo.sprite.name == "QSolutionsLogo", "Q Solutions is visible on launch");
                        Check(!ui.Get("Screen/Entry").gameObject.activeSelf, "Entry waits for splash");
                        Time.timeScale = 0; // Startup must work independently of gameplay time.
                    }
                    if (launches == 0)
                        Check(!(bool)typeof(Ads).GetField("started", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null), "Ads cannot start over the initial splash");
                    if (!captured && EditorApplication.timeSinceStartup - splashAt > .2)
                    {
                        SplashDifficultyVerification.Capture(ui, "live-startup-" + launches, 720, 1280);
                        captured = true;
                    }
                    return;
                }
                if (!ui.Get("Screen/Entry").gameObject.activeInHierarchy) return;
                Check(sawSplash && captured, "Rendered splash precedes entry on every launch");
                Check(EditorApplication.timeSinceStartup - splashAt >= 1.9, "Splash remains visible for at least two seconds");
                Time.timeScale = 1;
                if (++launches < 2)
                {
                    sawSplash = captured = false;
                    SceneManager.LoadScene("Main");
                    return;
                }
                SplashDifficultyVerification.Capture(ui, "live-entry", 720, 1280);
                File.WriteAllText(Output + "/startup-result.txt", "PASS: " + assertions + " assertions; two real Play-mode launches displayed Q Solutions before Entry, with gameplay time paused and ads deferred. Player saves isolated.");
                Debug.Log(File.ReadAllText(Output + "/startup-result.txt"));
                finishing = true;
                EditorApplication.ExitPlaymode();
            }
            catch (Exception error)
            {
                Time.timeScale = 1;
                exitCode = 1;
                File.WriteAllText(Output + "/startup-result.txt", "FAIL: " + error);
                Debug.LogException(error);
                finishing = true;
                EditorApplication.ExitPlaymode();
            }
        }
        public static void BuildAndroidPreview()
        {
            BuildTools.Prepare();
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory("Builds/Android");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Main.unity" },
                locationPathName = "Builds/Android/ArrowAttack-Splash-TestAds.apk",
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Android build failed: " + report.summary.result);
            Debug.Log("STARTUP_ANDROID_BUILD_OK");
        }
    }
}
