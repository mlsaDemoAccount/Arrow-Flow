using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArrowFlow.Editor
{
    // A batch-only integration check. Player saves are isolated, and editor options restored.
    public static class ReferenceUIPlayCheck
    {
        static bool oldEnabled, finishing;
        static EnterPlayModeOptions oldOptions;
        static double started;
        static int assertions, exitCode;
        const string Report = "Builds/ReferenceUI/play-mode-result.txt";

        public static void ApplyAndRun()
        {
            ReferenceUIBuild.Apply();
            Run();
        }
        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run this check in a separate Unity batch session.");
            HeartVerification.Run();
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
            oldEnabled=EditorSettings.enterPlayModeOptionsEnabled;
            oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            SaveStore.VerificationDirectory=Path.GetFullPath("Builds/ReferenceUI/PlaySave");
            SaveStore.Save(new ArrowFlow.Core.Progress { tutorialSeen=true,sound=false,haptics=false });
            started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick;
            EditorApplication.EnterPlaymode();
        }
        static void Check(bool result,string reason)
        {
            assertions++;
            if(!result) throw new Exception(reason);
        }
        static void Click(SceneUI ui,string id)
        {
            Check(ui.Get(id).gameObject.activeInHierarchy && ui.Get(id).button.interactable,"Clickable control: "+id);
            ui.Get(id).button.onClick.Invoke();
        }
        static void Tick()
        {
            if(finishing)
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                EditorApplication.update-=Tick;
                EditorSettings.enterPlayModeOptionsEnabled=oldEnabled;
                EditorSettings.enterPlayModeOptions=oldOptions;
                SaveStore.VerificationDirectory=null;
                EditorApplication.Exit(exitCode);return;
            }
            try
            {
                if(EditorApplication.timeSinceStartup-started>90) throw new Exception("Play mode startup timed out.");
                if(!EditorApplication.isPlaying) return;
                var app=GameApp.Instance;
                if(app==null) return;
                var ui=app.GetComponent<SceneUI>();
                if(!ui.Get("Screen/Entry").gameObject.activeSelf) return;
                int count=ui.design.GetComponentsInChildren<RectTransform>(true).Length;
                Check(ui.Get("Screen/Entry").gameObject.activeSelf,"Splash advances to Entry");
                Click(ui,"Entry/Play");Check(ui.Get("Screen/Home").gameObject.activeSelf,"Entry opens Home");
                Click(ui,"Home/Levels");Check(ui.Get("Screen/Levels").gameObject.activeSelf,"Levels navigation");
                Click(ui,"Levels/Next");Check(ui.Get("Levels/Level0/Label").text.text=="33","Next page binding");
                Click(ui,"Levels/Previous");Click(ui,"Levels/Level0");
                Check(ui.Get("Screen/Game").gameObject.activeSelf && ui.Get("Game/Title").text.text=="Level 1","Campaign button binding");
                Click(ui,"Game/Guide");Check(app.board.guide,"Path guide binding");
                Click(ui,"Game/Hint");Check(app.attempt.hintsUsed==1,"Hint binding");
                int blocked=app.puzzle.arrows.First(a=>!app.puzzle.CanExit(a.id,app.board.removed)).id;
                app.Tap(blocked);Check(app.attempt.lives==2,"Blocked arrow removes one life");
                Check(ui.Get("Game/Heart2").graphic.color.a<1,"Heart sprite reflects life loss");
                Click(ui,"Game/Restart");Check(ui.Get("Dialog/Restart").gameObject.activeSelf,"Restart confirmation binding");
                Click(ui,"Restart/Confirm");Check(app.attempt.lives==3 && app.attempt.hintsUsed==1,"Restart retains hint usage");
                Click(ui,"Game/Back");Click(ui,"Home/Daily");Check(ui.Get("Screen/Daily").gameObject.activeSelf,"Daily navigation");
                Click(ui,"Daily/Back");Click(ui,"Home/Weekly");Check(ui.Get("Screen/Weekly").gameObject.activeSelf,"Weekly navigation");
                Click(ui,"Weekly/Back");Click(ui,"Home/Trophies");Check(ui.Get("Screen/Trophies").gameObject.activeSelf,"Trophies navigation");
                Click(ui,"Trophies/Back");Click(ui,"Home/Settings");Check(ui.Get("Screen/Settings").gameObject.activeSelf,"Settings navigation");
                Check(ui.gameAudio!=null && ui.gameAudio.musicSource.clip!=null && ui.gameAudio.wrongMove!=null && ui.gameAudio.levelComplete!=null && ui.gameAudio.levelFailed!=null,"Authored audio references");
                Check(!ui.gameAudio.SoundEnabled && !ui.gameAudio.musicSource.isPlaying,"Saved sound-off preference silences music");
                ui.gameAudio.SendMessage("OnApplicationFocus",true);
                Click(ui,"Settings/Sound");Check(ui.gameAudio.SoundEnabled && ui.gameAudio.musicSource.isPlaying,"Sound toggle starts looping music");
                Check(ui.gameAudio.musicSource.loop,"Background track loops");
                ui.gameAudio.PlayMove(false);Check(ui.gameAudio.effectsSource.isPlaying,"Wrong move sound plays");
                ui.gameAudio.SendMessage("OnApplicationPause",true);Check(!ui.gameAudio.musicSource.isPlaying && !ui.gameAudio.effectsSource.isPlaying,"App pause silences audio");
                ui.gameAudio.SendMessage("OnApplicationPause",false);Check(ui.gameAudio.musicSource.isPlaying,"Resume restores music");
                Click(ui,"Settings/Sound");Check(!ui.gameAudio.musicSource.isPlaying,"Sound off pauses music");
                ui.gameAudio.PlayMove(false);Check(!ui.gameAudio.effectsSource.isPlaying,"Sound off suppresses error effect");
                Click(ui,"Settings/Back");Click(ui,"Home/Help");Check(ui.Get("Dialog/Tutorial").gameObject.activeSelf,"Tutorial binding");
                Click(ui,"Tutorial/Close");
                Check(ui.design.GetComponentsInChildren<RectTransform>(true).Length==count,"Navigation creates no UI objects");
                File.WriteAllText(Report,"PASS: "+assertions+" play-mode assertions. Authored splash/entry, persistent navigation, 32-slot paging, gameplay actions, life sprites, restart, zero runtime UI creation, authored music/error effects, sound settings, and audio pause/resume verified. Player saves isolated.");
                Debug.Log(File.ReadAllText(Report));
                finishing=true;EditorApplication.ExitPlaymode();
            }
            catch(Exception error)
            {
                exitCode=1;File.WriteAllText(Report,"FAIL: "+error);Debug.LogException(error);
                finishing=true;EditorApplication.ExitPlaymode();
            }
        }
    }
}


