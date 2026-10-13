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
            foreach (var other in arrows)
                if (other.id != id && !removed.Contains(other.id))
                    foreach (var c in other.cells)
                    {
                        Cell delta = c - a.Head;
                        Cell direction = a.Direction;
                        if (direction.x != 0 ? delta.y == 0 && delta.x * direction.x > 0
                            : delta.x == 0 && delta.y * direction.y > 0)
                            return false;
                    }
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

    public static partial class Levels
    {
        public const int CampaignCount = 500;
        // Candidate boards the catalogue picks the campaign levels from (see Tests/Puzzles --catalogue).
        public const int CandidateCount = 1000;
        // Bump when campaign boards change: saved unfinished campaign attempts then restart.
        public const int GenerationVersion = 9;
        // Bump when daily, weekly or tournament boards change.
        public const int ChallengeGenerationVersion = 10;

        // Layout version that saved attempts of this kind of puzzle ("campaign", "daily", ...) must match.
        public static int LayoutVersion(string kind)
        {
            return kind == "campaign" ? GenerationVersion : ChallengeGenerationVersion;
        }
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
            return CampaignCandidate(CampaignOrder[Math.Max(1, Math.Min(CampaignCount, level)) - 1]);
        }

        // The checked-in catalogue picks the levels from these fixed candidates, ordered by arrow count and then
        // measured challenge. Regenerate it with the puzzle test runner when changing generation rules.
        public static Puzzle CampaignCandidate(int candidate)
        {
            return CandidateBoard(candidate, 7139 + candidate * 104729);
        }

        // A board at a candidate's point on the difficulty curve, built from the given seed.
        static Puzzle CandidateBoard(int candidate, int seed)
        {
            int step = Math.Max(0, Math.Min(CandidateCount - 1, candidate - 1));
            int progress = step * 1000 / (CandidateCount - 1);
            // Board size and layout difficulty both climb fastest through the early levels, so each level
            // feels harder than the one before.
            int curve = DifficultyCurve(progress);
            int width, height;
            CampaignSize(curve, out width, out height);
            int hardness = FirstLevelHardness + (LastLevelHardness - FirstLevelHardness) * curve / 1000;
            return Generate(seed, width, height, hardness);
        }

        // Seeds tried for each LikeLevel board.
        public const int LikeLevelTries = 12;

        // A new board that plays like campaign level `level`: the same point on the difficulty curve (board size,
        // arrow lengths and layout hardness), built from its own seed. Of up to LikeLevelTries seeds, the first
        // with that level's arrow count or one more is kept (otherwise the closest), so these boards follow the
        // campaign's arrow counts and a run of rising levels rarely loses an arrow.
        public static Puzzle LikeLevel(int level, int seed)
        {
            level = Math.Max(1, Math.Min(CampaignCount, level));
            // Level i sits near position 2i of the candidate pool, which is ordered by size and difficulty.
            int candidate = 1 + (int)((long)(level - 1) * (CandidateCount - 1) / (CampaignCount - 1));
            int target = CampaignArrows[level - 1];
            Puzzle best = null;
            for (int attempt = 0; attempt < LikeLevelTries; attempt++)
            {
                var board = CandidateBoard(candidate, unchecked(seed + attempt * 15485863));
                int extra = board.arrows.Count - target;
                if (extra == 0 || extra == 1) return board;
                // Otherwise the closest count wins, more arrows before fewer.
                int bestExtra = best == null ? int.MaxValue : best.arrows.Count - target;
                if (best == null || Math.Abs(extra) < Math.Abs(bestExtra) || Math.Abs(extra) == Math.Abs(bestExtra) && extra > bestExtra) best = board;
            }
            return best;
        }

        public const int FirstLevelHardness = 350, LastLevelHardness = 1000;
        public const int FirstLevelArea = 130, LastLevelArea = 840;

        // Board for a point on the difficulty curve (0..1000): its area grows from 10x13 to 26x32 a little at a
        // time. Any portrait shape up to 4:5 may be used, so neighbouring levels get slightly different sizes
        // rather than long runs of the same one; ties go to the shape nearest the 8:9 board area.
        public static void CampaignSize(int curve, out int width, out int height)
        {
            curve = Math.Max(0, Math.Min(1000, curve));
            int area = FirstLevelArea + (LastLevelArea - FirstLevelArea) * curve / 1000;
            width = height = 0;
            int bestGap = int.MaxValue, bestShape = int.MaxValue;
            for (int w = 2; w * w <= area * 2; w++)
                for (int h = w + 1; h * 4 <= w * 5; h++)
                {
                    int gap = Math.Abs(w * h - area), shape = Math.Abs(h * 8 - w * 9);
                    if (gap < bestGap || gap == bestGap && shape < bestShape)
                    {
                        bestGap = gap; bestShape = shape; width = w; height = h;
                    }
                }
        }

        // Campaign progress 0..1000 to difficulty 0..1000: a steady climb that is steepest early and keeps
        // rising to the last level. Pairs of (progress, difficulty), joined by straight lines.
        static readonly int[] Curve = { 0, 0, 18, 73, 38, 137, 58, 189, 98, 272, 198, 425, 399, 619, 599, 763, 800, 882, 1000, 1000 };

        public static int DifficultyCurve(int progress)
        {
            progress = Math.Max(0, Math.Min(1000, progress));
            for (int i = 2; i < Curve.Length; i += 2)
                if (progress <= Curve[i])
                    return Curve[i - 1] + (Curve[i + 1] - Curve[i - 1]) * (progress - Curve[i - 2]) / (Curve[i] - Curve[i - 2]);
            return 1000;
        }

        static int Sqrt(int n)
        {
            int r = (int)Math.Sqrt(n);
            while (r * r > n) r--;
            while ((r + 1) * (r + 1) <= n) r++;
            return r;
        }

        // Roughly the 8:9 shape of the game's board area.
        public static int BoardHeight(int width)
        {
            return width + 1 + width / 8;
        }

        // Daily puzzles climb through each month: the 1st plays like campaign level 10 and every later date like
        // the next level, up to level 40 on the 31st. Each date has its own board.
        public const int DailyFirstLevel = 10;

        public static int DailyLevel(DateTime date)
        {
            return DailyFirstLevel - 1 + date.Day;
        }

        public static Puzzle Daily(DateTime date)
        {
            return LikeLevel(DailyLevel(date), DateSeed(date));
        }

        // Every weekly puzzle (and tournament puzzle, which uses the same boards) plays like campaign level 100.
        public const int WeeklyLevel = 100;

        public static Puzzle Weekly(DateTime date, int index)
        {
            return LikeLevel(WeeklyLevel, unchecked(DateSeed(WeekStart(date)) * 31 + index * 7919));
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

        // Every arrow gets an order key; removing arrows from the highest key down always works,
        // because an arrow's key is above every arrow whose exit it covers and below every arrow
        // covering its own exit. New arrows may slot in between existing keys, so they can fill
        // corridors and pockets instead of leaving empty space.
        public static Puzzle Generate(int seed, int width, int height)
        {
            return Generate(seed, width, height, 500);
        }

        // Builds several layouts and ranks them by DifficultyRating. Hardness 0 keeps the easiest
        // (more arrows free at the start, shorter blocking chains), 1000 the hardest.
        public static Puzzle Generate(int seed, int width, int height, int hardness)
        {
            if (width < 2 || height < 2)
                throw new ArgumentOutOfRangeException("width", "Boards need at least two rows and columns.");
            hardness = Math.Max(0, Math.Min(1000, hardness));
            const int variants = 8;
            var layouts = new Puzzle[variants];
            var ratings = new int[variants];
            var gaps = new int[variants];
            var blocked = new bool[variants];
            for (int variant = 0; variant < variants; variant++)
            {
                var p = layouts[variant] = GenerateCandidate(seed, width, height, variant, hardness);
                ratings[variant] = DifficultyRating(p);
                gaps[variant] = width * height - p.arrows.Sum(a => a.cells.Count);
                var none = new HashSet<int>();
                blocked[variant] = p.arrows.Any(a => !p.CanExit(a.id, none));
            }
            // Only the fullest layouts compete, so small boards never show a hole, and every
            // board keeps at least one blocked arrow when it can.
            var ranked = Enumerable.Range(0, variants).OrderBy(v => gaps[v]).ThenBy(v => blocked[v] ? 0 : 1).ToList();
            ranked = ranked.Where(v => gaps[v] == gaps[ranked[0]] && blocked[v] == blocked[ranked[0]])
                .OrderBy(v => ratings[v]).ThenBy(v => v).ToList();
            return layouts[ranked[(hardness * (ranked.Count - 1) + 500) / 1000]];
        }

        public static int DifficultyRating(Puzzle p)
        {
            var owners = new Dictionary<Cell, int>();
            foreach (var a in p.arrows)
                foreach (var c in a.cells) owners[c] = a.id;
            var depths = new int[p.arrows.Count];
            int edges = 0, available = 0, turns = 0, depthSum = 0;
            for (int i = p.arrows.Count - 1; i >= 0; i--)
            {
                var a = p.arrows[i];
                var blockers = new HashSet<int>();
                for (Cell c = a.Head + a.Direction; p.Inside(c); c += a.Direction)
                {
                    int owner;
                    if (owners.TryGetValue(c, out owner) && owner != a.id) blockers.Add(owner);
                }
                if (blockers.Count == 0) available++;
                foreach (int blocker in blockers) depths[i] = Math.Max(depths[i], depths[blocker] + 1);
                edges += blockers.Count;
                depthSum += depths[i];
                for (int j = 2; j < a.cells.Count; j++)
                    if (!(a.cells[j] - a.cells[j - 1]).Equals(a.cells[j - 1] - a.cells[j - 2])) turns++;
            }
            return owners.Count * 20 + edges * 5 + depthSum * 6 + turns - available * 40;
        }

        const long KeyMax = 1L << 62;
        const int WalkBias = 75, AttemptsPerCell = 3;

        // Board growth 0..1000 between the first campaign board (10x13) and the largest (26x32).
        static int Growth(int width, int height)
        {
            return Math.Min(1000, Math.Max(0, width * height - FirstLevelArea) * 1000 / (LastLevelArea - FirstLevelArea));
        }

        // Medium arrows on the first boards; longer, more winding arrows as boards grow.
        static void ArrowLengths(int width, int height, out int minLength, out int maxLength)
        {
            int t = Growth(width, height);
            minLength = 2 + t / 1000;
            maxLength = 4 + 4 * t / 1000;
        }

        // Chance (percent) that a growing arrow carries straight on instead of turning.
        static int Straightness(int width, int height, int hardness)
        {
            return Math.Max(10, 45 - 25 * Growth(width, height) / 1000 - 10 * hardness / 1000);
        }

        static void Shuffle(Random rng, List<int> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                int swap = items[i]; items[i] = items[j]; items[j] = swap;
            }
        }

        static Puzzle GenerateCandidate(int seed, int width, int height, int variant, int hardness)
        {
            var rng = new Random(unchecked(seed + variant * 486187739));
            var b = new Builder(width, height, rng);
            b.straight = Straightness(width, height, hardness);
            int minLength, maxLength;
            ArrowLengths(width, height, out minLength, out maxLength);
            int cap = maxLength + 1;
            // Arrows from random free cells.
            for (int attempt = 0; attempt < width * height * AttemptsPerCell && b.free.Count > 0; attempt++)
            {
                int cell = b.free[rng.Next(b.free.Count)];
                Cell direction = D[rng.Next(4)];
                b.TryArrow(cell % width, cell / width, direction, rng.Next(minLength, maxLength + 1));
            }
            // Short arrows in every remaining pocket, longer tails, then stranded single cells.
            for (int round = 0; round < 2; round++)
            {
                var pockets = new List<int>(b.free);
                Shuffle(rng, pockets);
                foreach (int cell in pockets)
                {
                    if (b.owner[cell] >= 0) continue;
                    int start = rng.Next(4);
                    for (int k = 0; k < 4; k++)
                        if (b.TryArrow(cell % width, cell / width, D[(start + k) % 4], maxLength)) break;
                }
                var order = new List<int>();
                for (int a = 0; a < b.keys.Count; a++) order.Add(a);
                Shuffle(rng, order);
                foreach (int a in order) b.GrowTail(a, cap);
                var stranded = new List<int>(b.free);
                Shuffle(rng, stranded);
                foreach (int cell in stranded)
                    if (b.owner[cell] < 0) b.Absorb(cell, cap + 3);
            }
            // Ids follow the keys, so removing arrows in reverse id order solves the board.
            var ranked = new int[b.keys.Count];
            for (int i = 0; i < ranked.Length; i++) ranked[i] = i;
            Array.Sort(ranked, (x, y) => b.keys[x] != b.keys[y] ? b.keys[x].CompareTo(b.keys[y]) : x.CompareTo(y));
            var p = new Puzzle
            {
                seed = seed,
                width = width,
                height = height
            };
            foreach (int a in ranked)
            {
                var path = b.paths[a];
                var cells = new List<Cell>(path.Count);
                for (int i = path.Count - 1; i >= 0; i--)
                    cells.Add(new Cell(path[i] % width, path[i] / width));
                p.arrows.Add(new Arrow { id = p.arrows.Count, cells = cells });
            }
            return p;
        }

        // Board state while generating. Cells are indexed x + y * width; paths are stored head first.
        sealed class Builder
        {
            readonly int w, h;
            readonly Random rng;
            public int straight;
            public readonly int[] owner;
            readonly int[] headOf, freeAt;
            readonly long[] rayMax; // largest key whose exit ray passes over the cell (0 = none)
            public readonly List<int> free;
            public readonly List<long> keys = new List<long>();
            public readonly List<List<int>> paths = new List<List<int>>();
            readonly List<Cell> directions = new List<Cell>();
            readonly List<int> walk = new List<int>(), options = new List<int>(), ties = new List<int>();

            public Builder(int width, int height, Random random)
            {
                w = width;
                h = height;
                rng = random;
                int n = width * height;
                owner = new int[n];
                headOf = new int[n];
                freeAt = new int[n];
                rayMax = new long[n];
                free = new List<int>(n);
                for (int i = 0; i < n; i++)
                {
                    owner[i] = -1;
                    headOf[i] = -1;
                    freeAt[i] = i;
                    free.Add(i);
                }
            }

            bool Inside(int x, int y)
            {
                return x >= 0 && y >= 0 && x < w && y < h;
            }

            void Take(int cell, int arrow)
            {
                owner[cell] = arrow;
                int i = freeAt[cell], last = free[free.Count - 1];
                free[i] = last;
                freeAt[last] = i;
                free.RemoveAt(free.Count - 1);
                freeAt[cell] = -1;
            }

            int OpenNeighbours(int cell, bool growing)
            {
                int x = cell % w, y = cell / w, count = 0;
                foreach (Cell d in D)
                    if (Inside(x + d.x, y + d.y))
                    {
                        int c = x + d.x + (y + d.y) * w;
                        if (owner[c] < 0 && !(growing && walk.Contains(c))) count++;
                    }
                return count;
            }

            // Usually step to the most enclosed free neighbour so single cells are not stranded.
            int Choose(List<int> choices, bool growing)
            {
                if (choices.Count > 1 && rng.Next(100) < WalkBias)
                {
                    int fewest = 5;
                    ties.Clear();
                    foreach (int c in choices)
                    {
                        int open = OpenNeighbours(c, growing);
                        if (open < fewest)
                        {
                            fewest = open;
                            ties.Clear();
                            ties.Add(c);
                        }
                        else if (open == fewest)
                            ties.Add(c);
                    }
                    return ties[rng.Next(ties.Count)];
                }
                return choices[rng.Next(choices.Count)];
            }

            // The cell continuing the path's last segment, when it is allowed and the straightness roll passes.
            int Straight(List<int> path, List<int> choices)
            {
                int last = path[path.Count - 1], before = path[path.Count - 2];
                int x = last % w * 2 - before % w, y = last / w * 2 - before / w;
                if (!Inside(x, y) || !choices.Contains(x + y * w)) return -1;
                return rng.Next(100) < straight ? x + y * w : -1;
            }

            long LowestKeyAhead(int x, int y, Cell d)
            {
                long lowest = KeyMax;
                for (x += d.x, y += d.y; Inside(x, y); x += d.x, y += d.y)
                {
                    int o = owner[x + y * w];
                    if (o >= 0 && keys[o] < lowest) lowest = keys[o];
                }
                return lowest;
            }

            long HighestKeyAimingAt(int cell, int skip)
            {
                int x0 = cell % w, y0 = cell / w;
                long highest = 0;
                foreach (Cell d in D)
                    for (int x = x0 - d.x, y = y0 - d.y; Inside(x, y); x -= d.x, y -= d.y)
                    {
                        int a = headOf[x + y * w];
                        if (a >= 0 && a != skip && directions[a].Equals(d) && keys[a] > highest) highest = keys[a];
                    }
                return highest;
            }

            void Add(List<int> path, Cell d, long lo, long hi)
            {
                long key = lo + (hi - lo) / 2;
                int arrow = keys.Count;
                keys.Add(key);
                paths.Add(path);
                directions.Add(d);
                foreach (int c in path) Take(c, arrow);
                headOf[path[0]] = arrow;
                for (int x = path[0] % w + d.x, y = path[0] / w + d.y; Inside(x, y); x += d.x, y += d.y)
                {
                    int c = x + y * w;
                    if (rayMax[c] < key) rayMax[c] = key;
                }
            }

            // Head at (hx, hy) pointing along d; the body grows backwards from the neck.
            public bool TryArrow(int hx, int hy, Cell d, int length)
            {
                int nx = hx - d.x, ny = hy - d.y;
                if (!Inside(nx, ny)) return false;
                int head = hx + hy * w, neck = nx + ny * w;
                if (owner[head] >= 0 || owner[neck] >= 0) return false;
                long hi = LowestKeyAhead(hx, hy, d);
                long lo = Math.Max(rayMax[head], rayMax[neck]);
                if (lo >= hi - 1) return false;
                var path = new List<int> { head, neck };
                walk.Clear();
                walk.Add(head);
                walk.Add(neck);
                int x = nx, y = ny;
                for (int j = 2; j < length; j++)
                {
                    options.Clear();
                    foreach (Cell e in D)
                    {
                        int cx = x + e.x, cy = y + e.y;
                        if (!Inside(cx, cy)) continue;
                        int c = cx + cy * w;
                        if (owner[c] >= 0 || walk.Contains(c) || rayMax[c] >= hi - 1) continue;
                        bool ahead = d.x != 0 ? cy == hy && (cx - hx) * d.x > 0 : cx == hx && (cy - hy) * d.y > 0;
                        if (!ahead) options.Add(c);
                    }
                    if (options.Count == 0) break;
                    int next = Straight(path, options);
                    if (next < 0) next = Choose(options, true);
                    path.Add(next);
                    walk.Add(next);
                    if (rayMax[next] > lo) lo = rayMax[next];
                    x = next % w;
                    y = next / w;
                }
                Add(path, d, lo, hi);
                return true;
            }

            // A tail may only cover cells whose crossing exits all leave after this arrow.
            public void GrowTail(int arrow, int cap)
            {
                var path = paths[arrow];
                while (path.Count < cap)
                {
                    int t = path[path.Count - 1], tx = t % w, ty = t / w;
                    options.Clear();
                    foreach (Cell e in D)
                        if (Inside(tx + e.x, ty + e.y))
                        {
                            int c = tx + e.x + (ty + e.y) * w;
                            if (owner[c] < 0 && rayMax[c] < keys[arrow]) options.Add(c);
                        }
                    if (options.Count == 0) break;
                    int next = Straight(path, options);
                    if (next < 0) next = Choose(options, false);
                    path.Add(next);
                    Take(next, arrow);
                }
            }

            // Attach a stranded cell to a neighbouring tail, or move a head forward onto it.
            public bool Absorb(int cell, int cap)
            {
                int x = cell % w, y = cell / w;
                foreach (Cell e in D)
                {
                    if (!Inside(x + e.x, y + e.y)) continue;
                    int n = x + e.x + (y + e.y) * w, a = owner[n];
                    if (a < 0 || paths[a].Count >= cap) continue;
                    var path = paths[a];
                    if (path[path.Count - 1] == n && rayMax[cell] < keys[a])
                    {
                        path.Add(cell);
                        Take(cell, a);
                        return true;
                    }
                    if (path[0] == n && directions[a].Equals(new Cell(-e.x, -e.y)) && HighestKeyAimingAt(cell, a) < keys[a])
                    {
                        headOf[n] = -1;
                        path.Insert(0, cell);
                        Take(cell, a);
                        headOf[cell] = a;
                        return true;
                    }
                }
                return false;
            }
        }
    }

    [Serializable]
    public sealed class Attempt
    {
        public const int HintLimit = 3;
        public string key;
        public int generationVersion;
        public void PrepareForGeneration(int version)
        {
            if (generationVersion == version) return;
            removed.Clear();
            lives = 3;
            generationVersion = version;
        }
        public int lives = 3;
        // Missing in older saves means zero hints used, so no save migration is needed.
        public int hintsUsed;
        public int HintsRemaining { get { return Math.Max(0, HintLimit - hintsUsed); } }
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
