using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ArrowFlow.Core;
using Progress = ArrowFlow.Core.Progress;

namespace ArrowFlow.Editor
{
    // Run with -batchmode -executeMethod ArrowFlow.Editor.HeartVerification.Run.
    // Exercises the real scene and game handlers, with saves isolated under Builds.
    public static class HeartVerification
    {
        static int assertions;
        static readonly string Output = Path.GetFullPath("Builds/HeartVerification");
        static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void Run()
        {
            if (EditorApplication.isPlaying || !Application.isBatchMode)
                throw new InvalidOperationException("Run heart verification in a separate Unity batch session.");
            Directory.CreateDirectory(Output);
            SaveStore.VerificationDirectory = Path.Combine(Output, "Save");
            assertions = 0;
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                var app = UnityEngine.Object.FindAnyObjectByType<GameApp>();
                var ui = app.GetComponent<SceneUI>();
                var hearts = ui.Get("Game/Hearts").graphic as HeartDisplay;
                Check(hearts != null && !hearts.raycastTarget, "Scene uses non-interactive heart graphics.");
                app.progress = new Progress { tutorialSeen = true, sound = false, haptics = false };
                app.Play("campaign", 1, DateTime.Today);
                Check(app.attempt.lives == 3, "New puzzle starts with three lives.");
                Capture(ui, "three-hearts", 720, 1280);
                int safe = app.puzzle.arrows.Last(a => app.puzzle.CanExit(a.id, app.board.removed)).id;
                app.Tap(safe);
                Check(app.attempt.lives == 3 && app.attempt.removed.Contains(safe), "Safe move preserves lives.");
                app.Home();
                app.PlayCampaign();
                Check(app.attempt.removed.Contains(safe), "Resume preserves removed arrows.");
                int blocked = app.puzzle.arrows.First(a => !app.board.removed.Contains(a.id) && !app.puzzle.CanExit(a.id, app.board.removed)).id;
                for (int remaining = 2; remaining >= 0; remaining--)
                {
                    app.Tap(blocked);
                    Check(app.attempt.lives == remaining, "Blocked move removes exactly one heart.");
                    Check(hearts.Lives == remaining, "Visible hearts match remaining lives.");
                    Capture(ui, remaining + "-hearts", 720, 1280);
                    app.Home();
                    app.progress = SaveStore.Load();
                    app.PlayCampaign();
                    Check(app.attempt.lives == remaining, "Save/load and resume preserve remaining hearts.");
                    Check(app.attempt.removed.Contains(safe), "Save/load preserves board progress.");
                }
                Check(ui.Get("Dialog/Fail").gameObject.activeSelf, "Zero-heart resume opens failure dialog.");
                app.Tap(blocked);
                Check(app.attempt.lives == 0, "Further taps cannot make lives negative.");
                int count = app.attempt.removed.Count;
                app.RefillLives();
                Check(app.attempt.lives == 0 && app.attempt.removed.Count == count, "Unavailable ad cannot grant a refill.");
                app.RestartPuzzle();
                Check(app.attempt.lives == 3 && app.attempt.removed.Count == 0, "Restart restores hearts and clears board progress.");
                Check(!ui.Get("Dialog/Fail").gameObject.activeSelf, "Restart closes failure dialog.");
                Capture(ui, "small-phone", 360, 640);
                Capture(ui, "tall-phone", 1080, 2400);
                CaptureLegacy(ui, hearts);
                VerifyHints(app, ui);
                SceneUIBuilder.Validate();
                File.WriteAllText(Path.Combine(Output, "result.txt"), "PASS: " + assertions + " heart and hint lifecycle assertions; real Unity scene rendered at 360x640, 720x1280, 1080x2400.\n" + Checks.Run());
                Debug.Log(File.ReadAllText(Path.Combine(Output, "result.txt")));
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(Output, "result.txt"), "FAIL: " + error);
                throw;
            }
            finally { SaveStore.VerificationDirectory = null; }
        }

        static void VerifyHints(GameApp app, SceneUI ui)
        {
            Check(app.attempt.HintsRemaining == 3, "New attempt has three hints.");
            app.ShowTutorial();
            app.Hint();
            Check(app.attempt.hintsUsed == 0, "Dialog blocks hint use.");
            Capture(ui, "hint-tutorial", 720, 1280);
            app.CloseDialog();
            for (int remaining = 2; remaining >= 0; remaining--)
            {
                app.Hint();
                Check(app.attempt.HintsRemaining == remaining, "A new hint spends one allowance.");
                Check(app.board.hint >= 0 && app.puzzle.CanExit(app.board.hint, app.board.removed), "Hint highlights a safe arrow.");
                Check(ui.Get("Game/Hint/Label").text.text == "Hint (" + remaining + ")", "Button displays remaining hints.");
                Check(ui.Get("Game/Hint").button.interactable == (remaining > 0), "Button is disabled exactly when exhausted.");
                app.Hint();
                Check(app.attempt.HintsRemaining == remaining, "Repeated hint cannot spend twice or exceed the limit.");
                Capture(ui, "hints-" + remaining, 360, 640);
                app.Home();
                app.progress = SaveStore.Load();
                app.PlayCampaign();
                Check(app.attempt.HintsRemaining == remaining, "Hint usage survives save/load/resume.");
                app.RestartPuzzle();
                Check(app.attempt.HintsRemaining == remaining, "Restart cannot refill hints.");
            }
            app.Hint();
            Check(app.attempt.hintsUsed == Attempt.HintLimit && app.board.hint == -1, "Exhausted handler cannot reveal another hint.");
            app.Play("campaign", 2, DateTime.Today);
            Check(app.attempt.HintsRemaining == 3, "Another puzzle has its own hint allowance.");
            int safe = app.puzzle.Hint(app.board.removed);
            app.Tap(safe);
            app.Hint();
            Check(app.attempt.hintsUsed == 0, "Hints cannot be spent during an arrow animation.");
            app.Play("campaign", 1, DateTime.Today);
            Check(app.attempt.HintsRemaining == 0, "Switching puzzles cannot reset the allowance.");
            var legacy = JsonUtility.FromJson<Progress>("{\"version\":1,\"unlocked\":1,\"days\":[],\"weeklyWins\":[],\"attempts\":[{\"key\":\"campaign:1\",\"lives\":3,\"removed\":[]}]}");
            Check(SaveStore.Save(legacy) && SaveStore.Load().GetAttempt("campaign:1").HintsRemaining == 3, "Older saves start with three hints.");
            legacy.GetAttempt("campaign:1").hintsUsed = 999;
            Check(SaveStore.Save(legacy) && SaveStore.Load().GetAttempt("campaign:1").hintsUsed == 3, "Corrupt over-limit usage is clamped.");
            legacy.GetAttempt("campaign:1").hintsUsed = -1;
            Check(SaveStore.Save(legacy) && SaveStore.Load().GetAttempt("campaign:1").hintsUsed == 0, "Corrupt negative usage is clamped.");
        }

        static void Capture(SceneUI ui, string name, int width, int height)
        {
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            var target = new RenderTexture(width, height, 24);
            var previous = RenderTexture.active;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target;
                ui.canvas.renderMode = RenderMode.ScreenSpaceCamera;
                ui.canvas.worldCamera = camera;
                ui.canvas.planeDistance = 10;
                ui.canvas.GetComponent<CanvasScaler>().enabled = false;
                ui.canvas.scaleFactor = 1;
                var safeArea = ui.design.parent.GetComponent<SafeArea>();
                safeArea.enabled = false;
                var safeRect = (RectTransform)ui.design.parent;
                safeRect.anchorMin = Vector2.zero;
                safeRect.anchorMax = Vector2.one;
                safeRect.offsetMin = safeRect.offsetMax = Vector2.zero;
                var fit = ui.design.GetComponent<FitDesign>();
                fit.enabled = false;
                fit.FitTo(new Vector2(width, height));
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                pixels.Apply();
                File.WriteAllBytes(Path.Combine(Output, name + ".png"), pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous;
                camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(pixels);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static void CaptureLegacy(SceneUI ui, HeartDisplay hearts)
        {
            // Keep a reference image of the old font-based HUD for visual comparison.
            var legacy = new GameObject("Legacy hearts reference", typeof(RectTransform), typeof(Text));
            try
            {
                var rect = legacy.GetComponent<RectTransform>();
                rect.SetParent(hearts.transform, false);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var text = legacy.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize = 25;
                text.alignment = TextAnchor.MiddleCenter;
                text.color = hearts.color;
                text.text = "\u2665\u2665\u2661";
                hearts.enabled = false;
                Capture(ui, "legacy-two-hearts", 720, 1280);
            }
            finally
            {
                hearts.enabled = true;
                UnityEngine.Object.DestroyImmediate(legacy);
            }
        }
    }
}
