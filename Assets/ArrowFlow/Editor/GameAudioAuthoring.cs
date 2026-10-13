using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArrowFlow.Editor
{
    public static class GameAudioAuthoring
    {
        public static void Configure(SceneUI ui)
        {
            var audio=ui.GetComponent<GameAudio>();
            if(audio==null)audio=ui.gameObject.AddComponent<GameAudio>();
            ui.gameAudio=audio;
            audio.effectsSource=ui.audioSource;
            audio.effectsSource.playOnAwake=false;
            audio.effectsSource.spatialBlend=0;
            audio.effectsSource.volume=.8f;
            if(audio.musicSource==null)
            {
                var go=new GameObject("Background Music",typeof(AudioSource));
                go.transform.SetParent(ui.transform,false);
                audio.musicSource=go.GetComponent<AudioSource>();
            }
            audio.musicSource.clip=Clip("BackgroundLoop");
            audio.musicSource.loop=true;
            audio.musicSource.playOnAwake=false;
            audio.musicSource.spatialBlend=0;
            audio.musicSource.volume=.28f;
            audio.musicSource.priority=192;
            audio.correctMove=Clip("CorrectMove");
            audio.wrongMove=Clip("WrongMove");
            audio.levelComplete=Clip("LevelComplete");
            audio.levelFailed=Clip("LevelFailed");
            EditorUtility.SetDirty(audio);EditorUtility.SetDirty(ui);
        }
        static AudioClip Clip(string name)
        {
            var clip=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/ArrowFlow/Audio/"+name+".wav");
            if(clip==null)throw new InvalidOperationException("Missing authored audio: "+name);
            return clip;
        }
        [MenuItem("Arrow Attack/Apply game audio")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            var ui=UnityEngine.Object.FindAnyObjectByType<SceneUI>();
            Configure(ui);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("Authored background music and move sounds saved in Main.unity.");
        }
        public static void ApplyAndVerify() { Apply(); ReferenceUIPlayCheck.Run(); }
    }
}
