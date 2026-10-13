using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrowFlow.Core
{
    public static class Checks
    {
        static int count;
        static void Expect(bool v, string text)
        {
            count++;
            if (!v)
                throw new Exception(text);
        }

        public static string Run()
        {
            count = 0;
            int previousArrows = -1, previousRating = -1;
            long early = 0, late = 0;
            var campaignSeeds = new HashSet<int>();
            var none = new HashSet<int>();
            for (int level = 1; level <= 500; level++)
            {
                var campaign = Levels.Campaign(level);
                Validate(campaign, true);
                int rating = Levels.DifficultyRating(campaign), arrows = campaign.arrows.Count;
                Expect(arrows == Levels.CampaignArrows[level - 1], "Catalogue is out of date at level " + level + "; regenerate it with --catalogue");
                // Levels never lose arrows; equal counts are ordered by measured difficulty.
                Expect(arrows > previousArrows || arrows == previousArrows && rating >= previousRating, "Campaign difficulty decreases at level " + level);
                Expect(campaignSeeds.Add(campaign.seed), "Duplicate campaign puzzle");
                // Boards stay small enough for thick, easily tapped arrows, and each one has something to solve.
                Expect(campaign.width * campaign.height <= Levels.LastLevelArea + campaign.height && campaign.height > campaign.width, "Campaign board too large or not portrait at level " + level);
                Expect(campaign.arrows.Any(arrow => !campaign.CanExit(arrow.id, none)), "Level " + level + " has no blocked arrow");
                if (level <= 50) early += rating;
                if (level > 450) late += rating;
                previousArrows = arrows;
                previousRating = rating;
            }
            Expect(late > early * 2, "Measured difficulty rises across the campaign");
            // A real first puzzle, then a climb players can feel: more arrows every few levels from the start.
            var first = Levels.Campaign(1);
            Expect(first.width <= 12 && first.arrows.Count >= 30 && first.arrows.Count <= 55, "Level 1 is a real puzzle without a wall of arrows");
            Expect(first.arrows.Count(arrow => !first.CanExit(arrow.id, none)) * 2 >= first.arrows.Count, "Level 1 needs some planning");
            for (int level = 6; level <= 101; level += 5)
                Expect(Levels.CampaignArrows[level - 1] > Levels.CampaignArrows[level - 6], "Arrows grow every five levels early on, at level " + level);
            for (int level = 5; level <= 100; level++)
                Expect(Levels.CampaignArrows[level - 1] > Levels.CampaignArrows[level - 5], "At most four early levels in a row share an arrow count, at level " + level);
            for (int level = 120; level <= 500; level += 20)
                Expect(Levels.CampaignArrows[level - 1] > Levels.CampaignArrows[level - 21], "Arrows keep growing in later levels, at level " + level);
            Expect(Levels.CampaignArrows[9] * 10 >= Levels.CampaignArrows[0] * 13 && Levels.CampaignArrows[49] * 10 >= Levels.CampaignArrows[0] * 22, "Difficulty climbs noticeably through the first levels");
            Expect(Levels.CampaignArrows[9] <= 80 && Levels.CampaignArrows[49] <= 140 && Levels.CampaignArrows[99] <= 170, "Early levels stay well below the old 115-arrow start");
            Expect(Levels.CampaignArrows[499] <= 260, "Late levels stay manageable");
            foreach (int level in new[] { 250, 500 })
            {
                var board = Levels.Campaign(level);
                Expect(board.arrows.Count(arrow => !board.CanExit(arrow.id, none)) >= board.arrows.Count * .5,
                    "Later levels need blockers cleared first " + level);
            }
            // Daily boards climb through each month like campaign levels 10 to 40, and every date has its own board.
            var dailyBoards = new HashSet<string>();
            int dailyMatches = 0, previousDaily = -1, firstOfMonth = -1;
            for (int day = 0; day < 366; day++)
            {
                var when = new DateTime(2028, 1, 1).AddDays(day);
                var daily = Levels.Daily(when);
                Validate(daily, true);
                int level = Levels.DailyLevel(when), extra = daily.arrows.Count - Levels.CampaignArrows[level - 1];
                string name = when.ToString("yyyy-MM-dd");
                Expect(level == 9 + when.Day, "Daily level follows the date, " + name);
                Expect(Math.Abs(extra) <= 3, "Daily " + name + " plays like level " + level);
                if (extra == 0 || extra == 1) dailyMatches++;
                Expect(dailyBoards.Add(Fingerprint(daily)), "Every date has its own daily board, " + name);
                if (when.Day == 1) firstOfMonth = daily.arrows.Count;
                else Expect(daily.arrows.Count >= previousDaily - 3, "Daily arrows never drop sharply within a month, " + name);
                if (when.AddDays(1).Day == 1)
                    Expect(daily.arrows.Count >= firstOfMonth + 25, "Daily puzzles get harder through the month, " + when.ToString("yyyy-MM"));
                previousDaily = daily.arrows.Count;
            }
            Expect(dailyMatches * 10 >= 366 * 9, "Daily boards have their level's arrow count (or one more)");
            // Weekly boards, which tournaments also use, play like campaign level 100.
            var weeklyBoards = new HashSet<string>();
            int weeklyMatches = 0;
            for (int week = 0; week < 52; week++)
                for (int i = 1; i <= 5; i++)
                {
                    var weekly = Levels.Weekly(new DateTime(2026, 1, 1).AddDays(week * 7), i);
                    Validate(weekly, true);
                    int extra = weekly.arrows.Count - Levels.CampaignArrows[Levels.WeeklyLevel - 1];
                    Expect(Math.Abs(extra) <= 4, "Weekly puzzles play like level " + Levels.WeeklyLevel);
                    if (extra == 0 || extra == 1) weeklyMatches++;
                    Expect(weeklyBoards.Add(Fingerprint(weekly)), "Every weekly puzzle has its own board");
                }
            Expect(weeklyMatches * 10 >= 260 * 8, "Weekly boards have level 100's arrow count (or one more)");
            Expect(Levels.LayoutVersion("campaign") == Levels.GenerationVersion && Levels.LayoutVersion("daily") == Levels.ChallengeGenerationVersion
                && Levels.LayoutVersion("weekly") == Levels.ChallengeGenerationVersion && Levels.LayoutVersion("tournament") == Levels.ChallengeGenerationVersion,
                "Each kind of puzzle checks its own layout version");
            var oldDaily = new Attempt { lives = 2, generationVersion = Levels.ChallengeGenerationVersion - 1 };
            oldDaily.removed.Add(1);
            oldDaily.PrepareForGeneration(Levels.LayoutVersion("daily"));
            Expect(oldDaily.removed.Count == 0 && oldDaily.lives == 3, "Saved daily attempts restart on the new daily boards");
            var currentCampaign = new Attempt { lives = 2, generationVersion = Levels.GenerationVersion };
            currentCampaign.removed.Add(1);
            currentCampaign.PrepareForGeneration(Levels.LayoutVersion("campaign"));
            Expect(currentCampaign.removed.Count == 1 && currentCampaign.lives == 2, "Unchanged campaign boards keep saved progress");
            var a = Levels.Generate(834, 12, 15);
            var b = Levels.Generate(834, 12, 15);
            Expect(Fingerprint(a) == Fingerprint(b), "Determinism");
            var legacy = new Attempt { lives = 1, hintsUsed = 2 };
            legacy.removed.Add(3);
            legacy.PrepareForGeneration(Levels.GenerationVersion);
            Expect(legacy.removed.Count == 0 && legacy.lives == 3 && legacy.hintsUsed == 2,
                "Changed layout resets moves and hearts while retaining spent hints");
            legacy.removed.Add(4);
            legacy.lives = 2;
            legacy.PrepareForGeneration(Levels.GenerationVersion);
            Expect(legacy.removed.Count == 1 && legacy.lives == 2, "Current layout resumes intact");
            Expect(Levels.Campaign(500).arrows.Count >= Levels.Campaign(1).arrows.Count * 2.5, "Arrow count grows with the level");
            var p = Levels.Tutorial();
            var gone = new HashSet<int>();
            Expect(!p.CanExit(0, gone), "Tutorial blocked path");
            Expect(p.CanExit(1, gone), "Tutorial safe path");
            gone.Add(1);
            Expect(p.CanExit(0, gone), "Removing blocker opens exit");
            Expect(!p.CanExit(1, gone), "Removed arrow cannot move");
            Expect(!p.CanExit(-5, gone), "Missing arrow cannot move");
            var progress = new Progress();
            var date = new DateTime(2028, 2, 1);
            progress.Complete("campaign", 1, date, "campaign:1");
            Expect(progress.unlocked == 2, "Unlock next");
            progress.Complete("campaign", 1, date, "campaign:1");
            Expect(progress.unlocked == 2, "Replay preserves unlock");
            progress.Complete("campaign", 500, date, "campaign:500");
            Expect(progress.unlocked == 501, "Campaign completion");
            for (int day = 1; day <= 28; day++)
                progress.Complete("daily", 0, new DateTime(2028, 2, day), "daily:" + day);
            Expect(!progress.MonthComplete(2028, 2), "Leap month needs day 29");
            progress.Complete("daily", 0, new DateTime(2028, 2, 29), "daily:29");
            Expect(progress.MonthComplete(2028, 2), "Leap trophy");
            progress.Complete("daily", 0, new DateTime(2028, 2, 29), "daily:29");
            Expect(progress.days.Count == 29, "Daily idempotence");
            Expect(Levels.WeekStart(new DateTime(2026, 9, 20)) == new DateTime(2026, 9, 14), "Sunday week boundary");
            Expect(Levels.WeekStart(new DateTime(2026, 9, 21)) == new DateTime(2026, 9, 21), "Monday week boundary");
            string key = "weekly:2026-09-14:1";
            progress.Complete("weekly", 1, date, key);
            progress.Complete("weekly", 1, date, key);
            Expect(progress.WeeklyCount(new DateTime(2026, 9, 15)) == 1, "Weekly duplicates");
            Expect(progress.WeeklyCount(new DateTime(2026, 9, 21)) == 0, "Weekly reset");
            var saved = progress.GetAttempt("campaign:8");
            saved.lives = 1;
            saved.removed.Add(3);
            Expect(progress.GetAttempt("campaign:8").lives == 1, "Attempt resume");
            Expect(progress.GetAttempt("daily:x").lives == 3, "Separate attempts");
            return "PASS: " + count + " assertions; 500 campaign + 366 daily + 260 weekly boards validated.";
        }

        static string Fingerprint(Puzzle p)
        {
            return string.Join(";", p.arrows.Select(a => string.Join(",", a.cells.Select(c => c.x + ":" + c.y).ToArray())).ToArray());
        }

        static void Validate(Puzzle p, bool reverse)
        {
            Expect(p.arrows.Count > 0, "Empty puzzle");
            var occupied = new HashSet<Cell>();
            var ids = new HashSet<int>();
            foreach (var a in p.arrows)
            {
                Expect(ids.Add(a.id), "Duplicate arrow id");
                Expect(a.cells.Count >= 2, "Short path");
                foreach (var c in a.cells)
                {
                    Expect(p.Inside(c), "Out of bounds");
                    Expect(occupied.Add(c), "Overlapping paths");
                }

                for (int i = 1; i < a.cells.Count; i++)
                {
                    var d = a.cells[i] - a.cells[i - 1];
                    Expect(Math.Abs(d.x) + Math.Abs(d.y) == 1, "Disconnected path");
                }
            }

            // Generated boards fill the grid; only a few single cells may stay empty.
            if (reverse)
                Expect(occupied.Count >= p.width * p.height * .98, "Gap-free layout seed " + p.seed);
            var removed = new HashSet<int>();
            if (reverse)
            {
                for (int i = p.arrows.Count - 1; i >= 0; i--)
                {
                    Expect(p.CanExit(p.arrows[i].id, removed), "Reverse proof failed seed " + p.seed);
                    removed.Add(p.arrows[i].id);
                }
            }
            else
            {
                for (int i = 0; i < p.arrows.Count; i++)
                {
                    int id = p.Hint(removed);
                    Expect(id >= 0, "Unsolvable tutorial");
                    removed.Add(id);
                }
            }

            // Solve independently in first-available order, rather than only the construction order.
            // Tracking blockers gives the same order as repeated Hint calls without rescanning dense boards.
            var owner = new Dictionary<Cell, int>();
            foreach (var a in p.arrows)
                foreach (var c in a.cells)
                    owner[c] = a.id;
            var waiting = new Dictionary<int, int>();
            var unblocks = new Dictionary<int, List<int>>();
            var available = new SortedSet<int>();
            foreach (var a in p.arrows)
            {
                var blockers = new HashSet<int>();
                int found;
                for (Cell c = a.Head + a.Direction; p.Inside(c); c += a.Direction)
                    if (owner.TryGetValue(c, out found) && found != a.id)
                        blockers.Add(found);
                waiting[a.id] = blockers.Count;
                foreach (int blocker in blockers)
                {
                    List<int> list;
                    if (!unblocks.TryGetValue(blocker, out list))
                        unblocks[blocker] = list = new List<int>();
                    list.Add(a.id);
                }
                if (blockers.Count == 0)
                    available.Add(a.id);
            }
            removed.Clear();
            while (available.Count > 0)
            {
                int id = available.Min;
                available.Remove(id);
                Expect(p.CanExit(id, removed), "Greedy solver stuck");
                removed.Add(id);
                List<int> freed;
                if (unblocks.TryGetValue(id, out freed))
                    foreach (int next in freed)
                        if (--waiting[next] == 0)
                            available.Add(next);
            }

            Expect(removed.Count == p.arrows.Count, "Greedy solver stuck");
            Expect(p.Hint(removed) == -1, "Completed board has no hint");
        }
    }
}
