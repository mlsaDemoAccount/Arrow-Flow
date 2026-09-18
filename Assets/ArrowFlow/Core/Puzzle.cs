using System;
using System.Collections.Generic;
using System.Linq;

namespace ArrowFlow.Core
{
    [Serializable]
    public struct Cell : IEquatable<Cell>
    {
        public int x, y;
        public Cell(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public bool Equals(Cell b)
        {
            return x == b.x && y == b.y;
        }

        public override bool Equals(object b)
        {
            return b is Cell && Equals((Cell)b);
        }

        public override int GetHashCode()
        {
            return x * 397 ^ y;
        }

        public static Cell operator +(Cell a, Cell b)
        {
            return new Cell(a.x + b.x, a.y + b.y);
        }

        public static Cell operator -(Cell a, Cell b)
        {
            return new Cell(a.x - b.x, a.y - b.y);
        }
    }

    [Serializable]
    public sealed class Arrow
    {
        public int id;
        public List<Cell> cells = new List<Cell>();
        public Cell Head
        {
            get
            {
                return cells[cells.Count - 1];
            }
        }

        public Cell Direction
        {
            get
            {
                return Head - cells[cells.Count - 2];
            }
        }
    }

    [Serializable]
    public sealed class Puzzle
    {
        public int width, height, seed;
        public List<Arrow> arrows = new List<Arrow>();
        public bool Inside(Cell c)
        {
            return c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;
        }

        public bool CanExit(int id, HashSet<int> removed)
        {
            if (removed.Contains(id))
                return false;
            Arrow a = arrows.Find(v => v.id == id);
            if (a == null)
                return false;
            var occupied = new HashSet<Cell>();
            foreach (var other in arrows)
                if (other.id != id && !removed.Contains(other.id))
                    foreach (var c in other.cells)
                        occupied.Add(c);
            for (Cell c = a.Head + a.Direction; Inside(c); c += a.Direction)
                if (occupied.Contains(c))
                    return false;
            return true;
        }

        public int Hint(HashSet<int> removed)
        {
            foreach (var a in arrows)
                if (CanExit(a.id, removed))
                    return a.id;
            return -1;
        }
    }

    public static class Levels
    {
        public const int CampaignCount = 500;
        static readonly Cell[] D =
        {
            new Cell(1, 0),
            new Cell(0, 1),
            new Cell(-1, 0),
            new Cell(0, -1)
        };
        public static int DateSeed(DateTime date)
        {
            return date.Year * 10000 + date.Month * 100 + date.Day;
        }

        public static DateTime WeekStart(DateTime date)
        {
            return date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }

        public static Puzzle Campaign(int level)
        {
            if (level == 1)
                return Tutorial();
            int s = Math.Min(19, 6 + (level - 1) / 12);
            return Generate(7139 + level * 104729, s, s + 2);
        }

        public static Puzzle Daily(DateTime date)
        {
            return Generate(DateSeed(date), 13, 15);
        }

        public static Puzzle Weekly(DateTime date, int index)
        {
            return Generate(unchecked(DateSeed(WeekStart(date)) * 31 + index * 7919), 14, 16);
        }

        public static Puzzle Tutorial()
        {
            return new Puzzle
            {
                width = 6,
                height = 7,
                seed = 1,
                arrows = new List<Arrow>
                {
                    new Arrow
                    {
                        id = 0,
                        cells = new List<Cell>
                        {
                            new Cell(0, 2),
                            new Cell(0, 3),
                            new Cell(1, 3),
                            new Cell(2, 3)
                        }
                    },
                    new Arrow
                    {
                        id = 1,
                        cells = new List<Cell>
                        {
                            new Cell(3, 2),
                            new Cell(3, 3),
                            new Cell(3, 4),
                            new Cell(3, 5)
                        }
                    },
                    new Arrow
                    {
                        id = 2,
                        cells = new List<Cell>
                        {
                            new Cell(5, 1),
                            new Cell(4, 1),
                            new Cell(3, 1),
                            new Cell(2, 1)
                        }
                    },
                    new Arrow
                    {
                        id = 3,
                        cells = new List<Cell>
                        {
                            new Cell(1, 5),
                            new Cell(1, 6),
                            new Cell(2, 6)
                        }
                    }
                }
            };
        }

        // Reverse insertion order is a solution: each new arrow has an exit clear of every previous arrow.
        public static Puzzle Generate(int seed, int width, int height)
        {
            var p = new Puzzle
            {
                seed = seed,
                width = width,
                height = height
            };
            var occupied = new HashSet<Cell>();
            var rng = new Random(seed);
            for (int attempt = 0; attempt < width * height * 45; attempt++)
            {
                Cell head = new Cell(rng.Next(width), rng.Next(height)), dir = D[rng.Next(4)], neck = head - dir;
                if (!p.Inside(neck) || occupied.Contains(head) || occupied.Contains(neck))
                    continue;
                bool clear = true;
                for (Cell c = head + dir; p.Inside(c); c += dir)
                    if (occupied.Contains(c))
                    {
                        clear = false;
                        break;
                    }

                if (!clear)
                    continue;
                var path = new List<Cell>
                {
                    head,
                    neck
                };
                var own = new HashSet<Cell>(path);
                int length = rng.Next(3, 10);
                for (int j = 2; j < length; j++)
                {
                    var options = new List<Cell>();
                    foreach (Cell d in D)
                    {
                        Cell c = path[path.Count - 1] + d;
                        bool ray = dir.x != 0 ? c.y == head.y && (c.x - head.x) * dir.x > 0 : c.x == head.x && (c.y - head.y) * dir.y > 0;
                        if (p.Inside(c) && !occupied.Contains(c) && !own.Contains(c) && !ray)
                            options.Add(c);
                    }

                    if (options.Count == 0)
                        break;
                    Cell next = options[rng.Next(options.Count)];
                    path.Add(next);
                    own.Add(next);
                }

                path.Reverse();
                p.arrows.Add(new Arrow { id = p.arrows.Count, cells = path });
                foreach (Cell c in path)
                    occupied.Add(c);
                if (occupied.Count >= width * height * 0.88f)
                    break;
            }

            return p;
        }
    }

    [Serializable]
    public sealed class Attempt
    {
        public string key;
        public int lives = 3;
        public List<int> removed = new List<int>();
    }

    [Serializable]
    public sealed class Progress
    {
        public int version = 1, unlocked = 1;
        public bool sound = true, haptics = true, tutorialSeen, reminders;
        public List<string> days = new List<string>(), weeklyWins = new List<string>();
        public List<Attempt> attempts = new List<Attempt>();
        public Attempt GetAttempt(string key)
        {
            var a = attempts.Find(x => x.key == key);
            if (a == null)
            {
                a = new Attempt
                {
                    key = key
                };
                attempts.Add(a);
            }

            return a;
        }

        public void Complete(string mode, int level, DateTime date, string key)
        {
            attempts.RemoveAll(a => a.key == key);
            if (mode == "campaign")
                unlocked = Math.Max(unlocked, Math.Min(Levels.CampaignCount + 1, level + 1));
            if (mode == "daily" && !days.Contains(date.ToString("yyyy-MM-dd")))
                days.Add(date.ToString("yyyy-MM-dd"));
            if (mode == "weekly" && !weeklyWins.Contains(key))
                weeklyWins.Add(key);
        }

        public bool MonthComplete(int year, int month)
        {
            for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                if (!days.Contains(new DateTime(year, month, d).ToString("yyyy-MM-dd")))
                    return false;
            return true;
        }

        public int WeeklyCount(DateTime date)
        {
            string prefix = "weekly:" + Levels.WeekStart(date).ToString("yyyy-MM-dd") + ":";
            return weeklyWins.Count(w => w.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
