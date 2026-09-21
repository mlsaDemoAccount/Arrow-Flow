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
        TournamentEvent tournament;
        AudioClip goodTone, badTone;

        void Awake()
        {
            Instance = this;
            if (ui == null) { Debug.LogError("Assign Scene UI on Arrow Flow. Use Arrow Flow > Build scene UI to migrate an old scene.", this); enabled = false; return; }
            Application.targetFrameRate = 60;
            UnityEngine.Screen.orientation = ScreenOrientation.Portrait;
            progress = SaveStore.Load();
            Ads.Initialize();
            goodTone = Tone(660, .09f); badTone = Tone(190, .12f);
            ui.hero.puzzle = Levels.Tutorial(); ui.hero.interactive = false; ui.hero.hint = 1; ui.hero.Refresh();
            ui.board.tapped = Tap;
            Home();
        }
        void Update()
        {
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
            Ads.SetBannerVisible(name != "Game");
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
            Ads.SetBannerVisible(currentScreen != "Game");
        }
        public void Home()
        {
            if (submitting || deleting || refilling || advancing) return;
            Persist(); Screen("Home");
            int current = Math.Min(Levels.CampaignCount, progress.unlocked);
            bool resume = progress.attempts.Any(a => a.key == "campaign:" + current);
            ui.Text("Home/Play/Label", (resume ? "Continue" : "Play") + "  /  Level " + current);
        }
        public void PlayCampaign() { Play("campaign", Math.Min(Levels.CampaignCount, progress.unlocked), DateTime.Today); }
        public void ShowDaily() { Calendar(DateTime.Today); }
        public void ShowLevels() { LevelMenu((Math.Min(Levels.CampaignCount, progress.unlocked) - 1) / 30); }
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
            attempt.removed.RemoveAll(id => !puzzle.arrows.Any(a => a.id == id));
            attempt.removed = attempt.removed.Distinct().ToList();
            // Keep board progress, but start each play session with three lives.
            attempt.lives = 3;
            Persist();
            ui.Text("Game/Title", kind == "campaign" ? "LEVEL " + index : kind == "daily" ? date.ToString("MMM d").ToUpperInvariant() : (kind == "weekly" ? "WEEKLY / " : "TOURNAMENT / ") + index);
            board.SetPuzzle(puzzle, new HashSet<int>(attempt.removed)); playing = true;
            ui.Text("Game/Status", "Tap an arrow with a clear way out"); UpdateHUD();
            if (attempt.removed.Count == puzzle.arrows.Count) Win();
            else if (!progress.tutorialSeen) { progress.tutorialSeen = true; Persist(); ShowTutorial(); }
        }
        void UpdateHUD()
        {
            ui.Text("Game/Hearts", new string('\u2665', attempt.lives) + new string('\u2661', 3 - attempt.lives));
            ui.Text("Game/Counter", (puzzle.arrows.Count - attempt.removed.Count) + " arrows remaining");
            ui.Get("Game/Progress/Fill").gameObject.transform.localScale = new Vector3((float)attempt.removed.Count / puzzle.arrows.Count, 1, 1);
        }
        public void Tap(int id)
        {
            if (!playing || transitioning || dialog != null || attempt.lives <= 0 || board.Animating || board.removed.Contains(id)) return;
            if (!puzzle.CanExit(id, board.removed))
            {
                attempt.lives--; board.Error(id); Feedback(false); UpdateHUD(); Persist();
                ui.Text("Game/Status", "That path is blocked. Follow the red guide.");
                if (attempt.lives == 0) ShowFail(); return;
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
        public void Hint()
        {
            if (!playing || transitioning || attempt.lives <= 0 || board.Animating) return;
            board.hint = puzzle.Hint(board.removed); board.Refresh(); ui.Text("Game/Status", "The green arrow can leave safely.");
        }
        void ShowFail()
        {
            if (!playing || attempt == null || attempt.lives > 0) return;
            UpdateRefillButton();
            Dialog("Fail");
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
        public void RestartPuzzle() { progress.attempts.RemoveAll(a => a.key == key); Persist(); Play(mode, level, puzzleDate); }
        void Win()
        {
            transitioning = true;
            if (mode == "tournament") { RetrySubmission(); return; }
            Ads.LevelCompleted();
            progress.Complete(mode, level, puzzleDate, key); Persist();
            ui.Text("Win/Body", mode == "daily" ? "Daily star collected.\nAnother small moment of clarity." : mode == "weekly" ? "One more puzzle toward your weekly medal." : "Every arrow found its way.");
            ui.Text("Win/Next/Label", mode == "campaign" && level < 500 ? "Next level" : mode == "weekly" ? "Next puzzle" : "Back home");
            Dialog("Win");
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
            if (mode == "campaign" && level < 500) Play(mode, level + 1, puzzleDate);
            else if (mode == "weekly") Play(mode, progress.WeeklyCount(puzzleDate) + 1, puzzleDate);
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
            Screen("Levels"); levelPage = Mathf.Clamp(pageIndex, 0, (Levels.CampaignCount - 1) / 30);
            int start = levelPage * 30 + 1;
            for (int i = 0; i < 30; i++)
            {
                int n = start + i; string id = "Levels/Level" + i;
                ui.Active(id, n <= Levels.CampaignCount); ui.Text(id + "/Label", n.ToString());
                ui.Interactable(id, n <= progress.unlocked);
            }
            ui.Interactable("Levels/Previous", levelPage > 0); ui.Interactable("Levels/Next", start + 30 <= Levels.CampaignCount);
        }
        public void PreviousLevelPage() { LevelMenu(levelPage - 1); }
        public void NextLevelPage() { LevelMenu(levelPage + 1); }
        public void PlayLevelSlot(int slot)
        {
            int n = levelPage * 30 + slot + 1;
            if (slot >= 0 && slot < 30 && n <= progress.unlocked && n <= 500) Play("campaign", n, DateTime.Today);
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
            ui.Text("Trophies/Medals", "Bronze  " + bronze + "     Silver  " + silver + "     Gold  " + gold);
        }
        public void ShowSettings()
        {
            Screen("Settings"); ui.Text("Settings/Sound/Label", "Sound: " + (progress.sound ? "On" : "Off"));
            ui.Text("Settings/Haptics/Label", "Haptics: " + (progress.haptics ? "On" : "Off"));
            ui.Text("Settings/Reminder/Label", "Daily reminder: " + (progress.reminders ? "On" : "Off"));
            ui.Text("Settings/SaveError", SaveStore.LastError ?? "");
        }
        public void ToggleSound() { progress.sound = !progress.sound; Persist(); ShowSettings(); }
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
            SaveStore.Delete(); progress = new Progress(); Persist(); Home();
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
        AudioClip Tone(float frequency,float duration)
        {
            int samples=(int)(22050*duration);var data=new float[samples];
            for(int i=0;i<samples;i++) data[i]=Mathf.Sin(i/22050f*frequency*Mathf.PI*2)*.15f*Mathf.Sin(Mathf.PI*i/samples);
            var clip=AudioClip.Create("Tone",samples,1,22050,false);clip.SetData(data,0);return clip;
        }
        void Feedback(bool correct)
        {
            if(progress.sound) ui.audioSource.PlayOneShot(correct?goodTone:badTone);
            if(!correct&&progress.haptics&&Application.isMobilePlatform) Handheld.Vibrate();
        }
    }
}
