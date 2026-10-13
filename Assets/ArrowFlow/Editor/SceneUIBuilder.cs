using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ArrowFlow.Core;
namespace ArrowFlow.Editor
{
    public static class SceneUIBuilder
    {
        [Serializable] public class Layout { public Node[] nodes; }
        [Serializable] public class Node
        {
            public string id, parent, kind, text, color, method, sprite, font, decoration;
            public float outline, rotation;
            public bool preserveAspect;
            public float x, y, width, height, radius, pivotX;
            public int fontSize, alignment, argument;
            public bool bold, active, hasArgument;
        }
        const string LayoutPath = "Assets/ArrowFlow/Editor/Layout/SceneLayout.json";
        [MenuItem("Arrow Attack/Build scene UI")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play mode before building scene UI."); return; }
            var scene = EditorSceneManager.GetActiveScene();
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            // Validate handlers before changing the user's scene.
            foreach (var n in layout.nodes) if (n.kind == "button")
            {
                var signature = n.hasArgument ? new[] { typeof(int) } : Type.EmptyTypes;
                if (typeof(GameApp).GetMethod(n.method, signature) == null) throw new InvalidOperationException("Missing public button method: " + n.method);
            }
            var oldRoots = scene.GetRootGameObjects();
            var created = new List<GameObject>();
            Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Build editable Arrow Attack UI");
            try
            {
                var canvasObject = Root("Canvas", created, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(720,1280); scaler.matchWidthOrHeight = .5f;
                var safe = new GameObject("Safe area",typeof(RectTransform),typeof(SafeArea)).GetComponent<RectTransform>(); safe.SetParent(canvas.transform,false); safe.anchorMin = Vector2.zero; safe.anchorMax = Vector2.one; safe.offsetMin = safe.offsetMax = Vector2.zero;
                var design = new GameObject("Design",typeof(RectTransform),typeof(FitDesign)).GetComponent<RectTransform>(); design.SetParent(safe,false); design.anchorMin = design.anchorMax = design.pivot = new Vector2(.5f,.5f); design.sizeDelta = new Vector2(720,1280);
                var controller = Root("Arrow Attack",created,typeof(GameApp),typeof(SceneUI),typeof(AudioSource));
                var app = controller.GetComponent<GameApp>(); var ui = controller.GetComponent<SceneUI>(); ui.canvas=canvas;ui.design=design;ui.audioSource=controller.GetComponent<AudioSource>();ui.audioSource.playOnAwake=false;
                var links = new Dictionary<string,RectTransform> { {"Design",design} };
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                foreach (var n in layout.nodes)
                {
                    var r = new GameObject(n.id,typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(links[n.parent],false);r.anchorMin=r.anchorMax=new Vector2(.5f,1);r.pivot=new Vector2(n.pivotX,1);r.anchoredPosition=new Vector2(n.x,-n.y);r.sizeDelta=new Vector2(n.width,n.height);links.Add(n.id,r);
                    Color color;if(!ColorUtility.TryParseHtmlString(n.color,out color))color=Color.white;
                    r.localRotation=Quaternion.Euler(0,0,n.rotation);
                    Graphic graphic=null;Text text=null;Button button=null;
                    switch(n.kind)
                    {
                        case "hearts":
                            var hearts=r.gameObject.AddComponent<HeartDisplay>();hearts.raycastTarget=false;graphic=hearts;break;
                        case "stars":
                            var stars=r.gameObject.AddComponent<StarDisplay>();stars.raycastTarget=false;if(n.id.StartsWith("Fail/",StringComparison.Ordinal))stars.Earned=0;graphic=stars;break;
                        case "text":
                            text=r.gameObject.AddComponent<Text>();text.font=string.IsNullOrEmpty(n.font)?font:AssetDatabase.LoadAssetAtPath<Font>(n.font);text.text=n.text;text.fontSize=n.fontSize;text.fontStyle=n.bold?FontStyle.Bold:FontStyle.Normal;text.alignment=(TextAnchor)n.alignment;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;graphic=text;
                            if(n.outline>0){var stroke=r.gameObject.AddComponent<TextStroke>();stroke.width=n.outline;}
                            break;
                        case "panel":case "button":
                            if(!string.IsNullOrEmpty(n.sprite)){var skin=r.gameObject.AddComponent<Image>();skin.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(n.sprite);skin.preserveAspect=n.preserveAspect;graphic=skin;}
                            else if(!string.IsNullOrEmpty(n.decoration)){var skin=r.gameObject.AddComponent<ReferenceDecoration>();skin.style=(ReferenceDecoration.Style)Enum.Parse(typeof(ReferenceDecoration.Style),n.decoration);graphic=skin;}
                            else {var panel=r.gameObject.AddComponent<RoundedPanel>();panel.radius=n.radius;graphic=panel;}
                            graphic.raycastTarget=n.kind=="button"||n.parent.StartsWith("Dialog/",StringComparison.Ordinal);break;
                        case "image":case "overlay":
                            var img=r.gameObject.AddComponent<Image>();if(!string.IsNullOrEmpty(n.sprite))img.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(n.sprite);img.preserveAspect=n.preserveAspect;graphic=img;graphic.raycastTarget=n.kind=="overlay";break;
                        case "decoration":
                            var decoration=r.gameObject.AddComponent<ReferenceDecoration>();decoration.style=(ReferenceDecoration.Style)Enum.Parse(typeof(ReferenceDecoration.Style),n.decoration);decoration.raycastTarget=false;graphic=decoration;break;
                        case "mask":r.gameObject.AddComponent<RectMask2D>();break;
                        case "board":
                            var board=r.gameObject.AddComponent<BoardView>();board.puzzle=Levels.Tutorial();board.interactive=n.id!="Home/Hero";board.raycastTarget=board.interactive;board.hint=board.interactive?-1:1;graphic=board;if(board.interactive)ui.board=board;else ui.hero=board;break;
                    }
                    if(graphic!=null)graphic.color=color;
                    if(n.kind=="button")
                    {
                        button=r.gameObject.AddComponent<Button>();button.targetGraphic=graphic;
                        var states=button.colors;states.highlightedColor=new Color(1,1,1,1);states.pressedColor=new Color(.78f,.85f,.9f,1);states.disabledColor=new Color(.68f,.75f,.8f,.72f);button.colors=states;
                        if(n.hasArgument)
                        {
                            var call=(UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>),app,n.method);
                            UnityEventTools.AddIntPersistentListener(button.onClick,call,n.argument);
                        }
                        else
                        {
                            var call=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),app,n.method);
                            UnityEventTools.AddPersistentListener(button.onClick,call);
                        }
                    }
                    ui.elements.Add(new SceneUI.Entry { id=n.id,gameObject=r.gameObject,text=text,button=button,graphic=graphic });
                    r.gameObject.SetActive(n.active);
                }
                var heartDisplay=ui.Get("Game/Hearts").graphic as HeartDisplay;
                if(links.ContainsKey("Game/Heart0")){heartDisplay.slots=new Image[3];for(int i=0;i<3;i++)heartDisplay.slots[i]=links["Game/Heart"+i].GetComponent<Image>();heartDisplay.RefreshSlots();}
                GameAudioAuthoring.Configure(ui);
                var serialized=new SerializedObject(app);serialized.FindProperty("ui").objectReferenceValue=ui;serialized.ApplyModifiedPropertiesWithoutUndo();
                var cameraObject=Root("Camera",created,typeof(Camera),typeof(AudioListener));var camera=cameraObject.GetComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color32(35,174,237,255);camera.orthographic=true;camera.transform.position=new Vector3(0,0,-10);
                Root("EventSystem",created,typeof(EventSystem),typeof(StandaloneInputModule));
                // Preserve the old setup as an inactive backup. Do not destroy scene edits.
                GameObject backup=null;
                foreach(var old in oldRoots)
                {
                    if(old.GetComponentInChildren<Canvas>(true)==null&&old.GetComponentInChildren<GameApp>(true)==null&&old.GetComponentInChildren<Camera>(true)==null&&old.GetComponentInChildren<EventSystem>(true)==null)continue;
                    if(old.name.StartsWith("Previous UI",StringComparison.Ordinal))continue;
                    if(backup==null){backup=Root("Previous UI (disabled backup)",created);backup.SetActive(false);}
                    Undo.RecordObject(old,"Rename previous setup"); old.name = "Previous - " + old.name;
                    Undo.SetTransformParent(old.transform,backup.transform,"Preserve previous scene setup");
                }
                EditorSceneManager.MarkSceneDirty(scene);Selection.activeObject=canvasObject;
                Debug.Log("All Arrow Attack UI and persistent button events are in the scene. Save with Ctrl+S. Use the SceneUI Inspector to preview screens. The previous setup is preserved in an inactive backup.");
                Undo.CollapseUndoOperations(undo);
            }
            catch { Undo.RevertAllDownToGroup(undo);throw; }
        }
        static GameObject Root(string name,List<GameObject> created,params Type[] components)
        {var go=new GameObject(name,components);Undo.RegisterCreatedObjectUndo(go,"Create "+name);created.Add(go);return go;}
        [MenuItem("Arrow Attack/Validate scene UI")]
        public static void Validate()
        {
            var ui=UnityEngine.Object.FindAnyObjectByType<SceneUI>();if(ui==null)throw new InvalidOperationException("No active SceneUI found.");
            if(!(ui.Get("Game/Hearts").graphic is HeartDisplay))throw new InvalidOperationException("Game/Hearts must use HeartDisplay.");
            var ids=new HashSet<string>();int buttons=0;
            foreach(var e in ui.elements)
            {
                if(!ids.Add(e.id)||e.gameObject==null)throw new InvalidOperationException("Invalid reference: "+e.id);
                if(e.button==null)continue;buttons++;
                if(e.button.onClick.GetPersistentEventCount()<1)throw new InvalidOperationException("No persistent On Click: "+e.id);
                for(int i=0;i<e.button.onClick.GetPersistentEventCount();i++)if(e.button.onClick.GetPersistentTarget(i)==null||string.IsNullOrEmpty(e.button.onClick.GetPersistentMethodName(i)))throw new InvalidOperationException("Broken On Click: "+e.id);
            }
            if(ui.board==null||ui.hero==null||ui.audioSource==null)throw new InvalidOperationException("Missing game presentation reference.");
            Debug.Log("Scene UI validation passed: "+ids.Count+" elements, "+buttons+" buttons with persistent events.");
        }
    }
}

