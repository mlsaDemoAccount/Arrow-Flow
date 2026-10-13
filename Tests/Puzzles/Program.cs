using System;
using System.Linq;
using System.Collections.Generic;
using ArrowFlow.Core;
if (args.Contains("--catalogue")) {
 // Sort every candidate by arrow count, then measured difficulty. Levels follow that order (level i near pool position 2i),
 // but after three levels with the same arrow count the next level skips ahead to a board with more arrows, so the
 // climb stays noticeable. No level has fewer arrows than the one before.
 var pool = Enumerable.Range(1, Levels.CandidateCount).Select(n => { var p = Levels.CampaignCandidate(n); return new { Number = n, Arrows = p.arrows.Count, Rating = Levels.DifficultyRating(p) }; }).OrderBy(x => x.Arrows).ThenBy(x => x.Rating).ThenBy(x => x.Number).ToArray();
 var picks = new List<int>();
 int last = -1, run = 0;
 for (int level = 0; level < Levels.CampaignCount; level++) {
  int ideal = (int)((long)level * (pool.Length - 1) / (Levels.CampaignCount - 1)), latest = pool.Length - (Levels.CampaignCount - level);
  int pick = Math.Min(Math.Max(last + 1, ideal), latest);
  if (run >= 3) { int next = pick; while (next <= latest && pool[next].Arrows == pool[last].Arrows) next++; if (next <= latest && next - ideal <= 8) pick = next; }
  run = last >= 0 && pool[pick].Arrows == pool[last].Arrows ? run + 1 : 1;
  picks.Add(pick); last = pick;
 }
 var ranked = picks.Select(i => pool[i]).ToArray();
 var order = Enumerable.Range(0, 25).Select(row => "            " + string.Join(", ", ranked.Skip(row * 20).Take(20).Select(x => x.Number)) + ",");
 var counts = Enumerable.Range(0, 25).Select(row => "            " + string.Join(", ", ranked.Skip(row * 20).Take(20).Select(x => x.Arrows)) + ",");
 System.IO.File.WriteAllText("Assets/ArrowFlow/Core/CampaignOrder.cs", "// Generated with Tests/Puzzles --catalogue. Keep in sync with GenerationVersion.\n// Levels are picked from the candidate pool by arrow count, then DifficultyRating; arrow counts never decrease.\nnamespace ArrowFlow.Core\n{\n    public static partial class Levels\n    {\n        static readonly int[] CampaignOrder =\n        {\n" + string.Join("\n", order) + "\n        };\n\n        // Arrow count of each level; Checks.Run compares it with the generator to catch drift.\n        public static readonly int[] CampaignArrows =\n        {\n" + string.Join("\n", counts) + "\n        };\n    }\n}\n");
 Console.WriteLine($"Picked {ranked.Length} of {pool.Length} boards: {ranked.First().Arrows} to {ranked.Last().Arrows} arrows, difficulty {ranked.Min(x => x.Rating)} to {ranked.Max(x => x.Rating)}.");
 return;
}
foreach (int level in new[] { 1, 2, 20, 100, 500 }) {
 var p = Levels.Campaign(level);
 Console.WriteLine($"Level {level}: {p.width}x{p.height}, {p.arrows.Count} arrows, {p.arrows.Sum(a=>a.cells.Count)*100.0/(p.width*p.height):F1}% occupied, {p.arrows.Count(a=>!p.CanExit(a.id,new HashSet<int>()))} blocked");
}
foreach (int day in new[] { 1, 15, 31 }) {
 var date = new DateTime(2026, 10, day);
 var p = Levels.Daily(date);
 Console.WriteLine($"Daily {date:MMM d} (like level {Levels.DailyLevel(date)}): {p.width}x{p.height}, {p.arrows.Count} arrows");
}
var weekly = Levels.Weekly(new DateTime(2026, 10, 5), 1);
Console.WriteLine($"Weekly (like level {Levels.WeeklyLevel}): {weekly.width}x{weekly.height}, {weekly.arrows.Count} arrows");
System.IO.Directory.CreateDirectory("Builds/PuzzlePreview");
var first = Levels.Campaign(1);
System.IO.File.WriteAllLines("Builds/PuzzlePreview/level-1.txt", first.arrows.Select(a => string.Join(" ", a.cells.Select(c => c.x + "," + c.y))));
if (!args.Contains("--preview")) Console.WriteLine(Checks.Run());


