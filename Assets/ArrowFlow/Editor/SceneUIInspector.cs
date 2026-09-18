using System;
using UnityEditor;
using UnityEngine;
namespace ArrowFlow.Editor
{
    [CustomEditor(typeof(SceneUI))]
    public sealed class SceneUIInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var ui=(SceneUI)target;
            EditorGUILayout.HelpBox("Every screen is saved under Canvas > Safe area > Design. Preview a screen below, then select a button and inspect Button > On Click. Position, style and static text edits remain in the scene. Preview controls are disabled during Play mode.",MessageType.Info);
            using(new EditorGUI.DisabledScope(Application.isPlaying))
            {
                EditorGUILayout.LabelField("Preview screen",EditorStyles.boldLabel);
                foreach(var entry in ui.elements)if(entry.id.StartsWith("Screen/",StringComparison.Ordinal)&&GUILayout.Button(entry.id.Substring(7)))
                {RecordRoots(ui);ui.ShowDialog(null);ui.ShowScreen(entry.id.Substring(7));Selection.activeGameObject=entry.gameObject;EditorUtility.SetDirty(ui);}
                EditorGUILayout.LabelField("Preview dialog",EditorStyles.boldLabel);
                foreach(var entry in ui.elements)if(entry.id.StartsWith("Dialog/",StringComparison.Ordinal)&&GUILayout.Button(entry.id.Substring(7)))
                {RecordRoots(ui);ui.ShowDialog(entry.id.Substring(7));Selection.activeGameObject=entry.gameObject;EditorUtility.SetDirty(ui);}
                if(GUILayout.Button("Hide dialogs")){RecordRoots(ui);ui.ShowDialog(null);}
            }
            DrawDefaultInspector();
        }
        static void RecordRoots(SceneUI ui){foreach(var e in ui.elements)if(e.id.StartsWith("Screen/",StringComparison.Ordinal)||e.id.StartsWith("Dialog/",StringComparison.Ordinal))Undo.RecordObject(e.gameObject,"Preview scene UI");}
    }
}
