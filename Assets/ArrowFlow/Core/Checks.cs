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
            for (int level = 1; level <= 500; level++)
                Validate(Levels.Campaign(level), level != 1);
            for (int day = 0; day < 366; day++)
                Validate(Levels.Daily(new DateTime(2028, 1, 1).AddDays(day)), true);
            for (int week = 0; week < 52; week++)
                for (int i = 1; i <= 5; i++)
                    Validate(Levels.Weekly(new DateTime(2026, 1, 1).AddDays(week * 7), i), true);
            var a = Levels.Generate(834, 12, 15);
            var b = Levels.Generate(834, 12, 15);
            Expect(Fingerprint(a) == Fingerprint(b), "Determinism");
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
            removed.Clear();
            for (int i = 0; i < p.arrows.Count; i++)
            {
                int id = p.Hint(removed);
                Expect(id >= 0, "Greedy solver stuck");
                removed.Add(id);
            }

            Expect(p.Hint(removed) == -1, "Completed board has no hint");
        }
    }
}
