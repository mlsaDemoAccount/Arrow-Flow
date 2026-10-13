using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ArrowFlow.Core;

namespace ArrowFlow.Editor
{
    public static class ReferenceUIBuild
    {
        const string Output = "Builds/ReferenceUI";
        [MenuItem("Arrow Attack/Apply reference screen design")]
        public static void Apply()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play mode first.");
            AssetDatabase.Refresh();
            foreach(string path in Directory.GetFiles("Assets/ArrowFlow/Art/Reference", "*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite;
                importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;
                importer.alphaIsTransparency=true;
                var settings=new TextureImporterSettings(); importer.ReadTextureSettings(settings); settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
                importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.Uncompressed;
                importer.maxTextureSize=2048;
                importer.SaveAndReimport();
            }
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            SceneUIBuilder.Build();
            SceneUIBuilder.Validate();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Verify();
        }
        public static void Verify()
        {
            Directory.CreateDirectory(Output);
            SaveStore.VerificationDirectory=Path.GetFullPath(Path.Combine(Output,"Save"));
            try
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                var ui=UnityEngine.Object.FindAnyObjectByType<SceneUI>();
                var app=ui.GetComponent<GameApp>();
                app.progress=new ArrowFlow.Core.Progress { tutorialSeen=true,sound=false,haptics=false,unlocked=500 };
                foreach(var e in ui.elements)
                {
                    if(e.graphic!=null && e.graphic.GetComponent<CanvasRenderer>()==null) throw new Exception("Missing CanvasRenderer: "+e.id);
                    if(e.text!=null && e.text.font==null) throw new Exception("Missing font: "+e.id);
                    if(e.graphic is Image img && e.id!="Game/Progress/Fill" && !e.id.StartsWith("Dialog/") && img.sprite==null)
                        throw new Exception("Missing sprite: "+e.id);
                }
                SceneUIBuilder.Validate();
                foreach(var name in new[]{"Splash","Entry","Home","Daily","Levels","Weekly","Trophies","Settings","TournamentLoading","Tournament"})
                {
                    ui.ShowDialog(null);ui.ShowScreen(name);
                    if(name != "Splash") Require(ui.Get(name+"/Background").gameObject.activeInHierarchy,"Screen background must remain visible");
                    Capture(ui,name,720,1280);
                }
                app.Play("campaign",1,DateTime.Today); Capture(ui,"Game",720,1280);
                ui.ShowDialog("Win");Capture(ui,"Win",720,1280);ui.ShowDialog(null);
                foreach(var name in new[]{"Tutorial","Fail","Restart","About","Delete","Notice","NetworkError","SubmissionError","Submitted","Submitting","Medals"})
                {ui.ShowDialog(name);Capture(ui,"Dialog-"+name,720,1280);}
                ui.ShowDialog(null);
                app.ShowLevels();
                Require(ui.Get("Levels/Level19/Label").text.text=="500","Final campaign page reaches 500");
                Require(!ui.Get("Levels/Level20").gameObject.activeSelf,"Final page hides nonexistent levels");
                app.progress.unlocked=1;app.ShowLevels();
                Require(ui.Get("Levels/Level31/Label").text.text=="32","32 slots on the first campaign page");
                Require(!ui.Get("Levels/Level1").button.interactable,"Locked levels stay locked");
                app.NextLevelPage();Require(ui.Get("Levels/Level0/Label").text.text=="33","Next page starts at 33");
                app.PreviousLevelPage();Require(ui.Get("Levels/Level0/Label").text.text=="1","Previous page returns to 1");
                app.Calendar(new DateTime(2024,2,1));
                Require(ui.elements.Count(e=>e.id.StartsWith("Daily/Day") && e.button!=null && e.gameObject.activeSelf)==29,"Leap February has 29 dates");
                app.Calendar(new DateTime(2026,3,1));
                Require(ui.Get("Daily/Day36").gameObject.activeSelf,"Six-row month is represented");
                Capture(ui,"Daily-six-rows",720,1280);
                app.Home();Capture(ui,"Home-small",360,640);Capture(ui,"Home-tall",1080,2400);
                app.progress.unlocked=500;app.Home();Capture(ui,"Home-level-500",720,1280);
                Require(ui.Get("Home/Play/Label").text.preferredWidth<=((RectTransform)ui.Get("Home/Play/Label").gameObject.transform).rect.width,"Three-digit level label fits");
                app.Play("campaign",1,DateTime.Today);Capture(ui,"Game-small",360,640);Capture(ui,"Game-tall",1080,2400);
                ui.Hearts(1);Require(ui.Get("Game/Heart0").graphic.color.a==1 && ui.Get("Game/Heart1").graphic.color.a<1,"Separate heart Images follow lives");
                File.WriteAllText(Path.Combine(Output,"result.txt"),"PASS: "+ui.elements.Count+" serialized UI elements. Sprites/fonts, persistent callbacks, 32-slot campaign paging, locked/final levels, leap/six-row calendar, separate hearts, and real Unity renders verified.\n"+Checks.Run());
                Debug.Log("REFERENCE_UI_VERIFIED: "+File.ReadAllText(Path.Combine(Output,"result.txt")));
            }
            finally {SaveStore.VerificationDirectory=null;}
        }
        static void Require(bool result,string message){if(!result)throw new Exception(message);}
        static void Capture(SceneUI ui,string name,int width,int height)
        {
            var camera=UnityEngine.Object.FindAnyObjectByType<Camera>();
            var target=new RenderTexture(width,height,24);
            var previous=RenderTexture.active;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=target;ui.canvas.renderMode=RenderMode.ScreenSpaceCamera;ui.canvas.worldCamera=camera;ui.canvas.planeDistance=10;
                ui.canvas.GetComponent<CanvasScaler>().enabled=false;ui.canvas.scaleFactor=1;
                ui.design.parent.GetComponent<SafeArea>().enabled=false;
                var safe=(RectTransform)ui.design.parent;safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
                var fit=ui.design.GetComponent<FitDesign>();fit.enabled=false;fit.FitTo(new Vector2(width,height));
                Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);}
        }
    }
}



