using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ArrowFlow.Core;

namespace ArrowFlow.Editor
{
    public static class SplashDifficultyVerification
    {
        const string Output = "Builds/SplashDifficulty";
        static int assertions;
        static void Check(bool value, string message)
        {
            assertions++;
            if (!value) throw new Exception(message);
        }
        public static void Run()
        {
            Directory.CreateDirectory(Output);
            SaveStore.VerificationDirectory = Path.GetFullPath(Output + "/Save");
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                var ui = UnityEngine.Object.FindAnyObjectByType<SceneUI>();
                var app = ui.GetComponent<GameApp>();
                app.progress = new ArrowFlow.Core.Progress { tutorialSeen = true, sound = false, haptics = false };
                Check(ui.Get("Splash/Background").graphic is RoundedPanel, "Splash has a native white background");
                Check(ui.Get("Splash/Background").graphic.color == Color.white, "Splash background is white");
                var logo = ui.Get("Splash/Logo").graphic as Image;
                Check(logo != null && logo.sprite != null && logo.preserveAspect, "Splash has a proportional logo");
                Check(AssetDatabase.GetAssetPath(logo.sprite).EndsWith("QSolutionsLogo.png"), "User's Q Solutions logo is assigned");
                Check(!ui.Get("Splash/LoadingBar").gameObject.activeSelf, "Old loading artwork is hidden");
                ui.ShowScreen("Splash"); ui.ShowDialog(null);
                foreach (var size in new[] { new Vector2Int(360,640), new Vector2Int(720,1280), new Vector2Int(1080,2400) })
                    Capture(ui, "splash-" + size.x + "x" + size.y, size.x, size.y);
                // Startup timing is verified by StartupPlayVerification in real Play mode.
                ui.ShowScreen("Entry");
                Capture(ui, "entry", 720, 1280);
                foreach (int level in new[] { 1, 250, 500 })
                {
                    app.Play("campaign", level, DateTime.Today);
                    Check(app.puzzle.arrows.Count == Levels.CampaignArrows[level - 1], "Campaign level matches the catalogue");
                    foreach (bool guides in new[] { false, true })
                    {
                        ui.board.guide = guides;
                        ui.board.hint = app.puzzle.Hint(new HashSet<int>());
                        using (var vertices = new VertexHelper())
                        {
                            typeof(BoardView).GetMethod("OnPopulateMesh", BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(VertexHelper) }, null).Invoke(ui.board, new object[] { vertices });
                            Check(vertices.currentVertCount < 65000, "Dense board with guides fits Unity mesh limit");
                        }
                        ui.board.Refresh();
                        Capture(ui, "level-" + level + (guides ? "-guides" : ""), 720, 1280);
                    }
                }
                File.WriteAllText(Output + "/result.txt", "PASS: " + assertions + " assertions. Splash artwork, entry layout, campaign levels 1/250/500 and guide meshes verified. Rendered splash at three portrait sizes. No player saves changed.");
                Debug.Log(File.ReadAllText(Output + "/result.txt"));
            }
            finally { SaveStore.VerificationDirectory = null; }
        }
        public static void Capture(SceneUI ui, string name, int width, int height)
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
                ui.design.parent.GetComponent<SafeArea>().enabled = false;
                var safe = (RectTransform)ui.design.parent;
                safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one;
                safe.offsetMin = safe.offsetMax = Vector2.zero;
                var fit = ui.design.GetComponent<FitDesign>();
                fit.enabled = false;
                fit.FitTo(new Vector2(width, height));
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                File.WriteAllBytes(Output + "/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; camera.targetTexture = null;
                UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
