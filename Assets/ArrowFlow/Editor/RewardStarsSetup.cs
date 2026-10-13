using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArrowFlow.Editor
{
    // Makes sure the open Main scene has the reward stars on the Win and Fail dialogs and the matching
    // jingles. It runs inside Unity after scripts reload, whenever Main.unity opens and on returning from
    // Play mode, so a scene that was already open while its file changed on disk is fixed too, then saves
    // the scene. Also available from the Arrow Attack menu.
    [InitializeOnLoad]
    public static class RewardStarsSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static RewardStarsSetup()
        {
            EditorApplication.delayCall += () => Apply(false);
            EditorSceneManager.sceneOpened += (scene, mode) =>
            {
                if (scene.path == ScenePath) EditorApplication.delayCall += () => Apply(false);
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += () => Apply(false);
            };
        }

        [MenuItem("Arrow Attack/Add reward stars and jingles")]
        public static void ApplyFromMenu()
        {
            if (!Apply(true)) Debug.Log("Arrow Attack: the reward stars and jingles are already in Main.unity.");
        }

        static bool Apply(bool fromMenu)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || (Application.isBatchMode && !fromMenu)) return false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () => Apply(fromMenu);
                return false;
            }
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath) return false;
            var ui = Object.FindAnyObjectByType<SceneUI>(); // the live UI; the disabled backup is skipped
            if (ui == null) return false;
            bool changed = false;
            changed |= EnsureStars(ui, "Win/Stars", "Win/Card", "Win/Title", new Vector2(0, -133), new Vector2(330, 106), 3);
            changed |= EnsureStars(ui, "Fail/Stars", "Fail/Card", "Fail/Title", new Vector2(0, -120), new Vector2(270, 86), 0);
            changed |= EnsureClips(ui);
            if (!changed) return false;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Arrow Attack: added the reward stars and jingles to Main.unity and saved the scene.");
            return true;
        }

        static SceneUI.Entry Entry(SceneUI ui, string id)
        {
            foreach (var e in ui.elements) if (e.id == id) return e;
            return null;
        }

        static bool EnsureStars(SceneUI ui, string id, string parentId, string afterId, Vector2 position, Vector2 size, int earned)
        {
            var existing = Entry(ui, id);
            if (existing != null && existing.gameObject != null && existing.graphic is StarDisplay) return false;
            var parent = Entry(ui, parentId);
            if (parent == null || parent.gameObject == null) return false;
            var go = new GameObject(id, typeof(RectTransform), typeof(CanvasRenderer), typeof(StarDisplay));
            Undo.RegisterCreatedObjectUndo(go, "Add " + id);
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent.gameObject.transform, false);
            var after = Entry(ui, afterId);
            if (after != null && after.gameObject != null && after.gameObject.transform.parent == rect.parent)
                rect.SetSiblingIndex(after.gameObject.transform.GetSiblingIndex() + 1);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1);
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var stars = go.GetComponent<StarDisplay>();
            stars.raycastTarget = false;
            var serialized = new SerializedObject(stars);
            serialized.FindProperty("earned").intValue = earned;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Undo.RecordObject(ui, "Register " + id);
            if (existing != null) ui.elements.Remove(existing);
            int index = after != null ? ui.elements.IndexOf(after) + 1 : ui.elements.Count;
            ui.elements.Insert(index, new SceneUI.Entry { id = id, gameObject = go, graphic = stars });
            EditorUtility.SetDirty(ui);
            return true;
        }

        static bool EnsureClips(SceneUI ui)
        {
            var audio = ui.gameAudio;
            if (audio == null) return false;
            var complete = audio.levelComplete != null ? audio.levelComplete : Clip("LevelComplete");
            var failed = audio.levelFailed != null ? audio.levelFailed : Clip("LevelFailed");
            if (complete == audio.levelComplete && failed == audio.levelFailed) return false;
            Undo.RecordObject(audio, "Assign level jingles");
            audio.levelComplete = complete;
            audio.levelFailed = failed;
            EditorUtility.SetDirty(audio);
            return true;
        }

        static AudioClip Clip(string name)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ArrowFlow/Audio/" + name + ".wav");
        }
    }
}
