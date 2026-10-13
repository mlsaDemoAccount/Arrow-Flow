using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using ArrowFlow.Core;
namespace ArrowFlow
{
    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance;
        [SerializeField] SceneUI ui;
        public Progress progress;
        public BoardView board { get { return ui.board; } }
        public Puzzle puzzle;
        public Attempt attempt;
        string mode, key, dialog;
        int level, levelPage, screenVersion;
        DateTime puzzleDate, calendarMonth;
        bool playing, transitioning, submitting, deleting, refilling, advancing;
        string currentScreen;
        bool starting;
        TournamentEvent tournament;

        [Header("Q Solutions splash")]
        [Tooltip("Seconds the Q Solutions screen stays up, including the logo zoom (never shorter than the zoom).")]
        [SerializeField] float splashSeconds = 3f;
        [Tooltip("Logo size when the splash appears, relative to its saved size. It zooms out to 1.")]
        [SerializeField] float splashLogoStartScale = 1.3f;
        [Tooltip("Seconds the logo takes to slowly zoom out and fade in.")]
        [SerializeField] float splashLogoZoomSeconds = 2f;
        Graphic splashLogo;
        Vector3 splashLogoScale;
        Vector2 splashLogoPosition;
        Color splashLogoColor;


        void Awake()
        {
            Instance = this;
            if (ui == null) { Debug.LogError("Assign Scene UI on Arrow Attack. Use Arrow Attack > Build scene UI to migrate an old scene.", this); enabled = false; return; }
            starting = true;
            // Let every screen take this device's shape (phones, tablets, foldables) before anything stores UI positions.
            var fit = ui.design != null ? ui.design.GetComponent<FitDesign>() : null;
            if (fit != null) fit.MakeResponsive(ui);
            Screen("Splash");
            PrepareSplashLogo();
            Application.targetFrameRate = 60;
            UnityEngine.Screen.orientation = ScreenOrientation.Portrait;
            progress = SaveStore.Load();
            if(ui.gameAudio!=null) ui.gameAudio.SetSound(progress.sound);
            ui.hero.puzzle = Levels.Tutorial(); ui.hero.interactive = false; ui.hero.hint = 1; ui.hero.Refresh();
            ui.board.tapped = Tap;
            StartCoroutine(ShowEntryWhenReady());
        }
        IEnumerator ShowEntryWhenReady()
        {
#if !UNITY_EDITOR
            // In builds, Unity's own splash is still fading out over this scene's first frames.
            // Start the logo animation once it has gone; the cap covers platforms that never report it.
            float unitySplashLimit = Time.realtimeSinceStartup + 1.5f;
            while (!UnityEngine.Rendering.SplashScreen.isFinished && Time.realtimeSinceStartup < unitySplashLimit) yield return null;
#endif
            // Count visible rendered frames, not time spent importing, loading or unfocused.
            // A slow first frame must not consume the entire splash duration.
            float visibleSeconds = 0;
            int lastRenderedFrame = Time.renderedFrameCount;
            float splashDuration = Mathf.Max(splashSeconds, splashLogoZoomSeconds);
            while (visibleSeconds < splashDuration)
            {
                yield return null;
                int renderedFrame = Time.renderedFrameCount;
                if (renderedFrame == lastRenderedFrame) continue;
                lastRenderedFrame = renderedFrame;
                if (!Application.isFocused && !Application.isBatchMode) continue;
                visibleSeconds += Mathf.Min(Time.unscaledDeltaTime, .1f);
                AnimateSplashLogo(visibleSeconds);
            }
            RestoreSplashLogo();
            if (currentScreen == "Splash") Screen("Entry");
            starting = false;
            yield return null;
            // Consent dialogs and ad initialization cannot cover the startup artwork.
            Ads.Initialize();
        }
        // The Q Solutions logo starts enlarged and transparent, then zooms out to its saved layout.
        // Only runtime values change; the scene keeps the authored size, position and color.
        void PrepareSplashLogo()
        {
            foreach (var entry in ui.elements)
                if (entry.id == "Splash/Logo") splashLogo = entry.graphic;
            if (splashLogo == null) return;
            var rect = splashLogo.rectTransform;
            splashLogoScale = rect.localScale;
            splashLogoPosition = rect.anchoredPosition;
            splashLogoColor = splashLogo.color;
            AnimateSplashLogo(0);
        }
        void AnimateSplashLogo(float seconds)
        {
            if (splashLogo == null) return;
            float t = splashLogoZoomSeconds > 0 ? Mathf.Clamp01(seconds / splashLogoZoomSeconds) : 1;
            float zoom = Mathf.Sin(t * Mathf.PI * .5f); // Gentle ease-out: slow, even zoom with a soft landing.
            float scale = Mathf.LerpUnclamped(splashLogoStartScale, 1, zoom);
            var rect = splashLogo.rectTransform;
            rect.localScale = splashLogoScale * scale;
            // Scale around the logo's center, whatever its pivot.
            Vector2 pivotToCenter = Vector2.Scale(Vector2.Scale(new Vector2(.5f, .5f) - rect.pivot, rect.rect.size), (Vector2)splashLogoScale);
            rect.anchoredPosition = splashLogoPosition + pivotToCenter * (1 - scale);
            var color = splashLogoColor;
            color.a *= Mathf.SmoothStep(0, 1, Mathf.Clamp01(t / .4f));
            splashLogo.color = color;
        }
        void RestoreSplashLogo()
        {
            if (splashLogo == null) return;
            splashLogo.rectTransform.localScale = splashLogoScale;
            splashLogo.rectTransform.anchoredPosition = splashLogoPosition;
            splashLogo.color = splashLogoColor;
        }
        void Update()
        {
            if (starting) return;
            Ads.Tick();
            if (dialog == "Fail" && !refilling) UpdateRefillButton();
            if (dialog == "About") ui.Active("About/Privacy", Ads.PrivacyOptionsRequired);
            if (!Input.GetKeyDown(KeyCode.Escape)) return;
            if (deleting || submitting || refilling || advancing || Ads.Busy) return;
            if (dialog != null && !(playing && (transitioning || attempt.lives == 0))) CloseDialog();
            else Home();
        }
        void OnApplicationPause(bool paused) { if (paused) Persist(); }
        void OnApplicationQuit() { Persist(); }
        void Persist() { if (progress != null) SaveStore.Save(progress); }
        void Screen(string name)
        {
            screenVersion++; CloseDialog(); playing = false; transitioning = false;
            ui.ShowScreen(name);
            currentScreen = name;
            Ads.SetBannerVisible(name != "Game" && name != "Splash" && name != "Entry");
        }
        void Dialog(string name)
        {
            dialog = name; ui.ShowDialog(name); board.interactive = false;
            Ads.SetBannerVisible(false);
        }
        public void CloseDialog()
        {
            if (submitting || deleting || refilling || advancing) return;
            dialog = null; ui.ShowDialog(null); board.interactive = true;
            Ads.SetBannerVisible(currentScreen != "Game" && currentScreen != "Splash" && currentScreen != "Entry");
        }
        public void Home()
        {
            if (submitting || deleting || refilling || advancing) return;
            Persist(); Screen("Home");
            int current = Math.Min(Levels.CampaignCount, progress.unlocked);
            bool resume = progress.attempts.Any(a => a.key == "campaign:" + current);
            ui.Text("Home/Play/Label", (resume ? "Continue / " : "Play / Level ") + current);
        }
        public void PlayCampaign() { Play("campaign", Math.Min(Levels.CampaignCount, progress.unlocked), DateTime.Today); }
        public void ShowDaily() { Calendar(DateTime.Today); }
        public void ShowLevels() { LevelMenu((Math.Min(Levels.CampaignCount, progress.unlocked) - 1) / 32); }
        public void ShowTutorial() { Dialog("Tutorial"); }
        public void ShowAbout() { Dialog("About"); ui.Active("About/Privacy", Ads.PrivacyOptionsRequired); }
        public void ShowAdPrivacy()
        {
            Ads.ShowPrivacyOptions(error =>
            {
                if (this == null) return;
                if (error != null) ui.Text("About/Body", error);
            });
        }
        public void RequestRestart() { Dialog("Restart"); }
        public void RequestDeleteProgress() { Dialog("Delete"); }
        public void FitView() { board.ResetView(); }
        public void ToggleGuide()
        {
            if (!playing || transitioning) return;
            board.guide = !board.guide; board.Refresh();
            ui.Text("Game/Status", board.guide ? "Guides show each arrow's exit path" : "Path guide off");
        }
        public void Play(string kind, int index, DateTime date)
        {
            Screen("Game"); mode = kind; level = index; puzzleDate = date.Date;
            key = kind == "campaign" ? "campaign:" + index : kind == "daily" ? "daily:" + date.ToString("yyyy-MM-dd") : kind + ":" + Levels.WeekStart(date).ToString("yyyy-MM-dd") + ":" + index;
            puzzle = kind == "campaign" ? Levels.Campaign(index) : kind == "daily" ? Levels.Daily(date) : Levels.Weekly(date, index);
            attempt = progress.GetAttempt(key);
            attempt.PrepareForGeneration(Levels.LayoutVersion(kind));
            attempt.removed.RemoveAll(id => !puzzle.arrows.Any(a => a.id == id));
            attempt.removed = attempt.removed.Distinct().ToList();
            // Resuming keeps both the board and its remaining lives.
            attempt.lives = Mathf.Clamp(attempt.lives, 0, 3);
            attempt.hintsUsed = Mathf.Clamp(attempt.hintsUsed, 0, Attempt.HintLimit);
            Persist();
            ui.Text("Game/Title", kind == "campaign" ? "Level " + index : kind == "daily" ? date.ToString("MMM d").ToUpperInvariant() : (kind == "weekly" ? "WEEKLY / " : "TOURNAMENT / ") + index);
            board.SetPuzzle(puzzle, new HashSet<int>(attempt.removed)); playing = true;
            ui.Text("Game/Status", "Tap an arrow with a clear way out"); UpdateHUD();
            if (attempt.removed.Count == puzzle.arrows.Count) Win();
            else if (attempt.lives == 0) ShowFail();
            else if (!progress.tutorialSeen) { progress.tutorialSeen = true; Persist(); ShowTutorial(); }
        }
        void UpdateHUD()
        {
            ui.Hearts(attempt.lives);
            ui.Text("Game/Hint/Label", "Hint (" + attempt.HintsRemaining + ")");
            ui.Interactable("Game/Hint", attempt.HintsRemaining > 0 && attempt.lives > 0);
            ui.Text("Game/Counter", (puzzle.arrows.Count - attempt.removed.Count) + " arrows remaining");
            ui.Get("Game/Progress/Fill").gameObject.transform.localScale = new Vector3((float)attempt.removed.Count / puzzle.arrows.Count, 1, 1);
        }
        public void Tap(int id)
        {
            if (!playing || transitioning || dialog != null || attempt.lives <= 0 || board.Animating || board.removed.Contains(id)) return;
            if (!puzzle.CanExit(id, board.removed))
            {
                attempt.lives--; board.Error(id); Feedback(false); UpdateHUD(); Persist();
                ui.Text("Game/Status", "Blocked! Clear the arrow in its way first.");
                if (attempt.lives == 0) StartCoroutine(FailAfterBump(screenVersion)); return;
            }
            board.removed.Add(id); attempt.removed.Add(id); board.Exit(id); Feedback(true); UpdateHUD(); Persist();
            ui.Text("Game/Status", "A little more space.");
            if (attempt.removed.Count == puzzle.arrows.Count) StartCoroutine(FinishAfterAnimation(screenVersion));
        }
        IEnumerator FinishAfterAnimation(int version)
        {
            transitioning = true; yield return new WaitForSecondsRealtime(.55f);
            if (playing && screenVersion == version) Win();
        }
        // After the last heart is lost, the arrow bumps into its blocker and slides back before the dialog opens.
        IEnumerator FailAfterBump(int version)
        {
            transitioning = true; yield return new WaitForSecondsRealtime(board.WrongReturnSeconds + .3f);
            if (screenVersion != version) yield break;
            transitioning = false; ShowFail(true);
        }
        public void Hint()
        {
            if (!playing || transitioning || dialog != null || attempt.lives <= 0 || board.Animating || attempt.HintsRemaining <= 0) return;
            int id = puzzle.Hint(board.removed);
            if (id < 0 || board.hint == id) return;
            attempt.hintsUsed++;
            board.hint = id; board.Refresh(); UpdateHUD(); Persist();
            ui.Text("Game/Status", attempt.HintsRemaining == 0 ? "Last hint — the green arrow can leave safely." : "The green arrow can leave safely.");
        }
        // Three gray stars and, when the last heart was just lost, a gentle "you lose" phrase.
        void ShowFail(bool justLost = false)
        {
            if (!playing || attempt == null || attempt.lives > 0) return;
            UpdateRefillButton();
            ui.Stars("Fail/Stars", 0);
            Dialog("Fail");
            if (justLost && ui.gameAudio != null) { ui.gameAudio.SetSound(progress.sound); ui.gameAudio.PlayLevelFailed(); }
        }
        void UpdateRefillButton()
        {
            bool ready = Ads.RewardedReady;
            ui.Interactable("Fail/Refill", ready);
            ui.Text("Fail/Refill/Label", ready ? "Watch ad — refill 3 lives" : "Ad unavailable — try again soon");
        }
        public void RefillLives()
        {
            if (refilling || !playing || dialog != "Fail" || attempt.lives > 0 || !Ads.RewardedReady) return;
            refilling = true;
            var refillAttempt = attempt;
            int version = screenVersion;
            ui.Interactable("Fail/Refill", false);
            Ads.ShowRewarded(() =>
            {
                if (this == null || !progress.attempts.Contains(refillAttempt)) return;
                refillAttempt.lives = 3; Persist();
                if (screenVersion == version && attempt == refillAttempt)
                {
                    UpdateHUD();
                    // Some mediated providers can deliver the reward after dismissal.
                    if (!refilling && dialog == "Fail")
                    {
                        CloseDialog();
                        ui.Text("Game/Status", "Refilled — thanks for watching!");
                    }
                }
            }, earned =>
            {
                if (this == null) return;
                refilling = false;
                if (screenVersion != version || attempt != refillAttempt) return;
                if (earned) { CloseDialog(); ui.Text("Game/Status", "Refilled — thanks for watching!"); }
                else { UpdateRefillButton(); ui.Text("Game/Status", "Ad wasn't completed. Watch an ad to refill, or restart."); }
            });
        }
        public void RestartPuzzle()
        {
            // A fresh board restores lives, but cannot bypass this puzzle's hint limit.
            attempt.removed.Clear(); attempt.lives = 3;
            Persist(); Play(mode, level, puzzleDate);
        }
        void Win()
        {
            transitioning = true;
            if (mode == "tournament") { RetrySubmission(); return; }
            Ads.LevelCompleted();
            progress.Complete(mode, level, puzzleDate, key); Persist();
            // Short messages fit the dialog's single text line.
            ui.Text("Win/Body", mode == "daily" ? "Daily star collected!" : mode == "weekly" ? "One step closer to a medal!" : "Every arrow found its way.");
            ui.Text("Win/Next/Label", WinContinues ? (mode == "weekly" ? "Next puzzle" : "Next level") : "Back home");
            // Three stars pop in while the jingle plays.
            ui.Stars("Win/Stars", 3);
            Dialog("Win");
            if (ui.gameAudio != null) { ui.gameAudio.SetSound(progress.sound); ui.gameAudio.PlayLevelComplete(); }
        }
        // After a win there is always something next: the following campaign level (the current one
        // after a daily puzzle) or the next weekly puzzle, until the campaign is finished.
        bool WinContinues
        {
            get { return mode == "weekly" || (mode == "campaign" ? level < Levels.CampaignCount : progress.unlocked <= Levels.CampaignCount); }
        }
        public void NextPuzzle()
        {
            if (advancing || dialog != "Win") return;
            advancing = true;
            ui.Interactable("Win/Next", false);
            Ads.ShowInterstitialBetweenLevels(() =>
            {
                if (this == null) return;
                advancing = false;
                ui.Interactable("Win/Next", true);
                ContinueAfterWin();
            });
        }
        void ContinueAfterWin()
        {
            if (mode == "campaign" && level < Levels.CampaignCount) Play(mode, level + 1, puzzleDate);
            else if (mode == "weekly") Play(mode, progress.WeeklyCount(puzzleDate) + 1, puzzleDate);
            else if (mode == "daily" && progress.unlocked <= Levels.CampaignCount) Play("campaign", progress.unlocked, DateTime.Today);
            else Home();
        }
        public void Calendar(DateTime month)
        {
            Screen("Daily"); calendarMonth = new DateTime(month.Year, month.Month, 1);
            ui.Text("Daily/Month", calendarMonth.ToString("MMMM yyyy"));
            ui.Interactable("Daily/NextMonth", calendarMonth < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1));
            ui.Interactable("Daily/PreviousMonth", calendarMonth.Year > 1 || calendarMonth.Month > 1);
            int offset = ((int)calendarMonth.DayOfWeek + 6) % 7;
            int days = DateTime.DaysInMonth(month.Year, month.Month), earned = 0;
            for (int slot = 0; slot < 42; slot++)
            {
                int day = slot - offset + 1; bool valid = day >= 1 && day <= days;
                string id = "Daily/Day" + slot; ui.Active(id, valid);
                if (!valid) continue;
                DateTime date = calendarMonth.AddDays(day - 1); bool complete = progress.days.Contains(date.ToString("yyyy-MM-dd"));
                if (complete) earned++;
                ui.Text(id + "/Label", day + (complete ? " *" : "")); ui.Interactable(id, date <= DateTime.Today);
            }
            ui.Text("Daily/Stars", earned + " / " + days + " daily stars");
        }
        public void PreviousMonth() { if (calendarMonth > DateTime.MinValue) Calendar(calendarMonth.AddMonths(-1)); }
        public void NextMonth() { if (calendarMonth.AddMonths(1) <= DateTime.Today) Calendar(calendarMonth.AddMonths(1)); }
        public void PlayDailySlot(int slot)
        {
            int day = slot - (((int)calendarMonth.DayOfWeek + 6) % 7) + 1;
            if (day < 1 || day > DateTime.DaysInMonth(calendarMonth.Year, calendarMonth.Month)) return;
            DateTime date = calendarMonth.AddDays(day - 1); if (date <= DateTime.Today) Play("daily", 0, date);
        }
        void LevelMenu(int pageIndex)
        {
            Screen("Levels"); levelPage = Mathf.Clamp(pageIndex, 0, (Levels.CampaignCount - 1) / 32);
            int start = levelPage * 32 + 1;
            for (int i = 0; i < 32; i++)
            {
                int n = start + i; string id = "Levels/Level" + i;
                ui.Active(id, n <= Levels.CampaignCount); ui.Text(id + "/Label", n.ToString());
                ui.Interactable(id, n <= progress.unlocked);
            }
            ui.Interactable("Levels/Previous", levelPage > 0); ui.Interactable("Levels/Next", start + 32 <= Levels.CampaignCount);
        }
        public void PreviousLevelPage() { LevelMenu(levelPage - 1); }
        public void NextLevelPage() { LevelMenu(levelPage + 1); }
        public void PlayLevelSlot(int slot)
        {
            int n = levelPage * 32 + slot + 1;
            if (slot >= 0 && slot < 32 && n <= progress.unlocked && n <= 500) Play("campaign", n, DateTime.Today);
        }
        public void Weekly()
        {
            Screen("Weekly"); ui.Text("Weekly/Count", progress.WeeklyCount(DateTime.Today).ToString());
            ui.Text("Weekly/Week", "Week of " + Levels.WeekStart(DateTime.Today).ToString("MMM d"));
        }
        public void PlayWeekly() { Play("weekly", progress.WeeklyCount(DateTime.Today) + 1, DateTime.Today); }
        public void ShowTrophies()
        {
            Screen("Trophies");
            var months = progress.days.Where(d => d.Length >= 7).Select(d => d.Substring(0,7)).Distinct().OrderByDescending(d => d).Where(m => progress.MonthComplete(int.Parse(m.Substring(0,4)), int.Parse(m.Substring(5,2)))).ToList();
            int gold = 0, silver = 0, bronze = 0;
            foreach (var group in progress.weeklyWins.GroupBy(w => w.Split(':')[1]))
            { int n = group.Count(); if (n >= 30) gold++; else if (n >= 15) silver++; else if (n >= 5) bronze++; }
            ui.Text("Trophies/Count", months.Count.ToString());
            ui.Text("Trophies/Months", months.Count == 0 ? "Your first trophy is waiting.\nComplete a full month of daily puzzles." : string.Join(" / ", months.Take(6).ToArray()));
            ui.Text("Trophies/Medals", "BRONZE: " + bronze + "     SILVER: " + silver + "     GOLD: " + gold);
        }
        public void ShowSettings()
        {
            Screen("Settings"); ui.Text("Settings/Sound/Label", "Sound: " + (progress.sound ? "On" : "Off"));
            ui.Text("Settings/Haptics/Label", "Haptics: " + (progress.haptics ? "On" : "Off"));
            ui.Text("Settings/Reminder/Label", "Daily reminder: " + (progress.reminders ? "On" : "Off"));
            ui.Text("Settings/SaveError", SaveStore.LastError ?? "");
        }
        public void ToggleSound() { progress.sound = !progress.sound; if(ui.gameAudio!=null) ui.gameAudio.SetSound(progress.sound); Persist(); ShowSettings(); }
        public void ToggleHaptics() { progress.haptics = !progress.haptics; Persist(); ShowSettings(); }
        public void ToggleReminder() { Reminders.Set(!progress.reminders); }
        public void DeleteProgress() { if (!deleting) StartCoroutine(DeleteAllProgress()); }
        IEnumerator DeleteAllProgress()
        {
            deleting = true; ui.Interactable("Delete/Confirm", false); ui.Interactable("Delete/Cancel", false);
            bool deleted = false; yield return TournamentClient.Delete(ok => deleted = ok);
            deleting = false; ui.Interactable("Delete/Confirm", true); ui.Interactable("Delete/Cancel", true);
            if (!deleted) { Notice("Could not delete online scores", "Please reconnect and try again.\nYour progress has been kept."); yield break; }
            if (progress.reminders) Reminders.Set(false);
            SaveStore.Delete(); progress = new Progress(); if(ui.gameAudio!=null) ui.gameAudio.SetSound(progress.sound); Persist(); Home();
        }
        void Notice(string title, string body) { ui.Text("Notice/Title", title); ui.Text("Notice/Body", body); Dialog("Notice"); }
        public void OnReminderPermission(string result)
        {
            progress.reminders = result == "1"; Persist(); ShowSettings();
            if (result == "denied") Notice("Notifications are off", "Allow notifications in your device settings\nto receive your daily reminder at 6 pm.");
            else if (result == "desktop") Notice("Mobile reminders", "Daily reminders are available in the\nAndroid and iOS builds.");
        }
        public void OnlineTournament()
        {
            if (!TournamentClient.Configured) { Notice("Take the weekly challenge", "Online tournaments aren't available right now.\nYour daily and weekly puzzles are ready to play."); return; }
            Screen("TournamentLoading"); StartCoroutine(LoadTournament(screenVersion));
        }
        IEnumerator LoadTournament(int version)
        {
            TournamentEvent data = null; string problem = null;
            yield return TournamentClient.Load((result,error) => { data = result; problem = error; });
            if (version != screenVersion) yield break;
            if (problem != null) { ui.Text("NetworkError/Body", problem); Dialog("NetworkError"); yield break; }
            tournament = data; Screen("Tournament");
            ui.Text("Tournament/Subtitle", "Week of " + DateTime.Parse(data.week).ToString("MMM d") + " / UTC");
            ui.Text("Tournament/Score", "YOUR SCORE  " + data.score + "  /  RANK  " + data.rank);
            for (int i = 0; i < 8; i++)
            {
                string row = "Tournament/Row" + i; bool visible = data.entries != null && i < data.entries.Length;
                ui.Active(row, visible); if (!visible) continue;
                var entry = data.entries[i]; ui.Text(row + "/Rank", entry.rank.ToString()); ui.Text(row + "/Name", entry.name); ui.Text(row + "/Score", entry.score.ToString());
            }
        }
        public void ShowTournamentMedals()
        {
            ui.Text("Medals/Body", tournament == null || tournament.medals == null || tournament.medals.Length == 0 ? "Finish in the top three to earn a medal." : string.Join("\n", Enumerable.Reverse(tournament.medals).Take(6).ToArray())); Dialog("Medals");
        }
        public void PlayTournament()
        {
            if (tournament != null) Play("tournament", tournament.nextLevel, DateTime.ParseExact(tournament.week,"yyyy-MM-dd",null));
        }
        public void RetrySubmission() { if (!submitting) StartCoroutine(SubmitTournament()); }
        IEnumerator SubmitTournament()
        {
            CloseDialog(); submitting = true; Dialog("Submitting");
            var proof = new SolutionProof { week = Levels.WeekStart(puzzleDate).ToString("yyyy-MM-dd"), level = level, moves = attempt.removed.ToArray() };
            string problem = null; yield return TournamentClient.Submit(proof, error => problem = error); submitting = false;
            if (problem != null) { ui.Text("SubmissionError/Body", problem); Dialog("SubmissionError"); yield break; }
            progress.Complete(mode, level, puzzleDate, key); Persist(); Dialog("Submitted");
        }
        void Feedback(bool correct)
        {
            if(ui.gameAudio!=null) { ui.gameAudio.SetSound(progress.sound); ui.gameAudio.PlayMove(correct); }
            if(!correct&&progress.haptics&&Application.isMobilePlatform) Handheld.Vibrate();
        }
    }
}
